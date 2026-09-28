using System.Collections.ObjectModel;
using Byom.Abstractions.Interfaces;
using Byom.Abstractions.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace Byom.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ITransactionParser _parser;
    private readonly IFileDialogService _fileDialog;
    private readonly IByomHelpService _help;

    [ObservableProperty]
    private string? _selectedFilePath;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    public ObservableCollection<Transaction> Transactions { get; } = new();

    public MainViewModel(
        ITransactionParser parser,
        IFileDialogService fileDialog,
        IByomHelpService help)
    {
        _parser = parser;
        _fileDialog = fileDialog;
        _help = help;
    }

    [RelayCommand]
    private void PickFile()
    {
        // TODO: реализуем в задаче про VM
        var path = _fileDialog.PickOpenCsvFile();
        if (path is not null)
        {
            SelectedFilePath = path;
        }
    }

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private async Task LoadAsync()
    {
        // TODO: настоящая загрузка в задаче про парсер
        await Task.CompletedTask;
    }

    private bool CanLoad() => !IsBusy && !string.IsNullOrWhiteSpace(SelectedFilePath);

    [RelayCommand]
    private void ShowHelp()
    {
        _help.ShowHelp();
    }

    partial void OnIsBusyChanged(bool value)
    {
        LoadCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedFilePathChanged(string? value)
    {
        LoadCommand.NotifyCanExecuteChanged();
    }
}
