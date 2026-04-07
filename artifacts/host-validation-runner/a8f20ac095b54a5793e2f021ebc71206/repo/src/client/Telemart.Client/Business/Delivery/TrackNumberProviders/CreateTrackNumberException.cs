using System;
using System.Collections.Generic;
using Telemart.Client.Core.Exceptions;

namespace Telemart.Client.Business.Delivery.TrackNumberProviders
{
    [Serializable]
    public sealed class CreateTrackNumberException : GenericException<CreateTrackNumberExceptionArgs>
    {
        public CreateTrackNumberException(IReadOnlyCollection<string> validationResultItems, Exception innerException = null)
            : base(new CreateTrackNumberExceptionArgs(validationResultItems), "Failed to create track number", innerException)
        {
        }

        public CreateTrackNumberException()
        {
        }

        public CreateTrackNumberException(string message)
            : base(message)
        {
        }

        public CreateTrackNumberException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private CreateTrackNumberException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }
    }
}