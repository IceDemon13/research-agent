using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Telemart.Client.Data
{
    public interface IResponseHandler
    {
        Task<Exception> HandleResponseAsync(object requestBody, HttpMethod method, string requestUri, byte[] responseBody, HttpStatusCode statusCode, HttpStatusCode successStatusCode);

        Task<Exception> HandleExceptionAsync(HttpMethod method, string uri, object requestBody, Exception errorException);
    }
}