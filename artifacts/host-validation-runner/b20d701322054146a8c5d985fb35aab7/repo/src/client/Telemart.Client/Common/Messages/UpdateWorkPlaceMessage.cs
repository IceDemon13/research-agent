using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public class UpdateWorkPlaceMessage
    {
        public UpdateWorkPlaceMessage(WorkPlaceDto workPlace)
        {
            WorkPlace = workPlace;
        }

        public WorkPlaceDto WorkPlace { get; }
    }
}