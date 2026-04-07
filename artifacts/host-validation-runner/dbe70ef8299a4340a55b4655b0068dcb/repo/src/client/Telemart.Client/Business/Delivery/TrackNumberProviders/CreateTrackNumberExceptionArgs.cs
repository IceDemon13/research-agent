using System;
using System.Collections.Generic;
using Telemart.Client.Core.Exceptions;

namespace Telemart.Client.Business.Delivery.TrackNumberProviders
{
    public sealed class CreateTrackNumberExceptionArgs : ExceptionArgs
    {
        public CreateTrackNumberExceptionArgs(IReadOnlyCollection<string> errorMessages)
        {
            ErrorMessages = errorMessages ?? Array.Empty<string>();
        }

        public IReadOnlyCollection<string> ErrorMessages { get; }

        public override string Message => string.Join(",", ErrorMessages);
    }
}