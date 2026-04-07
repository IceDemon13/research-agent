using Telemart.Client.Common.Messages;
using Telemart.Client.TransferObjects.Locations;

namespace Telemart.Client.ViewModels.Locations
{
    public sealed class ClusterMessage : EntityMessage<ClusterDto>
    {
        public ClusterMessage(ClusterDto cluster, MessageType messageType)
         : base(cluster, messageType)
        {
        }
    }
}