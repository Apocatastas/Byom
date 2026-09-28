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
using LiveChartsCore.SkiaSharpView.Painting.Effects;
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

    [ObservableProperty]
    private ISeries[] _monthlySeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _monthlyXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _monthlyYAxes = Array.Empty<Axis>();

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
        CategorySeries = Array.Empty<ISeries>();
        MonthlySeries = Array.Empty<ISeries>();
        MonthlyXAxes = Array.Empty<Axis>();
        MonthlyYAxes = Array.Empty<Axis>();
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
            UpdateCategorySeries(Summary.Categories);
            UpdateMonthlySeries(Summary.Months);

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

    private static readonly SKColor IncomeColor = new(0x27, 0xAE, 0x60);
    private static readonly SKColor ExpenseColor = new(0xC0, 0x39, 0x2B);
    private static readonly SKColor NetColor = new(0x2C, 0x3E, 0x50);

    private void UpdateMonthlySeries(IReadOnlyList<MonthlySummary> months)
    {
        if (months.Count == 0)
        {
            MonthlySeries = Array.Empty<ISeries>();
            MonthlyXAxes = Array.Empty<Axis>();
            MonthlyYAxes = Array.Empty<Axis>();
            return;
        }

        MonthlySeries = new ISeries[]
        {
        new ColumnSeries<decimal>
        {
            Name = "Доход",
            Values = months.Select(m => m.Income).ToArray(),
            Fill = new SolidColorPaint(IncomeColor),
            Stroke = null,
            MaxBarWidth = 28,
            DataLabelsPaint = new SolidColorPaint(SKColors.Black),
            DataLabelsSize = 10,
            DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
            DataLabelsFormatter = point =>
                point.Coordinate.PrimaryValue > 0
                    ? $"{point.Coordinate.PrimaryValue:N0}"
                    : "",
        },
        new ColumnSeries<decimal>
        {
            Name = "Расход",
            Values = months.Select(m => m.Expense).ToArray(),
            Fill = new SolidColorPaint(ExpenseColor),
            Stroke = null,
            MaxBarWidth = 28,
            DataLabelsPaint = new SolidColorPaint(SKColors.Black),
            DataLabelsSize = 10,
            DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
            DataLabelsFormatter = point =>
                point.Coordinate.PrimaryValue > 0
                    ? $"{point.Coordinate.PrimaryValue:N0}"
                    : "",
        },
         new LineSeries<decimal>
    {
        Name = "Баланс",
        Values = months.Select(m => m.Net).ToArray(),
        Stroke = new SolidColorPaint(NetColor) { StrokeThickness = 2 },
        Fill = null,
        GeometryFill = new SolidColorPaint(NetColor),
        GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
        GeometrySize = 10,
        LineSmoothness = 0.3,
        DataLabelsPaint = new SolidColorPaint(NetColor),
        DataLabelsSize = 10,
        DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
        DataLabelsFormatter = point =>
            point.Coordinate.PrimaryValue != 0
                ? $"{point.Coordinate.PrimaryValue:N0}"
                : "",
    },
        };

        MonthlyXAxes = new Axis[]
        {
        new Axis
        {
            Labels = months.Select(m => m.DisplayName).ToArray(),
            LabelsRotation = 0,
            TextSize = 12,
            SeparatorsPaint = null,
            TicksPaint = null,
        }
        };

        MonthlyYAxes = new Axis[]
        {
        new Axis
        {
            Labeler = value => value.ToString("N0") + " ₽",
            TextSize = 11,
            SeparatorsPaint = new SolidColorPaint(SKColors.LightGray)
            {
                StrokeThickness = 1,
                PathEffect = new DashEffect(new float[] { 4, 4 })
            },
            TicksPaint = null,
        }
        };
    }
}
