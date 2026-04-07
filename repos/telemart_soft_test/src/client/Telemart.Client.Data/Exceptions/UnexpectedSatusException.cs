using System.Net;
using Telemart.Client.Core.Exceptions.Args;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Core.Exceptions
{
    public sealed class UnexpectedSatusException : GenericException<UnexpectedSatusExceptionArgs>
    {
        public UnexpectedSatusException(HttpStatusCode? httpStatusCode, Error error)
            : base(new UnexpectedSatusExceptionArgs(httpStatusCode, error), "An unexpected status code was returned.")
        {
        }
    }
}