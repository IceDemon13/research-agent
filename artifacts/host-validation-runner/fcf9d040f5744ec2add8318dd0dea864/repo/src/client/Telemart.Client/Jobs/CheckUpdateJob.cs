using System;
using System.Threading.Tasks;
using System.Windows;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Quartz;
using Telemart.Client.Common.Messages;
using Telemart.Client.Core.Update;

namespace Telemart.Client.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class CheckUpdateJob : IJob
    {
        public CheckUpdateJob(IUpdateManager updateManager, IMessenger messenger, ILogger<CheckUpdateJob> logger)
        {
            UpdateManager = updateManager ?? throw new ArgumentNullException(nameof(updateManager));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Logger = logger;
        }

        private ILogger<CheckUpdateJob> Logger { get; }

        private IMessenger Messenger { get; }

        private IUpdateManager UpdateManager { get; }

        public async Task Execute(IJobExecutionContext context)
        {
            Logger.LogInformation("Start checking update");

            try
            {
                WinCheckForUpdateResult? result = await UpdateManager.CheckIfUpdateAvailableAsync();

                if (result?.ReleasesToApply?.Length > 0)
                {
                    Application.Current.Dispatcher.BeginInvoke(() => { Messenger.Send(new UpdateAvailableMessage(result.Value)); });
                }

                Logger.LogInformation("Finish checking update");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to check update");
            }
        }
    }
}