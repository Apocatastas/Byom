using System.Windows;
using Byom.Abstractions.Interfaces;
using Byom.Core.Analytics;
using Byom.Core.Parsing;
using Byom.WPF.Services;
using Byom.WPF.ViewModels;
using Byom.WPF.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Byom.WPF;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .UseSerilog((context, services, config) =>
            {
                config
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .WriteTo.File(
                        path: "logs/byom-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7);
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            })
            .Build();

        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // --- Services (инфраструктура UI) ---
        services.AddSingleton<IFileDialogService, WpfFileDialogService>();
        services.AddSingleton<IByomHelpService, HelpService>();
        services.AddSingleton<ISummaryCalculator, SummaryCalculator>();

        // --- Domain services ---
        services.AddSingleton<ITransactionParser, TBankCsvParser>();

        // --- ViewModels ---
        services.AddSingleton<MainViewModel>();

        // --- Views ---
        services.AddSingleton<MainWindow>(sp =>
        {
            var window = new MainWindow();
            window.DataContext = sp.GetRequiredService<MainViewModel>();
            return window;
        });
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
