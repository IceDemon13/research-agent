using System.ComponentModel;

namespace Telemart.Client.Reports.Product
{
    public enum BarcodeReportFormat
    {
        [Description("30x20")]
        Barcode30X20,

        [Description("50x40")]
        Barcode50X40,

        [Description("Большой")]
        Large,

        [Description("A5")]
        A5,

        [Description("B2B")]
        B2B
    }
}