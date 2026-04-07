using System;
using Telemart.Client.Business.Barcode;
using Telemart.Client.Dictionaries;
using Telemart.Client.Reports.SerialNumber;
using Telemart.Client.ViewModels.Service.ServiceRequests;

namespace Telemart.Client.Reports.ServiceRequest
{
    public sealed class ServiceRequestReportData
    {
        private readonly ServiceRequestViewItem source;

        public ServiceRequestReportData(ServiceRequestViewItem viewItem, bool printExtention)
        {
            source = viewItem ?? throw new ArgumentNullException(nameof(viewItem));

            OurServiceBarcode serviceBarcode = new OurServiceBarcode(viewItem.Id);

            Barcode = new SerialNumberReportData(serviceBarcode.ServiceRequestId, serviceBarcode.BarcodeText);

            RequirementText = GetRequirementText(viewItem.Requirement, viewItem.ServiceRepairTypeId);

            PrintExtention = printExtention;

            if (!string.IsNullOrWhiteSpace(viewItem.Appearance))
            {
                int middleIndex = viewItem.Appearance.IndexOf('(');

                if (middleIndex == -1 || middleIndex == 0)
                {
                    AppearanceKind = viewItem.Appearance;
                }
                else
                {
                    AppearanceKind = viewItem.Appearance.Substring(0, middleIndex - 1);
                    Appearance = viewItem.Appearance.Substring(middleIndex + 1);
                    Appearance = Appearance.Trim('(', ')');
                }
            }
        }

        public static string Today => DateTime.Today.ToString("dd.MM.yyyy");

        public int Id => source.Id;

        public string Fio => source.Fio;

        public string Phone => source.Phone;

        public string Product => source.ProductName;

        public string SerialNumber => source.SerialNumber;

        public string StatedDefect => source.StatedDefect;

        public string Appearence => source.Appearance;

        public string Inspection => GetInspectionText();

        public string Completeness => source.CompletenessComment;

        public string ClientRequirement => source.RequirementSummary;

        public string AppearanceKind { get; }

        public string Appearance { get; }

        public string RequirementText { get; }

        public bool PrintExtention { get; }

        public DateTime OrderCompletedOn => source.OrderCompletedOn;

        public SerialNumberReportData Barcode { get; }

        private string GetRequirementText(ServiceRequestRequirement requirement, int? srtId)
        {
            return requirement.Id switch
            {
                ServiceRequestRequirement.RepairId => srtId == ServiceRepairType.Paid.Id ? "Негарантійний ремонт" : "Безоплатне усунення недоліків товару в тридцятиденний строк",
                ServiceRequestRequirement.ChangeId => "Обмін",
                ServiceRequestRequirement.ReturnMoneyId => "Повернення грошових коштів",
                _ => string.Empty,
            };
        }

        private string GetInspectionText()
        {
            if (string.IsNullOrEmpty(source.Inspection))
            {
                return null;
            }

            return "Під час приймання оцінка стану товару не проводилась. Недоліки записані зі слів Покупця. Остаточний висновок щодо стану товару надасть сервісний центр.";
        }
    }
}