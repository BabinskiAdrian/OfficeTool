using OfficeTool.Core.BackupConfigs.Models;
using OfficeTool.Core.BackupConfigs.Services;
using OfficeTool.Core.Shared;
using System.IO.Compression;
using System.Text;

namespace OfficeTool.Infrastructure.BackupConfigs;

public class ScalanceConfigService : IScalanceConfigService
{
    public async Task GenerateConfigsAsync(BackupConfigParameters parameters, IProgress<string> progressReporter)
    {
        string tempExtractPath = Path.Combine(Path.GetTempPath(), "OfficeTool_ZipTemp_" + Guid.NewGuid().ToString());

        try
        {
            if (Directory.Exists(tempExtractPath)) Directory.Delete(tempExtractPath, true);
            Directory.CreateDirectory(tempExtractPath);

            ZipFile.ExtractToDirectory(parameters.BaseZipPath, tempExtractPath);

            string[] confFiles = Directory.GetFiles(tempExtractPath, "*.conf", SearchOption.AllDirectories);
            if (confFiles.Length == 0)
                throw new FileNotFoundException("W bazowym archiwum ZIP nie znaleziono pliku konfiguracyjnego (.conf).");

            string confFilePath = confFiles[0];
            string[] originalLines = await File.ReadAllLinesAsync(confFilePath);

            string baseDeviceName = "Ca0001";
            foreach (var line in originalLines)
            {
                if (line.StartsWith("System Name="))
                {
                    baseDeviceName = line.Substring(12).Trim();
                    break;
                }
            }

            string namePrefix = baseDeviceName.Substring(0, baseDeviceName.Length - 2);
            int currentNameNumber = int.Parse(baseDeviceName.Substring(baseDeviceName.Length - 2));

            int ip1 = parameters.StartIp[0];
            int ip2 = parameters.StartIp[1];
            int ip3 = parameters.StartIp[2];
            int ip4 = parameters.StartIp[3];

            for (int i = 0; i < parameters.TotalFilesToGenerate; i++)
            {
                progressReporter?.Report($"Generowanie konfiguracji: plik {i + 1} z {parameters.TotalFilesToGenerate}...");

                string currentIp = $"{ip1}.{ip2}.{ip3}.{ip4}";
                string broadcastIp = $"{ip1}.{ip2}.{ip3}.255";
                string currentName = $"{namePrefix}{currentNameNumber:D2}";

                string newContent = GenerateModifiedConfig(originalLines, currentName, currentIp, broadcastIp);

                await File.WriteAllTextAsync(confFilePath, newContent, new UTF8Encoding(false));

                string destZipName = $"configpack_SCALANCE_W700_{currentName}.zip";
                string destZipPath = Path.Combine(parameters.DestinationFolderPath, destZipName);

                if (File.Exists(destZipPath)) File.Delete(destZipPath);
                ZipFile.CreateFromDirectory(tempExtractPath, destZipPath);

                currentNameNumber++;
                ip4++;
                if (ip4 > 254)
                {
                    ip4 = 1;
                    ip3++;
                }
            }
        }
        finally
        {
            if (Directory.Exists(tempExtractPath))
            {
                Directory.Delete(tempExtractPath, true);
            }
        }
    }
    private string GenerateModifiedConfig(string[] lines, string newName, string newIp, string broadcastIp)
    {
        StringBuilder sb = new StringBuilder();
        string currentContextName = string.Empty;

        foreach (string line in lines)
        {
            if (line.StartsWith("END OF HEADER - Checksum:"))
            {
                sb.Append($"END OF HEADER - Checksum:{ChecksumHelper.CalculateScalanceChecksum(sb.ToString())}\n");
                continue;
            }
            if (line.StartsWith("END OF FILE - Checksum:"))
            {
                sb.Append($"END OF FILE - Checksum:{ChecksumHelper.CalculateScalanceChecksum(sb.ToString())}");
                continue;
            }

            int separatorIndex = line.IndexOf('=');
            if (separatorIndex == -1)
            {
                sb.Append(line + "\n");
                continue;
            }

            string key = line.Substring(0, separatorIndex);
            string originalValue = line.Substring(separatorIndex + 1);

            switch (key)
            {
                case "System Name":
                    sb.Append($"System Name={newName}\n");
                    break;

                case "Ip Address (In-/Out-Band)":
                    sb.Append($"Ip Address (In-/Out-Band)={newIp}/0.0.0.0\n");
                    break;

                case "Name":
                    currentContextName = originalValue.Trim();
                    sb.Append(line + "\n");
                    break;

                case "Value":
                    switch (currentContextName)
                    {
                        case "sysName":
                            sb.Append($"Value={newName}\n");
                            break;
                        case "snMspsIfIpAddr":
                        case "automationSystemIpAddress":
                            sb.Append($"Value={newIp}\n");
                            break;
                        case "snMspsIfIpBroadcastAddr":
                            sb.Append($"Value={broadcastIp}\n");
                            break;
                        default:
                            sb.Append(line + "\n");
                            break;
                    }
                    break;

                default:
                    sb.Append(line + "\n");
                    break;
            }
        }

        return sb.ToString().TrimEnd('\n', '\r');
    }
}
