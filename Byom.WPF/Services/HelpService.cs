using System.ComponentModel.Design;
using System.Windows;
using Byom.Abstractions.Interfaces;
using Byom.WPF.Views;

namespace Byom.WPF.Services;

public sealed class HelpService : IByomHelpService
{
    private HelpWindow? _window;

    public void ShowHelp()
    {
        // Если окно уже открыто — просто активируем его
        if (_window is { IsLoaded: true })
        {
            if (_window.WindowState == WindowState.Minimized)
            {
                _window.WindowState = WindowState.Normal;
            }

            _window.Activate();
            return;
        }

        _window = new HelpWindow
        {
            Owner = Application.Current.MainWindow
        };

        _window.Closed += (_, _) => _window = null;
        _window.Show();
    }
}
