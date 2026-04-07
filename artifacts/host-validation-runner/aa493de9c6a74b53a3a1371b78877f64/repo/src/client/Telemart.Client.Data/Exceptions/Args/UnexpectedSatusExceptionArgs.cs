using System.Linq;
using System.Net;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Core.Exceptions.Args
{
    public sealed class UnexpectedSatusExceptionArgs : ExceptionArgs
    {
        public UnexpectedSatusExceptionArgs(HttpStatusCode? httpStatusCode, Error error)
        {
            HttpStatusCode = httpStatusCode;
            Error = error;
        }

        public Error Error { get; }

        public HttpStatusCode? HttpStatusCode { get; }

        public override string Message
        {
            get
            {
                string message = string.Empty;
                string details = string.Empty;

                if (Error != null)
                {
                    message = Error.ErrorMessage;
                    details = Error.Details?.Any() == true ? $" ({string.Join(",", Error.Details)})" : string.Empty;
                }

                return $"{HttpStatusCode?.ToString("G")} {message}{details}";
            }
        }
    }
}