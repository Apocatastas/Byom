using System.Collections.ObjectModel;
using System.Diagnostics;
using Byom.Abstractions.Interfaces;
using Byom.Abstractions.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Microsoft.Extensions.Logging;

namespace Byom.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ITransactionParser _parser;
    private readonly IFileDialogService _fileDialog;
    private readonly IByomHelpService _help;
    private readonly ILogger<MainViewModel> _logger;

    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string? _selectedFilePath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    public ObservableCollection<Transaction> Transactions { get; } = new();

    public MainViewModel(
        ITransactionParser parser,
        IFileDialogService fileDialog,
        IByomHelpService help,
        ILogger<MainViewModel> logger)
    {
        _parser = parser;
        _fileDialog = fileDialog;
        _help = help;
        _logger = logger;

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
}
