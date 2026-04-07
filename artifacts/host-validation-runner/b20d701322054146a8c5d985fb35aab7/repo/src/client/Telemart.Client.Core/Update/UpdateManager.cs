using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Telemart.Client.Core.Update
{
    public sealed class UpdateManager : IUpdateManager
    {
        private readonly Lazy<Version> _appVersion = new Lazy<Version>(GetAppVersion);
        private readonly UpdateManagerOptions _options;
        private readonly ILogger<UpdateManager> _logger;
        private readonly string _updateExe;

        public UpdateManager(UpdateManagerOptions options, ILogger<UpdateManager> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _logger = logger;

            string currentDirectory = Path.GetDirectoryName(GetType().Assembly.Location);
            _updateExe = Path.Combine(currentDirectory ?? throw new InvalidOperationException(), "..", "Update.exe");
        }

        public Task<WinCheckForUpdateResult?> CheckIfUpdateAvailableAsync()
        {
            if (File.Exists(_updateExe))
            {
                return Task.Run<WinCheckForUpdateResult?>(() =>
                {
                    string textResult = null;
                    ProcessStartInfo pi = new ProcessStartInfo(_updateExe, $"--checkForUpdate={_options.BaseAddress}")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    };

                    Process p = new Process();
                    p.StartInfo = pi;
                    p.OutputDataReceived += (s, e) =>
                    {
                        Debug.WriteLine($"Checking: {e.Data}");
                        if (e.Data?.StartsWith("{") ?? false)
                        {
                            textResult = e.Data;
                        }
                    };

                    p.Start();
                    p.BeginOutputReadLine();
                    p.WaitForExit();

                    if (textResult != null)
                    {
                        return JsonConvert.DeserializeObject<WinCheckForUpdateResult>(textResult);
                    }

                    throw new CheckUpdateException("Failed to check update");
                });
            }
            else
            {
                _logger.LogInformation("Update.exe should be located at {Update}", _updateExe);
            }

            return Task.FromResult<WinCheckForUpdateResult?>(null);
        }

        public Task UpdateAsync(IProgress<int> progress)
        {
            if (File.Exists(_updateExe))
            {
                return Task.Run(() =>
                {
                    ProcessStartInfo pi = new ProcessStartInfo(_updateExe, $"--update={_options.BaseAddress}")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    };

                    Process p = new Process();
                    p.StartInfo = pi;
                    p.OutputDataReceived += (s, e) =>
                    {
                        Debug.WriteLine($"Updating: {e.Data}");

                        if (int.TryParse(e.Data, out int progressPercent))
                        {
                            progress.Report(progressPercent);
                        }
                    };
                    p.Start();
                    p.BeginOutputReadLine();
                    p.WaitForExit();
                });
            }
            else
            {
                _logger.LogInformation("Update.exe should be located at {Update}", _updateExe);
            }

            return Task.CompletedTask;
        }

        public Task RunLatestVersionAsync()
        {
            if (File.Exists(_updateExe))
            {
                return Task.Run(() =>
                {
                    ProcessStartInfo pi = new ProcessStartInfo(_updateExe, $"--processStart=Telemart.Client.exe")
                        {
                            RedirectStandardOutput = true,
                            UseShellExecute = false
                        };
                    Process p = new Process();

                    p.StartInfo = pi;
                    p.OutputDataReceived += (s, e) =>
                    {
                        Debug.WriteLine($"Running: {e.Data}");
                    };
                    p.Start();
                    p.BeginOutputReadLine();
                    p.WaitForExit();
                });
            }
            else
            {
                _logger.LogInformation("Update.exe should be located at {Update}", _updateExe);
                Process.Start(Assembly.GetEntryAssembly()?.Location.Replace(".dll", ".exe") ?? throw new InvalidOperationException());
            }

            return Task.CompletedTask;
        }

        public Version GetCurrentVersion()
        {
            return _appVersion.Value;
        }

        public Version GetCurrentAssemblyVersion()
        {
            return _appVersion.Value;
        }

        private static Version GetAppVersion()
        {
            string appVersion = (Assembly.GetEntryAssembly() ?? throw new InvalidOperationException()).GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.Split('+')[0];
            return new Version(appVersion);
        }
    }
}