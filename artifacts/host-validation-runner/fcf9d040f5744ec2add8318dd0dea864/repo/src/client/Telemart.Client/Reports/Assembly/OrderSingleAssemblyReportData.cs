using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Reports.Order.Assembly;

namespace Telemart.Client.ReportDesigner.Order
{
    public sealed class OrderSingleAssemblyReportData
    {
        private string _telegramManagermployee;
        private string _nameAssemblyComputers;

        public int AssemblyId { get; set; }

        public string NameAssemblyComputers
        {
            get { return string.IsNullOrEmpty(_nameAssemblyComputers) ? "Пользовательская сборка" : _nameAssemblyComputers; }
            set { _nameAssemblyComputers = value; }

        }

        public int OrderId { get; set; }

        public string NameAssemblyEmpolyees { get; set; }

        public string NameManagerEmployee { get; set; }

        public string TelegramManagerEmployee
        {
            get => string.IsNullOrEmpty(_telegramManagermployee) ? string.Empty : $"@{_telegramManagermployee}";
            set => _telegramManagermployee = value;
        }

        public string CustomerComment { get; set; }

        public string EmployeeComment { get; set; }

        public string DateTimeAssemblyPrint { get; private set; }

        public bool VisibleAdditionalServiceProducts => AssemblyAdditionalServiceProducts?.Any() == true;

        public IReadOnlyCollection<OrderAssemblyProductReportData> AssemblyServiceProducts { get; set; }

        public IReadOnlyCollection<AssemblyAdditionalServiceProductData> AssemblyAdditionalServiceProducts { get; private set; }

        public void SetDateTimeAssemblyPrint(DateTime dateTimeAssemblyPrint)
        {
            DateTimeAssemblyPrint = $"Дата печати: {dateTimeAssemblyPrint:dd.MM.yyyy HH:mm}";
        }

        public void SetAssemblyAdditionalServiceProducts(IReadOnlyCollection<AssemblyAdditionalServiceProductData> items)
        {
            AssemblyAdditionalServiceProducts = items;
        }
    }
}