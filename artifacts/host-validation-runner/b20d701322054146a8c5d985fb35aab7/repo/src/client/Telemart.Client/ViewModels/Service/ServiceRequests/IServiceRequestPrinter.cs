using System.Threading.Tasks;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    public interface IServiceRequestPrinter
    {
        Task PrintAsync(ServiceRequestViewItem serviceRequest);
    }
}