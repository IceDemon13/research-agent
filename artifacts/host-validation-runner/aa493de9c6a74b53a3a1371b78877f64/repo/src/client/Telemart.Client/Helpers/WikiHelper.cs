using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;

namespace Telemart.Client.Helpers
{
    public sealed class WikiHelper : IWikiHelper
    {
        private const string _login = "/login";
        private const string _email = "email";
        private const string _password = "password";
        private const string _token = nameof(_token);
        private const string _setCookie = "set-cookie";
        private const string _xsrfToken = "XSRF-TOKEN";
        private const string _bookstackSession = "bookstack_session";
        private string _baseUrl;

        public WikiHelper(ILogger<WikiHelper> logger)
        {
            WikiLogger = logger;

            HtmlParser = new HtmlParser();
        }

        private HtmlParser HtmlParser { get; }

        private ILogger<WikiHelper> WikiLogger { get; }

        public async Task<CookieWiki> GetCookieAutorisationAsync(string url, string email, string password)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return null;
            }

            _baseUrl = url;

            try
            {
                HttpResponseMessage loginResponse = await GetLoginPageAsync();

                string responseLogin = await loginResponse.Content.ReadAsStringAsync();

                string token = ParseToken(responseLogin);

                return await AuthorizationAsync(GetCookieFromHeadersForAuthorization(loginResponse.Headers), token, email, password);
            }
            catch (Exception ex)
            {
                WikiLogger.LogError(ex, "Failed to get authorizations cookie for user {USer}", email);
            }

            return null;
        }

        private string ParseToken(string response)
        {
            IHtmlHeadElement headElement = HtmlParser.ParseHead(response);

            IElement elementWithToken = headElement?.Children.FirstOrDefault(x => x is IHtmlMetaElement metaElement && metaElement.Name == "token");

            IHtmlMetaElement metaElementToken = (IHtmlMetaElement)elementWithToken;

            return metaElementToken?.Content;
        }

        private async Task<HttpResponseMessage> GetLoginPageAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetAsync(_baseUrl);
            }
        }

        private async Task<CookieWiki> AuthorizationAsync((string XsrfToken, string BookstackSession) cookies, string token, string userEmail, string userPassword)
        {
            try
            {
                CookieContainer cookieContainer = new CookieContainer();

                using (HttpClientHandler handler = new HttpClientHandler { CookieContainer = cookieContainer })
                using (HttpClient client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri(_baseUrl);

                    Dictionary<string, string> data = new Dictionary<string, string>
                    {
                        { _token, token },
                        { _email, userEmail },
                        { _password, userPassword }
                    };

                    cookieContainer.SetCookies(new Uri(_baseUrl), cookies.XsrfToken);
                    cookieContainer.SetCookies(new Uri(_baseUrl), cookies.BookstackSession);

                    HttpResponseMessage response = await client.PostAsync(_login, new FormUrlEncodedContent(data));

                    if (response.IsSuccessStatusCode)
                    {
                        CookieCollection cookieCollection = handler.CookieContainer.GetCookies(new Uri(_baseUrl));

                        return CreateCookieWiki(cookieCollection);
                    }

                    WikiLogger.LogWarning("Failed sending authorizations request for {User}. Status code not success.", userEmail);
                }
            }
            catch (Exception ex)
            {
                WikiLogger.LogError(ex, "Failed sending authorizations request for {User}", userEmail);
            }

            return null;
        }

        private (string, string) GetCookieFromHeadersForAuthorization(HttpResponseHeaders headers)
        {
            IEnumerable<string> cookie = new List<string>();

            if (headers?.TryGetValues(_setCookie, out cookie) == true)
            {
                string xsrfToken = cookie.FirstOrDefault(x => x.Contains(_xsrfToken));

                string bookstackSession = cookie.FirstOrDefault(x => x.Contains(_bookstackSession));

                return (xsrfToken, bookstackSession);
            }

            return (string.Empty, string.Empty);
        }

        private CookieWiki CreateCookieWiki(CookieCollection cookieCollection)
        {
            if (cookieCollection.Count == 2)
            {
                return new CookieWiki(cookieCollection.ToReadOnlyCollection());
            }

            return null;
        }
    }
}