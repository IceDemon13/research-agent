using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Telemart.Client.Reports.ServiceInvoice
{
    public class ServiceInvoiceReportData
    {
        private string employeeContacts;

        private string header;

        public ServiceInvoiceReportData(IReadOnlyCollection<ServiceInvoiceProductReportData> products)
        {
            Products = products;
        }

        public IReadOnlyCollection<ServiceInvoiceProductReportData> Products { get; }

        public string Header => header ?? (header = GetHeader());

        public int Number { get; set; }

        public DateTime DateSend { get; set; }

        public string OrganizationName { get; set; }

        public string EmployeeName { get; set; }

        public string[] EmployeePhones { private get; set; }

        public string EmployeeEmail { private get; set; }

        public string ServiceCenterName { get; set; }

        public string ServiceCenterAddress { get; set; }

        public string EmployeeContacts => employeeContacts ?? (employeeContacts = GetEmployeeContacts());

        private string GetEmployeeContacts()
        {
            string[] employeePhones = EmployeePhones.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            return $"тел: {string.Join(", ", employeePhones)}; e-mail: {EmployeeEmail}";
        }

        private string GetHeader()
        {
            return $"Акт приёма-передачи товара №{Number} от {DateSend.ToString("d MMMM yyyy 'г.'", CultureInfo.CreateSpecificCulture("ru"))}";
        }
    }
}
