using System.Collections.ObjectModel;
using System.Diagnostics;
using Byom.Abstractions.Interfaces;
using Byom.Abstractions.Models;
using Byom.Core.Analytics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Serilog;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Byom.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ITransactionParser _parser;
    private readonly IFileDialogService _fileDialog;
    private readonly IByomHelpService _help;
    private readonly ILogger<MainViewModel> _logger;
    private readonly ISummaryCalculator _summaryCalculator;

    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string? _selectedFilePath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private PeriodSummary _summary = PeriodSummary.Empty;

    [ObservableProperty]
    private ISeries[] _categorySeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private string _topCategorySummaryText = "";

    public ObservableCollection<Transaction> Transactions { get; } = new();

    public MainViewModel(
        ITransactionParser parser,
        IFileDialogService fileDialog,
        IByomHelpService help,
         ISummaryCalculator summaryCalculator,
        ILogger<MainViewModel> logger)
    {
        _parser = parser;
        _fileDialog = fileDialog;
        _help = help;
        _logger = logger;
        _summaryCalculator = summaryCalculator;
        StatusMessage = "Готов к работе";
    }

    [RelayCommand]
    private void PickFile()
    {
        var path = _fileDialog.PickOpenCsvFile();
        if (path is null)
        {
            return;
        }

        SelectedFilePath = path;
        _logger.LogInformation("Выбран файл: {Path}", path);
    }

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private async Task LoadAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedFilePath))
        {
            return;
        }

        _cts = new CancellationTokenSource();
        IsBusy = true;
        StatusMessage = "Загрузка...";
        Summary = PeriodSummary.Empty;
        CategorySeries = Array.Empty<ISeries>();
        Transactions.Clear();

        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Начало загрузки: {Path}", SelectedFilePath);

        try
        {
            var result = await _parser.ParseAsync(SelectedFilePath, _cts.Token);

            foreach (var tx in result.Transactions)
            {
                Transactions.Add(tx);
            }

            Summary = _summaryCalculator.Calculate(result.Transactions);
            UpdateCategorySeries(Summary.Categories);

            sw.Stop();
            StatusMessage = $"Загружено: {result.ParsedRows} из {result.TotalRows}, " +
                            $"пропущено: {result.SkippedRows}, " +
                            $"за {sw.ElapsedMilliseconds} мс";

            _logger.LogInformation(
                "Загрузка завершена. Всего: {Total}, загружено: {Parsed}, пропущено: {Skipped}, {Ms} мс",
                result.TotalRows, result.ParsedRows, result.SkippedRows, sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Загрузка отменена";
            _logger.LogInformation("Загрузка отменена пользователем");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
            _logger.LogError(ex, "Ошибка при загрузке файла");
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private bool CanLoad() => !IsBusy && !string.IsNullOrWhiteSpace(SelectedFilePath);

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    private bool CanCancel() => IsBusy;

    [RelayCommand]
    private void ShowHelp()
    {
        _help.ShowHelp();
    }

    partial void OnSelectedFilePathChanged(string? value)
    {
        LoadCommand.NotifyCanExecuteChanged();
    }

    private const int TopCategoriesCount = 10;

    private void UpdateCategorySeries(IReadOnlyList<CategorySummary> categories)
    {
        if (categories.Count == 0)
        {
            CategorySeries = Array.Empty<ISeries>();
            return;
        }

        var top = categories.Take(TopCategoriesCount).ToList();
        var othersTotal = categories.Skip(TopCategoriesCount).Sum(c => c.Total);

        var series = new List<ISeries>();

        foreach (var cat in top)
        {
            series.Add(BuildPieSlice(cat.Category, cat.Total));
        }

        if (othersTotal > 0)
        {
            series.Add(BuildPieSlice($"Прочее ({categories.Count - TopCategoriesCount})", othersTotal));
        }

        CategorySeries = series.ToArray();
    }

    private static PieSeries<decimal> BuildPieSlice(string name, decimal value)
    {
        return new PieSeries<decimal>
        {
            Name = name,
            Values = new[] { value },
            DataLabelsPaint = new SolidColorPaint(SKColors.White),
            DataLabelsSize = 12,
            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
            DataLabelsFormatter = point => $"{point.Model:N0} ₽",
            ToolTipLabelFormatter = point =>
                $"{point.Coordinate.PrimaryValue:N2} ₽",
            Pushout = 0,
        };
    }
}
