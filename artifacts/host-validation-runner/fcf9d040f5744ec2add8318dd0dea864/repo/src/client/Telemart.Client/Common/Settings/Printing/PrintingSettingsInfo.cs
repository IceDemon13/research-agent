using System;
using Newtonsoft.Json;

namespace Telemart.Client.Common.Settings.Printing
{
    public sealed class PrintingSettingsInfo
    {
        public PrintingSettingsInfo(
            PrinterSettingsInfo main,
            PrinterSettingsInfo cheque,
            PrinterSettingsInfo sticker,
            PrinterSettingsInfo warrantyCard,
            PrinterSettingsInfo barcode,
            PrinterSettingsInfo barcode50X40,
            PrinterSettingsInfo serialNumber,
            int chequeFormat,
            int? barcodeFormat,
            int? invoiceFormat,
            int warrantyFormat,
            bool printBarcodeAssemblyProduct,
            bool showChoosePrintRroCheck,
            string filesPath)
        {
            Main = main;
            Cheque = cheque;
            SerialNumber = serialNumber;
            Sticker = sticker;
            WarrantyCard = warrantyCard;
            Barcode = barcode;
            Barcode50X40 = barcode50X40;
            ChequeFormat = chequeFormat;
            BarcodeFormat = barcodeFormat;
            InvoiceFormat = invoiceFormat;
            WarrantyFormat = warrantyFormat;
            PrintBarcodeAssemblyProduct = printBarcodeAssemblyProduct;
            ShowChoosePrintRroCheck = showChoosePrintRroCheck;
            FilesPath = filesPath;
        }

        public PrintingSettingsInfo()
        {
        }

        [JsonProperty("main")]
        public PrinterSettingsInfo Main { get; set; }

        [JsonProperty("cheque")]
        public PrinterSettingsInfo Cheque { get; set; }

        [JsonProperty("sticker")]
        public PrinterSettingsInfo Sticker { get; set; }

        [JsonProperty("warranty_card")]
        public PrinterSettingsInfo WarrantyCard { get; set; }

        [JsonProperty("barcode")]
        public PrinterSettingsInfo Barcode { get; set; }

        [JsonProperty("barcode_50x40")]
        public PrinterSettingsInfo Barcode50X40 { get; set; }

        [JsonProperty("serial_number")]
        public PrinterSettingsInfo SerialNumber { get; set; }

        [JsonProperty("cheque_format")]
        public int? ChequeFormat { get; set; }

        [JsonProperty("invoice_format")]
        public int? InvoiceFormat { get; set; }

        [JsonProperty("barcode_format")]
        public int? BarcodeFormat { get; set; }

        [JsonProperty("warranty_format")]
        public int? WarrantyFormat { get; set; }

        [JsonProperty("print_barcode_assembly_product")]
        public bool? PrintBarcodeAssemblyProduct { get; set; }

        [JsonProperty("show_choose_print_rro_check")]
        public bool? ShowChoosePrintRroCheck { get; set; }

        [JsonProperty("files_path")]
        public string FilesPath { get; set; }
    }
}