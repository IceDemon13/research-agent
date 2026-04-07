using System;
using System.Collections.Generic;
using System.Linq;

namespace Telemart.Client.Reports.Order.Assembly
{
    public sealed class OrderAssemblyReportSimpleData
    {
        private string _telegramConfirmEmployee;

        public OrderAssemblyReportSimpleData()
        {
            DateTimeAssemblyPrint = $"Дата печати: {DateTime.Now:dd.MM.yyyy HH:mm}";
        }

        public IReadOnlyCollection<OrderPackListProductReportData> Products { get; set; }

        public int OrderId { get; set; }

        public string AssembledByNames { get; set; }

        public string ConfirmedByName { get; set; }

        public string TelegramConfirmEmployee
        {
            get => _telegramConfirmEmployee;
            set
            {
                _telegramConfirmEmployee = !string.IsNullOrEmpty(value) ? $"Telegram @{value}" : string.Empty;
            }
        }

        public string CustomerComment { get; set; }

        public string EmployeeComment { get; set; }

        public string DateTimeAssemblyPrint { get; private set; }

        public bool AnyProducts => Products.Any();

        public bool VisibleNameAssemblyEmployee => AssembledByNames?.Any() == true;
    }
}
