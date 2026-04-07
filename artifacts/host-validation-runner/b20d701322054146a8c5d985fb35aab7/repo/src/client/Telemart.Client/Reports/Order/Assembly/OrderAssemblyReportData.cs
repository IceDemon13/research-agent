using System;
using System.Collections.Generic;
using System.Linq;

namespace Telemart.Client.Reports.Order.Assembly
{
    public sealed class OrderAssemblyReportData
    {
        private string _telegramConfirmEmployee;

        public IReadOnlyCollection<OrderAssemblyProductReportData> Products { get; set; }

        public IReadOnlyCollection<OrderAssemblyProductReportData> AssemblyServiceProducts { get; set; }

        public int OrderId { get; set; }

        public string NameAssemblyComputers { get; set; }

        public string NameAssemblyEmpolyees { get; set; }

        public string NameConfirmEmployee { get; set; }

        public string TelegramConfirmEmployee
        {
            get => !string.IsNullOrEmpty(_telegramConfirmEmployee) ? $"Telegram @{_telegramConfirmEmployee}" : string.Empty;
            set => _telegramConfirmEmployee = value;
        }

        public string CustomerComment { get; set; }

        public string EmployeeComment { get; set; }

        public string DateTimeAssemblyPrint { get; private set; }

        public bool AnyAssemblyProducts => AssemblyServiceProducts.Any();

        public bool AnyProducts => Products.Any();

        public bool VisibleNameAssemblyComputer => NameAssemblyComputers?.Any() == true;

        public bool VisibleNameAssemblyEmpolyee => NameAssemblyEmpolyees?.Any() == true;

        public void SetDateTimeAssemblyPrint(DateTime dateTimeAssemblyPrint)
        {
            DateTimeAssemblyPrint = $"Дата печати: {dateTimeAssemblyPrint:dd.MM.yyyy HH:mm}";
        }
    }
}