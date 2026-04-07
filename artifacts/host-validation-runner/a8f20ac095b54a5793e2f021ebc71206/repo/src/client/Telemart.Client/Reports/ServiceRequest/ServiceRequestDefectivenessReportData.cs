using System;

namespace Telemart.Client.Reports.ServiceRequest
{
    public class ServiceRequestDefectivenessReportData
    {
        public ServiceRequestDefectivenessReportData(
            string currentEmployeeFio,
            DateTime? serviceRequestReceivedOn,
            DateTime? serviceRequestPurchasedOn,
            string organizationName,
            string productFullName,
            string serialNumber,
            string statedDefect,
            string completenessComment)
        {
            ServiceRequestReceivedOn = serviceRequestReceivedOn;
            ProductFullName = productFullName;
            SerialNumber = serialNumber;
            OrganizationName = organizationName;
            ServiceRequestPurchasedOn = serviceRequestPurchasedOn;
            StatedDefect = statedDefect;
            CompletenessComment = completenessComment;
            CurrentEmployeeFio = currentEmployeeFio;
        }

        public DateTime? ServiceRequestReceivedOn { get; }

        public string ProductFullName { get; }

        public string SerialNumber { get; }

        public string OrganizationName { get; }

        public DateTime? ServiceRequestPurchasedOn { get; }

        public string StatedDefect { get; }

        public string CompletenessComment { get; }

        public string CurrentEmployeeFio { get; }
    }
}
