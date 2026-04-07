using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.ModuleAnalytics;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.WorkAccount;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.ModuleAnalytics
{
    public sealed class ModuleAnalyticsViewModel : TelemartDialogViewModelBase
    {
        private readonly IModuleAnalyticsSettingsStore _moduleAnalyticsSettingsStore;
        private readonly MetabaseOptions _metabaseOptions;
        private WebView2 _webView2;
        private IReadOnlyCollection<Cookie> _cookies;
        private bool _authorized;
        private ModuleAnalyticsParameter _parameter;

        public ModuleAnalyticsViewModel(
            MetabaseOptions metabaseOptions,
            IWebClient webClient,
            IMessenger messenger,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IModuleAnalyticsSettingsStore moduleAnalyticsSettingsStore)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _metabaseOptions = metabaseOptions;
            _moduleAnalyticsSettingsStore = moduleAnalyticsSettingsStore;
            LoadWebView2Command = new DelegateCommand<RoutedEventArgs>(WebView2Loaded);
            NavigationStartingCommand = new AsyncCommand<CoreWebView2NavigationStartingEventArgs>(NavigationStartingAsync);

            messenger.Register<ModuleAnalyticsCloseMessage>(this, x =>
            {
                if (x.EntityId == _parameter?.EntityId)
                {
                    CloseOk();
                }
            });
        }

        public IDelegateCommand LoadWebView2Command { get; }

        public IAsyncCommand NavigationStartingCommand { get; }

        public override int Height => _parameter?.Height ?? 0;

        public override int MinHeight => _parameter?.Height ?? 0;

        public override int MinWidth => _parameter?.Width ?? 0;

        public override int Width => _parameter?.Width ?? 0;

        public override int MaxWidth => 1920;

        public override int MaxHeight => 1080;

        public string Url
        {
            get { return GetProperty(() => Url); }
            private set { SetProperty(() => Url, value); }
        }

        public double Top
        {
            get { return GetProperty(() => Top); }
            set { SetProperty(() => Top, value); }
        }

        public double Left
        {
            get { return GetProperty(() => Left); }
            set { SetProperty(() => Left, value); }
        }

        protected override async Task HandleLoadedAsync()
        {
            _parameter = (ModuleAnalyticsParameter)Parameter;

            RaisePropertiesChanged(
                nameof(Height),
                nameof(MinHeight),
                nameof(MinWidth),
                nameof(Width));

            Url = _parameter.Url;

            Title = _parameter.Title;
            Top = _parameter.Top;
            Left = _parameter.Left;

            await base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }

        private void WebView2Loaded(RoutedEventArgs eventArgs)
        {
            if (eventArgs.Source is WebView2)
            {
                _webView2 = (WebView2)eventArgs.Source;

                _webView2.ZoomFactor = 0.75;
            }
        }

        public override async void OnClose(CancelEventArgs e)
        {
            try
            {
                CurrentWindowService currentWindowService = (CurrentWindowService)GetService<ICurrentWindowService>(ServiceSearchMode.PreferLocal);

                if (currentWindowService is null)
                {
                    return;
                }

                ModuleAnalyticsSettings settings = await _moduleAnalyticsSettingsStore.LoadAsync();

                ModuleAnalyticsSetting currentAnalyticsSettings = settings.Settings.GetValueOrDefault(_parameter.ViewModelName);

                settings.Settings[_parameter.ViewModelName] = new ModuleAnalyticsSetting
                {
                    PositionId = currentAnalyticsSettings?.PositionId ?? ModuleAnalyticsPosition.Maximized.Id,
                    LocationId = currentAnalyticsSettings?.LocationId ?? ModuleAnalyticsLocation.Horizontal.Id,
                    Top = (int)currentWindowService.ActualWindow.Top,
                    Left = (int)currentWindowService.ActualWindow.Left,
                    Width = (int)currentWindowService.ActualWindow.Width,
                    Height = (int)currentWindowService.ActualWindow.Height
                };

                await _moduleAnalyticsSettingsStore.SaveAsync(settings);

                _webView2.Dispose();

                base.OnClose(e);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save analytics window position");
            }
        }

        private async Task NavigationStartingAsync(CoreWebView2NavigationStartingEventArgs eventArgs)
        {
            if (_webView2 is null || _authorized)
            {
                return;
            }

            if (_cookies?.Any() != true)
            {
                _cookies = await GetCookiesAsync();
            }

            if (_cookies is null)
            {
                return;
            }

            SetCookieInWebView2();

            _authorized = true;
        }

        private async Task<IReadOnlyCollection<Cookie>> GetCookiesAsync()
        {
            EmployeeAccountDto metabasePersonalAccount = WebClient.AuthenticatedEmployeeFullData.Accounts?.FirstOrDefault(x => x.AccountId == WorkAccountIds.MetabaseId);

            string login = WebClient.AuthenticatedEmployeeFullData.GenericAccounts?.Metabase?.Login ?? metabasePersonalAccount?.Login;
            string password = WebClient.AuthenticatedEmployeeFullData.GenericAccounts?.Metabase?.Password ?? metabasePersonalAccount?.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageFacadeService.ShowNotificationError("Не указаны логин и пароль для авторизации в Metabase");
                CloseOk();
                return [];
            }

            SplashScreenManager splashScreenManager = SplashScreenManager.CreateWaitIndicator();
            splashScreenManager.ViewModel.Status = "Авторизация...";
            splashScreenManager.Show();

            CookieContainer cookieContainer = new CookieContainer();

            HttpClientHandler handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer
            };

            HttpClient client = new HttpClient(handler);

            client.BaseAddress = new(_metabaseOptions.BaseAddress);

            var loginData = new
            {
                username = login,
                password = password,
                remember = true
            };

            string json = JsonConvert.SerializeObject(loginData);

            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("/api/session", content);

            splashScreenManager.Close();

            if (response.IsSuccessStatusCode)
            {
                return cookieContainer.GetCookies(client.BaseAddress);
            }

            MessageFacadeService.ShowNotificationError("Ошибка авторизации в Metabase");
            return Array.Empty<Cookie>();
        }

        private void SetCookieInWebView2()
        {
            WebView2 webView2 = _webView2;

            if (_cookies?.Any() == true)
            {
                foreach (Cookie cookie in _cookies)
                {
                    CoreWebView2Cookie webView2Cookie = webView2.CoreWebView2.CookieManager.CreateCookieWithSystemNetCookie(cookie);

                    webView2Cookie.IsHttpOnly = false;
                    webView2Cookie.IsSecure = false;

                    webView2.CoreWebView2.CookieManager.AddOrUpdateCookie(webView2Cookie);
                }
            }

            webView2.CoreWebView2.Navigate(Url);
        }
    }
}