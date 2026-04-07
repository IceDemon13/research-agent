using System.Collections.Generic;

namespace Telemart.Client.ReportDesigner
{
    public class MovementProductGroupReportData
    {
        public MovementProductGroupReportData(string name, IReadOnlyCollection<MovementProductReportData> products)
        {
            Name = name;
            Products = products;
        }

        public string Name { get; }

        public IReadOnlyCollection<MovementProductReportData> Products { get; }
    }
}
