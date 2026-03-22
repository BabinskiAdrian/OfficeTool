using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OfficeTool.Core.BackupConfigs.Models;
using OfficeTool.Core.BackupConfigs.Services;
using OfficeTool.Ui.Services;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace OfficeTool.Ui.ViewModels;

public partial class BackupConfigsViewModel : ViewModelBase
{
    private readonly IDialogService? _dialogService;
    private readonly IScalanceConfigService? _scalanceService;
    private readonly Action<ViewModelBase> _navigateAction;

    [ObservableProperty]
    private string _baseZipPath = "No base file selected";

    [ObservableProperty]
    private string _destinationFolderPath = "No destination folder selected";

    [ObservableProperty]
    private string _baseIpAddress= "No data read";

    // Mode: true = Amount, false = Range
    [ObservableProperty] private string _generationStatus = "Waiting for it to start...";
    [ObservableProperty] private bool _isGenerating = false;
    [ObservableProperty] private bool _isAmountMode = true;
    [ObservableProperty] private int _amountOfFiles = 1;

    // Range mode - low octet
    [ObservableProperty] private int _startOctet1 = 192;
    [ObservableProperty] private int _startOctet2 = 168;
    [ObservableProperty] private int _startOctet3 = 0;
    [ObservableProperty] private int _startOctet4 = 1;

    // Range mode - high octet
    [ObservableProperty] private int _endOctet1 = 192;
    [ObservableProperty] private int _endOctet2 = 168;
    [ObservableProperty] private int _endOctet3 = 0;
    [ObservableProperty] private int _endOctet4 = 10;

    [ObservableProperty]
    private int _calculatedTotalFiles = 1;


    [ObservableProperty]
    private int _generationMode = 0; // 0 = Up, 1 = Down, 2 = Up & Down


    public BackupConfigsViewModel(IDialogService dialogService, IScalanceConfigService scalanceService, Action<ViewModelBase> navigateAction)
    {
        _dialogService = dialogService;
        _scalanceService = scalanceService;
        _navigateAction = navigateAction;
    }

    // For designer Avalonia
    public BackupConfigsViewModel() 
    {
        _navigateAction = (_) => { };
    }

    [RelayCommand]
    private async Task PickBaseZipAsync()
    {
        if (_dialogService is null) return;


        var file = await _dialogService.PickFileAsync();
        if (file != null)
        {
            BaseZipPath = file; 
            BaseIpAddress = "Analyzing...";

            // Cała "brudna robota" schowana do metody poniżej
            await ExtractAndSetIpFromZipAsync(file);
        }
    }

    [RelayCommand]
    private async Task PickDestinationFolderAsync()
    {
        if (_dialogService is null) return;

        var folder = await _dialogService.PickFolderAsync();
        if (folder != null)
        {
            DestinationFolderPath = folder;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigateAction(new MainMenuViewModel(_navigateAction, _dialogService!, _scalanceService!));
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        if (_scalanceService == null) return;

        if (BaseZipPath.Contains("Not selected") || DestinationFolderPath.Contains("Not selected"))
        {
            GenerationStatus = "FAIL: Chose file and destination dictionary!";
            return;
        }

        IsGenerating = true;
        GenerationStatus = "Prosess starting...";

        try
        {
            var parameters = new BackupConfigParameters
            {
                BaseZipPath = BaseZipPath,
                DestinationFolderPath = DestinationFolderPath,
                StartIp = new[] { StartOctet1, StartOctet2, StartOctet3, StartOctet4 },
                TotalFilesToGenerate = CalculatedTotalFiles
            };

            var progress = new Progress<string>(message =>
            {
                GenerationStatus = message;
            });

            await _scalanceService.GenerateConfigsAsync(parameters, progress);

            GenerationStatus = "Completed successfull. The files are in the destination folder.";
        }
        catch (Exception ex)
        {
            GenerationStatus = $"FATAL ERROR: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    // Update logic
    partial void OnIsAmountModeChanged(bool value) => RecalculateTotal();
    partial void OnAmountOfFilesChanged(int value) => RecalculateTotal();
    partial void OnStartOctet1Changed(int value) => RecalculateTotal();
    partial void OnStartOctet2Changed(int value) => RecalculateTotal();
    partial void OnStartOctet3Changed(int value) => RecalculateTotal();
    partial void OnStartOctet4Changed(int value) => RecalculateTotal();
    partial void OnEndOctet1Changed(int value) => RecalculateTotal();
    partial void OnEndOctet2Changed(int value) => RecalculateTotal();
    partial void OnEndOctet3Changed(int value) => RecalculateTotal();
    partial void OnEndOctet4Changed(int value) => RecalculateTotal();

    private void RecalculateTotal()
    {
        if (IsAmountMode)
        {
            CalculatedTotalFiles = AmountOfFiles;
        }
        else
        {
            long startIp = (StartOctet1 * 16777216L) + (StartOctet2 * 65536L) + (StartOctet3 * 256L) + StartOctet4;
            long endIp = (EndOctet1 * 16777216L) + (EndOctet2 * 65536L) + (EndOctet3 * 256L) + EndOctet4;

            CalculatedTotalFiles = (int)Math.Abs(endIp - startIp) + 1;
        }
    }

    private async Task ExtractAndSetIpFromZipAsync(string zipFilePath)
    {
        try
        {
            using var archive = await ZipFile.OpenReadAsync(zipFilePath);
            var confEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".conf", StringComparison.OrdinalIgnoreCase));

            if (confEntry != null)
            {
                using var stream = confEntry.OpenAsync();
                using var reader = new StreamReader(await stream);
                string? line;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.StartsWith("Ip Address (In-/Out-Band)="))
                    {
                        // Ucinamy maskę podsieci
                        var ipPart = line.Substring(26).Split('/')[0];
                        BaseIpAddress = ipPart;

                        var octets = ipPart.Split('.');
                        if (octets.Length == 4)
                        {
                            StartOctet1 = int.Parse(octets[0]);
                            StartOctet2 = int.Parse(octets[1]);
                            StartOctet3 = int.Parse(octets[2]);
                            StartOctet4 = int.Parse(octets[3]);

                            EndOctet1 = StartOctet1;
                            EndOctet2 = StartOctet2;
                            EndOctet3 = StartOctet3;
                            EndOctet4 = StartOctet4;
                        }
                        break; // Znaleźliśmy IP, nie musimy czytać reszty pliku
                    }
                }
            }
            else
            {
                BaseIpAddress = "Not found .conf in ZIP";
            }
        }
        catch (Exception ex)
        {
            BaseIpAddress = $"File with Reading: {ex.Message}";
        }
    }
}