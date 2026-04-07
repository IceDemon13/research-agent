using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Flurl;

namespace Telemart.Client.Data.Requests
{
    public abstract class RestClientGatewayRequestBase<TResponse> : IRestClientGatewayRequest<TResponse>
        where TResponse : class
    {
        protected RestClientGatewayRequestBase(HttpMethod method)
        {
            Method = method;
        }

        public object Body { get; protected set; }

        public TimeSpan? DefaultTimeout { get; protected set; }

        public TimeSpan? CacheTime { get; protected set; }

        public HttpStatusCode SuccessStatusCode { get; protected set; } = HttpStatusCode.OK;

        public IEnumerable<object> PathParameters { get; protected set; }

        public IEnumerable<(string Name, object Value)> UrlParameters { get; protected set; }

        public HttpMethod Method { get; protected set; }

        public virtual HttpRequestMessage BuildRequest()
        {
            Url url = "api/v1";

            foreach (object name in PathParameters ?? Enumerable.Empty<string>())
            {
                url.AppendPathSegment(name);
            }

            foreach ((string name, object value) in UrlParameters ?? Enumerable.Empty<(string, object)>())
            {
                url.SetQueryParam(name, value);
            }

            HttpRequestMessage request = new HttpRequestMessage();

            request.RequestUri = url.ToUri();
            request.Method = Method;

            return request;
        }
    }
}