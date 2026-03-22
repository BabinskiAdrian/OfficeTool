using System;
using System.Collections.Generic;
using System.Text;

namespace OfficeTool.Core.BackupConfigs.Models;

public class BackupConfigParameters
{
    public string BaseZipPath { get; set; } = string.Empty;
    public string DestinationFolderPath { get; set; } = string.Empty;

    // Zapisujemy początkowe IP jako tablicę 4 liczb, co ułatwi nam matematykę
    public int[] StartIp { get; set; } = new int[4];

    // Silnik nie musi wiedzieć, czy użytkownik wybrał "Ilość" czy "Zakres". 
    // UI wyliczyło całkowitą pulę plików i tylko to nas interesuje.
    public int TotalFilesToGenerate { get; set; }
}
