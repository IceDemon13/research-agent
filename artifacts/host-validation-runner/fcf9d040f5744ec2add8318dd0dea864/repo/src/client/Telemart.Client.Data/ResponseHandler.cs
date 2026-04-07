using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telemart.Client.Core.Exceptions;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data
{
    public class ResponseHandler : IResponseHandler
    {
        private readonly ILogger<ResponseHandler> _logger;

        public ResponseHandler(ILogger<ResponseHandler> logger)
        {
            _logger = logger;
        }

        public async Task<Exception> HandleResponseAsync(object requestBody, HttpMethod method, string requestUri, byte[] responseBody, HttpStatusCode statusCode, HttpStatusCode successStatusCode)
        {
            if (successStatusCode != statusCode)
            {
                Error error;

                switch (statusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        error = new Error()
                        {
                            ErrorCode = ErrorCode.None,
                            ErrorMessage = "Authentication failed",
                            Details = new List<Error>()
                        };
                        break;
                    case HttpStatusCode.Forbidden:
                        error = new Error
                        {
                            ErrorCode = ErrorCode.None,
                            ErrorMessage = "Not enough permissions to perform the action",
                            Details = new List<Error>()
                        };
                        break;
                    case HttpStatusCode.NotFound:
                        error = new Error
                        {
                            ErrorCode = ErrorCode.None,
                            ErrorMessage = "Resource not found",
                            Details = new List<Error>()
                        };
                        break;
                    case HttpStatusCode.ServiceUnavailable:
                        error = new Error()
                        {
                            ErrorCode = ErrorCode.None,
                            ErrorMessage = "Service unavailable",
                            Details = new List<Error>()
                        };
                        break;
                    case HttpStatusCode.PreconditionFailed:
                        error = new Error()
                        {
                            ErrorCode = ErrorCode.OldClient,
                            ErrorMessage = "У вас устаревшая версия клиента. Обновите клиент",
                            Details = new List<Error>()
                        };
                        break;
                    default:
                        string content = Encoding.UTF8.GetString(responseBody ?? Array.Empty<byte>());
                        error = GetError(content);
                        break;
                }

                UnexpectedSatusException exception = new UnexpectedSatusException(statusCode, error);

                string requestContent = null;

                if (requestBody != null)
                {
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(1000);
                    int readBytes = 0;

                    using (MemoryStream stream = new MemoryStream(1000))
                    {
                        await using (StreamWriter writer = new StreamWriter(stream, leaveOpen: true))
                        {
                            JsonSerializer serializer = new JsonSerializer();

                            serializer.Serialize(writer, requestBody);

                            await writer.FlushAsync();
                        }

                        readBytes = await stream.ReadAsync(buffer, 0, 1000);
                    }

                    if (readBytes > 0)
                    {
                        requestContent = Encoding.UTF8.GetString(buffer, 0, readBytes);
                    }

                    ArrayPool<byte>.Shared.Return(buffer);
                }

                string resource =
                    $"{method} {requestUri}{Environment.NewLine}{requestContent}";

                _logger.LogError(
                    exception,
                    "Response handling failed: {Resource} ({ErrorMessage})",
                    resource,
                    error?.ErrorMessage);

                return exception;
            }

            return null;
        }

        public async Task<Exception> HandleExceptionAsync(HttpMethod method, string uri, object requestBody, Exception errorException)
        {
            string requestContent = null;

            if (requestBody != null)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(1000);
                int readBytes = 0;

                using (MemoryStream stream = new MemoryStream(1000))
                {
                    await using (StreamWriter writer = new StreamWriter(stream))
                    {
                        JsonSerializer serializer = new JsonSerializer();

                        serializer.Serialize(writer, requestBody);

                        await writer.FlushAsync();

                        readBytes = await stream.ReadAsync(buffer, 0, 1000);
                    }
                }

                if (readBytes > 0)
                {
                    requestContent = Encoding.UTF8.GetString(buffer, 0, readBytes);
                }

                ArrayPool<byte>.Shared.Return(buffer);
            }

            string resource = $"{method} {uri}";

            _logger.LogError(errorException, "Request send failed: {Resource} ({ErrorMessage})\n{RequestContent}", resource, errorException.Message, requestContent);

            UnexpectedErrorException exception = new UnexpectedErrorException(
                $"Error: {resource} ({errorException.Message})",
                errorException);

            return exception;
        }

        private Error GetError(string content)
        {
            Error error;

            try
            {
                error = JsonConvert.DeserializeObject<Error>(content);
            }
            catch (JsonSerializationException exception)
            {
                _logger.LogError(exception, "Unable to deserialize error content {Content}", content);

                error = new Error
                {
                    ErrorCode = ErrorCode.None,
                    ErrorMessage = "Unable to deserialize error content",
                    Details = new List<Error> { new Error { ErrorMessage = content } }
                };
            }

            return error;
        }
    }
}