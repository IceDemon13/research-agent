using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DevExpress.Data;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Quartz;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Equipment;
using Telemart.Client.Core;
using Telemart.Client.Extensions;
using Telemart.Client.Jobs;
using Telemart.Client.Localizers;
using Telemart.Client.SingleInstance;
using Telemart.Client.ViewModels;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.Views;

namespace Telemart.Client
{
    /// <summary>
    ///     Interaction logic for App.xaml.
    /// </summary>
    public partial class App : ISingleInstanceApp
    {
        private readonly IHost _host;
        private ILogger<App> _logger;
        private IMessageFacadeService _messageFacadeService;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureTelemartHost()
                .Build();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            DirectoryInfo dirInfo = new(ApplicationFolders.ApplicationData);

            if (dirInfo.Exists)
            {
                foreach (DirectoryInfo dir in dirInfo.GetDirectories())
                {
                    dir.Delete(true);
                }
            }

            DevExpress.Mvvm.DXSplashScreenViewModel splashScreenViewModel = new DevExpress.Mvvm.DXSplashScreenViewModel
            {
                Copyright = "Copyright © 2016-2022 Telemart",
                IsIndeterminate = true,
                Logo = new Uri("/Images/Image.png", UriKind.Relative),
                Status = "Загрузка...",
                Title = "Telemart Client"
            };

            SplashScreenManager splashScreenManager = SplashScreenManager.CreateFluent(splashScreenViewModel);

            splashScreenManager.ShowOnStartup();
        }

        private IScheduler Scheduler { get; set; }

        public bool SignalExternalCommandLineArgs()
        {
            Dispatcher.Invoke(
                () =>
                {
                    if (MainWindow != null)
                    {
                        if (MainWindow.WindowState == WindowState.Minimized)
                        {
                            MainWindow.WindowState = WindowState.Normal;
                        }

                        MainWindow.Activate();
                    }
                });

            return true;
        }

        [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Stop Quartz")]
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            SingleInstanceAppProcessor singleInstanceAppProcessor = _host.Services.GetRequiredService<SingleInstanceAppProcessor>();

            singleInstanceAppProcessor.Dispose();

            _logger.LogInformation("Application exit ...");

            CloseHost().GetAwaiter().GetResult();

            async Task CloseHost()
            {
                await await Task.Factory.StartNew(
                    async () =>
                    {
                        if (Scheduler != null)
                        {
                            await Scheduler.Shutdown().ConfigureAwait(false);
                        }

                        await _host.StopAsync().ConfigureAwait(false);

                        if (_host is IAsyncDisposable asyncDisposable)
                        {
                            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            _host.Dispose();
                        }
                    }).ConfigureAwait(false);
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();
            _logger = _host.Services.GetRequiredService<ILogger<App>>();
            _messageFacadeService = _host.Services.GetRequiredService<IMessageFacadeService>();
            await _host.Services.GetRequiredService<SyncCacheJob>().StopAsync(default);

            StyleCommands.ServiceProvider = _host.Services;
            ContainerExtension.ServiceProvider = _host.Services;
            ApplicationSettings.ServiceProvider = _host.Services;
            DocumentManagerServiceExtensions.ServiceProvider = _host.Services;
            CarryTypeExtensions.ServiceProvider = _host.Services;
            TelemartViewModelBase.ServiceProvider = _host.Services;
            DiContainer.ServiceProvider = _host.Services;

            SingleInstanceAppProcessor instanceProcessor = _host.Services.GetRequiredService<SingleInstanceAppProcessor>();

            instanceProcessor.Init(this);

            IEquipmentSettingsStore equipmentSettingsStore = _host.Services.GetRequiredService<IEquipmentSettingsStore>();

            EquipmentSettingsInfo equipmentSettings = await equipmentSettingsStore.LoadAsync();

            ApplicationThemeHelper.ApplicationThemeName = equipmentSettings.ThemeName;

            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

            ExcelPackage.License.SetNonCommercialOrganization("Noncommercial organization");

            EditorLocalizer.Active = new TelemartEditorLocalizer();

            base.OnStartup(e);

            CultureInfo cultureInfo = new CultureInfo("ru-RU", false)
            {
                NumberFormat =
                {
                    NumberDecimalSeparator = ".",
                    CurrencyDecimalSeparator = ".",
                    CurrencySymbol = string.Empty,
                    CurrencyPositivePattern = 0
                },
            };

            CultureInfo.CurrentUICulture = cultureInfo;
            CultureInfo.CurrentCulture = cultureInfo;

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            ShellHelper.TryRemoveShortcut(instanceProcessor.AppUniqueName);

            ISchedulerFactory schedulerFactory = _host.Services.GetRequiredService<ISchedulerFactory>();

            Scheduler = await schedulerFactory.GetScheduler();

            await Scheduler.Start();

            MainWindow window = new MainWindow
            {
                DataContext = _host.Services.GetService<MainWindowViewModel>()
            };

            _logger.LogInformation("Application start ...");

            window.ShowDialog();
        }

        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Exception ex = e.Exception.InnerException;

            switch (ex)
            {
                case (Win32Exception win32Exception) when win32Exception.Message.Contains("The RPC server is unavailable"):
                case (InvalidOperationException invalidOperationException) when invalidOperationException.Message.Contains("Printer  is invalid"):
                    if (Application.Current.Dispatcher != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke(() => _messageFacadeService.ShowNotificationError("Ошибка печати"));
                    }
                    break;
            }

            _logger.LogError(e.Exception, "Task scheduler unhandled exception occurred");
        }

        private void AppOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var ex = e.Exception;

            if (ex is AggregateException aggregateException)
            {
                ex = aggregateException.InnerException;
            }

            switch (ex)
            {
                case (Win32Exception win32Exception) when win32Exception.Message.Contains("The RPC server is unavailable"):
                case (InvalidOperationException invalidOperationException) when invalidOperationException.Message.Contains("Printer  is invalid"):
                    if (Application.Current.Dispatcher != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke(() => _messageFacadeService.ShowNotificationError("Ошибка печати"));
                    }
                    break;
            }

            _logger.LogError(e.Exception, "Dispatcher unhandled exception occurred");
            e.Handled = true;
        }
    }
}