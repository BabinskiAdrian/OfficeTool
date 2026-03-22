using CommunityToolkit.Mvvm.ComponentModel;
using OfficeTool.Core.BackupConfigs.Services;
using OfficeTool.Ui.Services;

namespace OfficeTool.Ui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDialogService? _dialogService;
    private readonly IScalanceConfigService? _scalanceService;

    [ObservableProperty]
    private ViewModelBase _currentPage;

    // Konstruktor główny
    public MainWindowViewModel(IDialogService dialogService, IScalanceConfigService scalanceService)
    {
        _dialogService = dialogService;
        _scalanceService = scalanceService;


        _currentPage = new MainMenuViewModel(Navigate, _dialogService, _scalanceService);
    }

    // Konstruktor dla Designera
    public MainWindowViewModel()
    {
        _currentPage = new MainMenuViewModel();
    }

    // For navigation
    private void Navigate(ViewModelBase viewModel) => CurrentPage = viewModel;
}