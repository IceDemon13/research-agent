using System;
using System.Drawing.Printing;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Core;
using Telemart.Client.Core.Serialization;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Common.Settings.Printing
{
    internal sealed class PrintingSettingsStore : SettingsStore<PrintingSettingsInfo>, IPrintingSettingsStore
    {
        private bool _haveNullParameter;

        public PrintingSettingsStore(ISerializerBuilder serializerBuilder, IFileSystem fileSystem)
            : base(serializerBuilder, fileSystem)
        {
        }

        public override string FileName { get; protected set; } = "printing_settings.json";

        protected override PrintingSettingsInfo GetDefault()
        {
            string defaultPrinterName = GetDefaultPrinterName();

            return new PrintingSettingsInfo(
                new PrinterSettingsInfo { Name = defaultPrinterName },
                new PrinterSettingsInfo { Name = defaultPrinterName },
                new PrinterSettingsInfo { Name = defaultPrinterName },
                new PrinterSettingsInfo { Name = defaultPrinterName },
                new PrinterSettingsInfo { Name = defaultPrinterName },
                new PrinterSettingsInfo { Name = defaultPrinterName },
                new PrinterSettingsInfo { Name = defaultPrinterName },
                PrintingSettingsChequeFormat.A4.Id,
                PrintingSettingsBarcodeFormat.Barcode30X20.Id,
                PrintingSettingsInvoiceFormat.A4.Id,
                PrintingSettingsWarrantyFormat.A5.Id,
                false,
                true,
                null);
        }

        protected override string GetBasePath()
        {
            return ApplicationFolders.CommonApplicationData;
        }

        protected override async Task PreProcessAsync(PrintingSettingsInfo settings)
        {
            await base.PreProcessAsync(settings);

            if (_haveNullParameter)
            {
                await SaveAsync(settings);
            }
        }

        protected override PrintingSettingsInfo MergeSettings(PrintingSettingsInfo settings, PrintingSettingsInfo defaultSettings)
        {
            _haveNullParameter = HaveNullParamener(settings);

            settings.ChequeFormat ??= defaultSettings.ChequeFormat;
            settings.BarcodeFormat ??= defaultSettings.BarcodeFormat;
            settings.InvoiceFormat ??= defaultSettings.InvoiceFormat;
            settings.WarrantyFormat ??= defaultSettings.WarrantyFormat;
            settings.PrintBarcodeAssemblyProduct ??= defaultSettings.PrintBarcodeAssemblyProduct;
            settings.ShowChoosePrintRroCheck ??= defaultSettings.ShowChoosePrintRroCheck;

            return settings;
        }

        private static string GetDefaultPrinterName()
        {
            try
            {
                return PrinterSettings.InstalledPrinters.Cast<string>()
                    .Select(printer => new { PrinterName = printer, PrinterSettings = new PrinterSettings { PrinterName = printer } })
                    .Where(t => t.PrinterSettings.IsValid && t.PrinterSettings.IsDefaultPrinter)
                    .Select(t => t.PrinterName)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private bool HaveNullParamener(PrintingSettingsInfo settings)
        {
            return settings.BarcodeFormat == null
                   || settings.ChequeFormat == null
                   || settings.InvoiceFormat == null
                   || settings.WarrantyFormat == null
                   || settings.PrintBarcodeAssemblyProduct == null
                   || settings.ShowChoosePrintRroCheck == null;
        }
    }
}