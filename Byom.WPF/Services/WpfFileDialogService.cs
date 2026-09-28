using Byom.Abstractions.Interfaces;
using Microsoft.Win32;

namespace Byom.WPF.Services;

public sealed class WpfFileDialogService : IFileDialogService
{
    public string? PickOpenCsvFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите выписку операций",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
