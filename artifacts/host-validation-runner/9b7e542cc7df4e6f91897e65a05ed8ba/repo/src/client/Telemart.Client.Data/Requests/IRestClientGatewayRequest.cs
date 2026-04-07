using System;
using System.Net;
using System.Net.Http;

namespace Telemart.Client.Data.Requests
{
    public interface IRestClientGatewayRequest
    {
        HttpStatusCode SuccessStatusCode { get; }

        TimeSpan? DefaultTimeout { get; }

        object Body { get; }

        TimeSpan? CacheTime { get; }

        HttpRequestMessage BuildRequest();
    }

    public interface IRestClientGatewayRequest<T> : IRestClientGatewayRequest
        where T : class
    {
    }
}