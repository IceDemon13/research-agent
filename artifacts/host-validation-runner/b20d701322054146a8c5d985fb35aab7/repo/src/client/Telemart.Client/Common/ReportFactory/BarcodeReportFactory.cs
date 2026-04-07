using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevExpress.XtraReports;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Dictionaries;
using Telemart.Client.Reports.Product;

namespace Telemart.Client.Common.ReportFactory
{
    public class BarcodeReportFactory : IBarcodeReportFactory
    {
        private PrintingSettingsInfo printingSettingsInfo;

        public BarcodeReportFactory(IPrintingSettingsStore printingSettingsStore)
        {
            PrintingSettingsStore = printingSettingsStore;
        }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        public async Task<BarcodeReportFactoryResult> CreateAsync(string productName, int productId, int quantity)
        {
            printingSettingsInfo = await PrintingSettingsStore.LoadAsync();

            BarcodeReportFormat format = printingSettingsInfo.BarcodeFormat == PrintingSettingsBarcodeFormat.Barcode50X40.Id
                ? BarcodeReportFormat.Barcode50X40
                : BarcodeReportFormat.Barcode30X20;

            return await CreateAsync(format, productName, productId, quantity);
        }

        public async Task<BarcodeReportFactoryResult> CreateAsync(BarcodeReportFormat barcodeReportFormat, string productName, int productId, int quantity)
        {
            printingSettingsInfo ??= await PrintingSettingsStore.LoadAsync();

            BarcodeReportData reportData = new BarcodeReportData(productName, productId, quantity);
            List<BarcodeReportData> dataSource = new List<BarcodeReportData> { reportData };

            IReport report;
            PrinterSettingsInfo printerSettings;

            switch (barcodeReportFormat)
            {
                case BarcodeReportFormat.Barcode30X20:
                    report = new BarcodeReport { DataSource = dataSource };
                    printerSettings = printingSettingsInfo.Barcode;
                    break;
                case BarcodeReportFormat.Barcode50X40:
                    report = new Barcode50x40Report { DataSource = dataSource };
                    printerSettings = printingSettingsInfo.Barcode50X40;
                    break;
                case BarcodeReportFormat.Large:
                    report = new BarcodeLargeReport { DataSource = dataSource };
                    printerSettings = printingSettingsInfo.Sticker;
                    break;
                case BarcodeReportFormat.A5:
                    report = new BarcodeExtendedReport { DataSource = dataSource };
                    printerSettings = printingSettingsInfo.Main;
                    break;
                case BarcodeReportFormat.B2B:
                    report = new B2BBarcodeReport { DataSource = dataSource };
                    printerSettings = printingSettingsInfo.Barcode50X40;
                    break;
                default:
                    throw new NotSupportedException("Barcode format not supported");
            }

            return new BarcodeReportFactoryResult(report, printerSettings);
        }
    }
}
