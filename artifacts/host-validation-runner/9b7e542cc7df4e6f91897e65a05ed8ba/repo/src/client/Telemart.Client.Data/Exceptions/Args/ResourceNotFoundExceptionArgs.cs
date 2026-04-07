using System.Collections.Generic;
using System.Linq;

namespace Telemart.Client.Core.Exceptions.Args
{
    public sealed class ResourceNotFoundExceptionArgs : ExceptionArgs
    {
        public ResourceNotFoundExceptionArgs(string resourceId, string resourceType, params (string PropertyName, object PropertyValue)[] searchCriteria)
        {
            ResourceId = resourceId;
            ResourceType = resourceType;
            SearchCriteria = searchCriteria;
        }

        public string ResourceId { get; set; }

        public string ResourceType { get; set; }

        public IEnumerable<(string PropertyName, object PropertyValue)> SearchCriteria { get; set; }

        public override string Message => string.IsNullOrEmpty(ResourceId)
            ? $"Resource {ResourceType} with search criteria: {string.Join(", ", SearchCriteria.Select(x => $"{x.PropertyName} = {x.PropertyValue}"))} not found"
            : $"Resource {ResourceType} with id:{ResourceId} not found";
    }
}