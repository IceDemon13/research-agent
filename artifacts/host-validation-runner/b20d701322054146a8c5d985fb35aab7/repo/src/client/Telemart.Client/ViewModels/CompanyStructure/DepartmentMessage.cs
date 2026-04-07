using Telemart.Client.Common.Messages;
using Telemart.Client.TransferObjects.CompanyStructure;

namespace Telemart.Client.ViewModels.CompanyStructure
{
    public sealed class DepartmentMessage : EntityMessage<DepartmentDto>
    {
        public DepartmentMessage(DepartmentDto departmentDto, MessageType messageType)
            : base(departmentDto, messageType)
        {
        }
    }
}