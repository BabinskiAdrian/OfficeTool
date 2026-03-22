using CommunityToolkit.Mvvm.Input;
using OfficeTool.Core.BackupConfigs.Services;
using OfficeTool.Ui.Services;
using System;

namespace OfficeTool.Ui.ViewModels;

public partial class MainMenuViewModel : ViewModelBase
{
    private readonly Action<ViewModelBase> _navigateAction;  // For changeing view in run-time
    private readonly IDialogService? _dialogService;
    private readonly IScalanceConfigService? _scalanceService;

    // Main constructor
    public MainMenuViewModel(Action<ViewModelBase> navigateAction, IDialogService dialogService, IScalanceConfigService scalanceService)
    {
        _navigateAction = navigateAction;
        _dialogService = dialogService;
        _scalanceService = scalanceService;
    }

    // Constructor for live view Avalonii's designer
    public MainMenuViewModel()
    {
        _navigateAction = (_) => { };
    }

    [RelayCommand]
    private void OpenBackupConfigs()
    {
        // Open new view/window
        if (_dialogService != null && _scalanceService != null)
        {
            var newPage = new BackupConfigsViewModel(_dialogService, _scalanceService, _navigateAction);
            _navigateAction(newPage);
        }
    }

    [RelayCommand]
    private void OpenNewExcelsGenerator()
    {
        // TODO: Nowe okno i funkcjonalności
        // _navigateAction(new ExcelGeneratorViewModel());
    }
}