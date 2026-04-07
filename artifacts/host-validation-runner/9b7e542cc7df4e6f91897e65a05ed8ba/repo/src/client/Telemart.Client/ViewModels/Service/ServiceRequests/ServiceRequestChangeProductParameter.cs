namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    public class ServiceRequestChangeProductParameter
    {
        public ServiceRequestChangeProductParameter(int serviceRequestId, string productName)
        {
            ServiceRequestId = serviceRequestId;
            ProductName = productName;
        }

        public int ServiceRequestId { get; }

        public string ProductName { get; }
    }
}
