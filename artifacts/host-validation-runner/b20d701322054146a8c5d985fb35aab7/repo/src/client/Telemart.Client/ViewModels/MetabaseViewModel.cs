using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.WorkAccount;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels
{
    public class MetabaseViewModel : TelemartViewModelBase
    {
        private readonly MetabaseOptions _metabaseOptions;

        private WebView2 _webView2;
        private IReadOnlyCollection<Cookie> _cookies;
        private EmployeeAccountDto _employeeAccountDto;
        private bool _authorized;
        private bool _authorizationCredentialsFilled;

        public MetabaseViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            MetabaseOptions metabaseOptions,
            ILogger<MetabaseViewModel> logger)
            : base(webClient, dictionaries, messageFacadeService, logger)
        {
            _metabaseOptions = metabaseOptions;

            LoadWebView2Command = new DelegateCommand<RoutedEventArgs>(WebView2Loaded);
            NavigationStartingCommand = new AsyncCommand<CoreWebView2NavigationStartingEventArgs>(NavigationStartingAsync);
        }

        public IDelegateCommand LoadWebView2Command { get; }

        public IAsyncCommand NavigationStartingCommand { get; }

        public string NavigationUrl
        {
            get { return GetProperty(() => NavigationUrl); }
            private set { SetProperty(() => NavigationUrl, value); }
        }

        protected override async Task HandleLoadedAsync()
        {
            SplashScreenManager splashScreenManager = SplashScreenManager.CreateWaitIndicator();
            splashScreenManager.ViewModel.Status = "Авторизация...";
            splashScreenManager.Show();

            _employeeAccountDto = await GetEmployeeAccountInfoAsync();

            NavigationUrl = _metabaseOptions.BaseAddress;

            _authorizationCredentialsFilled = !string.IsNullOrEmpty(_employeeAccountDto?.Login) && !string.IsNullOrEmpty(_employeeAccountDto?.Password);

            if (!_authorizationCredentialsFilled)
            {
                MessageFacadeService.ShowNotificationWarning("У вас нет аккаунта Metabase");
            }

            splashScreenManager.Close();
        }

        protected override Task HandleUnloadedAsync()
        {
            _webView2.Dispose();
            return base.HandleUnloadedAsync();
        }

        private void WebView2Loaded(RoutedEventArgs eventArgs)
        {
            if (eventArgs.Source is WebView2 view2)
            {
                _webView2 = view2;
            }
        }

        private async Task NavigationStartingAsync(CoreWebView2NavigationStartingEventArgs eventArgs)
        {
            if (_webView2 is null || _authorized)
            {
                return;
            }

            if (_cookies?.Any() != true && _authorizationCredentialsFilled)
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
            CookieContainer cookieContainer = new CookieContainer();

            HttpClientHandler handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer
            };

            HttpClient client = new HttpClient(handler);

            client.BaseAddress = new(_metabaseOptions.BaseAddress);

            var loginData = new
            {
                username = _employeeAccountDto.Login,
                password = _employeeAccountDto.Password,
                remember = true
            };

            string json = JsonConvert.SerializeObject(loginData);

            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("/api/session", content);

            if (response.IsSuccessStatusCode)
            {
                CookieCollection cookies = cookieContainer.GetCookies(client.BaseAddress);

                MessageFacadeService.ShowNotificationInfo("Авторизация успешна, ожидайте загрузку");

                return cookies;
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

            webView2.CoreWebView2.Navigate(NavigationUrl);
        }

        private async Task<EmployeeAccountDto> GetEmployeeAccountInfoAsync()
        {
            EmployeeRichDto employee = await WebClient.ExecuteApiRequestAsync(new QueryEmployee(WebClient.AuthenticatedEmployee.Id));

            return employee!.Accounts?.FirstOrDefault(x => x.AccountId == WorkAccountIds.MetabaseId);
        }
    }
}