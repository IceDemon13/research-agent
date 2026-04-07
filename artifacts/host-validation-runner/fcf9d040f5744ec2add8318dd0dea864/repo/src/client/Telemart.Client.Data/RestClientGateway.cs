using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Telemart.Client.Data.Diagnostics;
using Telemart.Client.Data.Requests;
using Telemart.Client.Data.Stores;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Telemart.Client.Data
{
    public sealed class RestClientGateway : IRestClientGateway
    {
        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>();

        private readonly IResponseHandler _responseHandler;
        private readonly ICallStore _callStore;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly INetworkDiagnoser _networkDiagnoser;
        private readonly ILogger<RestClientGateway> _logger;

        public RestClientGateway(
            IResponseHandler responseHandler,
            ICallStore callStore,
            IHttpClientFactory httpClientFactory,
            INetworkDiagnoser networkDiagnoser,
            ILogger<RestClientGateway> logger)
        {
            _responseHandler = responseHandler;
            _callStore = callStore;
            _httpClientFactory = httpClientFactory;
            _networkDiagnoser = networkDiagnoser;
            _logger = logger;
        }

        public void AddHeader(string key, string value)
        {
            if (!_headers.ContainsKey(key))
            {
                _headers.Add(key, value);
            }
        }

        public void RemoveHeader(string key)
        {
            _headers.Remove(key);
        }

        public async Task<byte[]> ExecuteAsBytesAsync(IRestClientGatewayRequest request, Services service)
        {
            byte[] responseBytes = await ExecuteRestRequestAsync(request, service).ConfigureAwait(false);
            return responseBytes;
        }

        public async Task<TResponse> ExecuteAsync<TResponse>(IRestClientGatewayRequest<TResponse> request, Services service)
            where TResponse : class
        {
            byte[] responseBytes = await ExecuteRestRequestAsync(request, service).ConfigureAwait(false);

            JsonSerializer serializer = new JsonSerializer();

            using MemoryStream ms = new MemoryStream(responseBytes);

            using StreamReader sr = new StreamReader(ms);

            TResponse data = (TResponse)serializer.Deserialize(sr, typeof(TResponse));

            return data;
        }

        public Task ExecuteAsync(IRestClientGatewayRequest request, Services service)
        {
            return ExecuteRestRequestAsync(request, service);
        }

        private async Task<byte[]> ExecuteRestRequestAsync(IRestClientGatewayRequest telemartRestRequest, Services service)
        {
            using HttpClient httpClient = _httpClientFactory.CreateClient(service.ToString());

            if (telemartRestRequest.DefaultTimeout.HasValue)
            {
                httpClient.Timeout = telemartRestRequest.DefaultTimeout.Value;
            }

            HttpRequestMessage request = telemartRestRequest.BuildRequest();

            foreach (KeyValuePair<string, string> header in _headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            if (_callStore.CallId.HasValue)
            {
                request.Headers.Add(Headers.Call, _callStore.CallId.ToString());
            }

            HttpResponseMessage response = null;

            Exception exception = null;

            byte[] responseBytes;

            request.Headers.Add(Headers.Call, _callStore.CallId.ToString());

            using (LogContext.PushProperty("RequestId", Guid.NewGuid()))
            {
                _logger.LogInformation("Request started. Method: {Method}, Url: {url}", request?.Method, request?.RequestUri);

                Stopwatch requestSw = Stopwatch.StartNew();
                try
                {
                    if (telemartRestRequest.Body != null)
                    {
                        JsonSerializer serializer = new JsonSerializer();

                        using MemoryStream ms = new MemoryStream();
                        await using StreamWriter writer = new StreamWriter(ms);

                        serializer.Serialize(writer, telemartRestRequest.Body);

                        await writer.FlushAsync().ConfigureAwait(false);
                        await ms.FlushAsync().ConfigureAwait(false);

                        ms.Seek(0, SeekOrigin.Begin);

                        StreamContent sc = new StreamContent(ms);

                        sc.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

                        request.Content = sc;

                        response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                            .ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    exception = await _responseHandler.HandleExceptionAsync(
                        request.Method,
                        request.RequestUri!.ToString(),
                        telemartRestRequest.Body,
                        e).ConfigureAwait(false);
                }
                finally
                {
                    requestSw.Stop();
                }

                Stopwatch responseSw = Stopwatch.StartNew();

                responseBytes = await response?.Content?.ReadAsByteArrayAsync();

                responseSw.Stop();

                _networkDiagnoser.SetDownloadTime(responseBytes.LongLength, responseSw.Elapsed.TotalMilliseconds);

                _logger.LogInformation("Request finished. Method: {Method}, Url: {url}, Processing time: {ProcessingTime}, Response fetch time: {ResponseFetchTime}", request?.Method, request?.RequestUri, requestSw.Elapsed, responseSw.Elapsed);
            }

            exception ??= await _responseHandler.HandleResponseAsync(
                telemartRestRequest.Body,
                request.Method,
                request.RequestUri?.ToString(),
                responseBytes,
                response!.StatusCode,
                telemartRestRequest.SuccessStatusCode).ConfigureAwait(false);

            if (exception != null)
            {
                throw exception;
            }

            return responseBytes;
        }
    }
}