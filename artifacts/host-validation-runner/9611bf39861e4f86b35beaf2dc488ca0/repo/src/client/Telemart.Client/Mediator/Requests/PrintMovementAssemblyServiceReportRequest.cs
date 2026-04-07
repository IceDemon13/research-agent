using MediatR;

namespace Telemart.Client.Mediator.Requests
{
    public sealed class PrintMovementAssemblyServiceReportRequest : IRequest
    {
        public PrintMovementAssemblyServiceReportRequest(int assemblyServiceId, int orderId, int places, int countProducts)
        {
            AssemblyServiceId = assemblyServiceId;
            OrderId = orderId;
            Places = places;
            CountProducts = countProducts;
        }

        public int OrderId { get; }

        public int AssemblyServiceId { get; }

        public int Places { get; }

        public int CountProducts { get; }
    }
}