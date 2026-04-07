using Telemart.Client.ViewModels.Service.ServiceProducts;

namespace Telemart.Client.Common.Messages
{
    public class ServiceProductViewMessage : ServiceProductParameter
    {
        public ServiceProductViewMessage(int id)
            : base(id)
        {
        }
    }
}
