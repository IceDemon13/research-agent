using Telemart.Client.Core.Exceptions.Args;

namespace Telemart.Client.Core.Exceptions
{
    public sealed class ResourceNotFoundException : GenericException<ResourceNotFoundExceptionArgs>
    {
        public ResourceNotFoundException(int resourceId, string resourceType = null)
            : this(resourceId.ToString(), resourceType)
        {
        }

        public ResourceNotFoundException(string resourceId, string resourceType = null)
            : base(new ResourceNotFoundExceptionArgs(resourceId, resourceType))
        {
        }

        public ResourceNotFoundException(string resourceType, params (string PropertyName, object PropertyValue)[] searchCriteria)
            : base(new ResourceNotFoundExceptionArgs(null, resourceType, searchCriteria))
        {
        }
    }
}