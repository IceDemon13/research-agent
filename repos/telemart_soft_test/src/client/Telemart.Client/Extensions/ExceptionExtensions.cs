using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using Telemart.Client.Core.ErrorHandling;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.Extensions
{
    public static class ExceptionExtensions
    {
        public static IReadOnlyCollection<ValidationResultItem> GetErrorItems(this UnexpectedSatusException exception, bool isError = true)
        {
            ValidationResultItem[] errorItems;

            switch (exception.Args.HttpStatusCode)
            {
                case HttpStatusCode.BadRequest:
                    errorItems = exception.Args.Error.HasErrorCodeMessage()
                        ? new[] { new ValidationResultItem(exception.Args.Error.GetErrorMessage(), isError) }
                        : exception.Args.Error.Details
                            .Select(x => new ValidationResultItem(x.ErrorMessage, (x.InnerError == null || JObject.FromObject(x.InnerError)["severity"] == null || JObject.FromObject(x.InnerError)["severity"]?.Value<string>()?.Equals("error", StringComparison.OrdinalIgnoreCase) == true) && isError))
                            .ToArray();
                    break;
                case HttpStatusCode.Forbidden:
                    errorItems = new[] { new ValidationResultItem(Resources.ErrorForbidden, isError) };
                    break;
                case HttpStatusCode.InternalServerError:
                    errorItems = new[] { new ValidationResultItem($"Внутренняя ошибка сервера: {exception.Args.Error.ErrorMessage}", isError) };
                    break;
                case HttpStatusCode.ServiceUnavailable:
                    errorItems = new[] { new ValidationResultItem(Resources.ServerUnavailable, isError) };
                    break;
                default:
                    errorItems = new[] { new ValidationResultItem(exception.Message, isError) };
                    break;
            }

            return errorItems;
        }
    }
}