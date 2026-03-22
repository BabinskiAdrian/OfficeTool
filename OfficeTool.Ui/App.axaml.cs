using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using OfficeTool.Ui.Services;
using OfficeTool.Ui.ViewModels;
using OfficeTool.Ui.Views;
using System.Linq;
using OfficeTool.Infrastructure.BackupConfigs;

namespace OfficeTool.Ui;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dialogService = new AvaloniaDialogService();
            var scalanceService = new ScalanceConfigService();

            // Main view
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(dialogService, scalanceService)
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}