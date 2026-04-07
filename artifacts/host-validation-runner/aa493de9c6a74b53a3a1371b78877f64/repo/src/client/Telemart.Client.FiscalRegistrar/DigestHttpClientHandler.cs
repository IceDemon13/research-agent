using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Telemart.Client.FiscalRegistrar
{
    public class DigestHttpClientHandler : DelegatingHandler
    {
        private const int NONCE_COUNT = 1;
        private const string REALM = "Digest realm";
        private const string NONCE = "nonce";
        private const string QOP = "qop";

        private readonly string username;
        private readonly string password;

        private string realm;
        private string nonce;
        private string qop;

        private bool authenticated = false;
        private int attemptCount = 0;

        public DigestHttpClientHandler(string username, string password)
            : base(new HttpClientHandler())
        {
            this.username = username;
            this.password = password;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("TelemartClient", null));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

            if(authenticated)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Digest", GetDigestHeader(request.RequestUri.ToString(), request.Method));
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            GetDigestDataFromResponse(response);

            if(attemptCount < 3 && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                attemptCount++;
                return await SendAsync(request, cancellationToken);
            }

            attemptCount = 0;

            return response;
        }

        private string GetDigestHeader(string digestUri, HttpMethod method)
        {
            string cnonce = new Random()
                .Next(123400, 9999999)
                .ToString(CultureInfo.InvariantCulture);

            string hash1 = GenerateMD5($"{this.username}:{this.realm}:{this.password}");
            string hash2 = GenerateMD5($"{method}:{digestUri}");
            string digestResponse = GenerateMD5($"{hash1}:{this.nonce}:{NONCE_COUNT:00000000}:{cnonce}:{this.qop}:{hash2}");
            return $"username=\"{this.username}\"," +
                $"realm=\"{this.realm}\"," +
                $"nonce=\"{this.nonce}\"," +
                $"uri=\"{digestUri}\"," +
                $"algorithm=MD5," +
                $"response=\"{digestResponse}\"," +
                $"qop={this.qop}," +
                $"nc={NONCE_COUNT:00000000}," +
                $"cnonce=\"{cnonce}\"";
        }

        private static string GenerateMD5(string input)
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = MD5.HashData(inputBytes);
            StringBuilder stringBuilder = new StringBuilder();
            hash.ToList().ForEach(b => stringBuilder.Append(b.ToString("x2")));
            return stringBuilder.ToString();
        }

        private void GetDigestDataFromResponse(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("WWW-Authenticate", out IEnumerable<string> headers) && headers.Any())
            {
                IReadOnlyDictionary<string, string> wwwAuthenticateHeader = TransformHeaderToDictionary(headers.FirstOrDefault());

                wwwAuthenticateHeader.TryGetValue(REALM, out realm);
                wwwAuthenticateHeader.TryGetValue(NONCE, out nonce);
                wwwAuthenticateHeader.TryGetValue(QOP, out qop);

                authenticated = true;
            }
        }

        private static IReadOnlyDictionary<string, string> TransformHeaderToDictionary(string wwwAuthenticateHeader)
        {
            return wwwAuthenticateHeader
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('='))
                .ToDictionary(
                    split => split[0].Trim(),
                    split => split[1].Replace('"', ' ').Trim());
        }
    }
}