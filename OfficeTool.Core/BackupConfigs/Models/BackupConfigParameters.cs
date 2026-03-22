using System;
using System.Collections.Generic;
using System.Text;

namespace OfficeTool.Core.BackupConfigs.Models;

public class BackupConfigParameters
{
    public string BaseZipPath { get; set; } = string.Empty;
    public string DestinationFolderPath { get; set; } = string.Empty;
    public int[] StartIp { get; set; } = new int[4];
    public int TotalFilesToGenerate { get; set; }
    public bool SkipBaseFile { get; set; }
}
