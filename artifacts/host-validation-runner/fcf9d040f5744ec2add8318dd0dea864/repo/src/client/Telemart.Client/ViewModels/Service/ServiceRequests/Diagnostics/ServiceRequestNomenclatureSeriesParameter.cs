namespace Telemart.Client.ViewModels.Service.ServiceRequests.Diagnostics
{
    public sealed class ServiceRequestNomenclatureSeriesParameter
    {
        public ServiceRequestNomenclatureSeriesParameter(int productId, string productName, string nomenclatureSeries, bool diagnosticStage, int serviceRequestId)
        {
            ProductId = productId;
            ProductName = productName;
            NomenclatureSeries = nomenclatureSeries;
            DiagnosticStage = diagnosticStage;
            ServiceRequestId = serviceRequestId;
        }

        public int ProductId { get; }

        public string ProductName { get; }

        public string NomenclatureSeries { get; }

        public int ServiceRequestId { get; }

        public bool DiagnosticStage { get; }
    }
}