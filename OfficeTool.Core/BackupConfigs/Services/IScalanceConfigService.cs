using OfficeTool.Core.BackupConfigs.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficeTool.Core.BackupConfigs.Services;

public interface IScalanceConfigService
{
    Task GenerateConfigsAsync(BackupConfigParameters parameters, IProgress<string> progressReporter);
}
