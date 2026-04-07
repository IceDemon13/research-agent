using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.XtraPrinting;
using DevExpress.XtraRichEdit;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Core.IO;
using Telemart.Client.Data.Requests.Features.TradeIn;
using Telemart.Client.Data.Requests.Features.TradeIn.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Client.ViewModels.TradeIn;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Reports.TradeIn
{
    public static class TradeInReportPrinter
    {
        public static async Task PrintActAsync(
            TradeInViewItem viewItem,
            IReadOnlyCollection<TradeInIndicatorValueDto> tradeInWarrantyValues,
            IReadOnlyCollection<TradeInIndicatorValueDto> tradeInPackageValues,
            IReadOnlyCollection<ComboBoxItem> allCategories,
            IWebClient webClient)
        {
            Result<TradeInActReportDto> tradeInActResult = await webClient.ExecuteApiRequestAsync(new QueryTradeInActReport(viewItem.Id));

            if (!tradeInActResult.IsSuccess)
            {
                return;
            }

            string html = await GetHtmlAsync(
                viewItem,
                tradeInWarrantyValues,
                tradeInPackageValues,
                allCategories,
                tradeInActResult.Data);

            await webClient.ExecuteApiRequestAsync(new PrintTradeInActRequest(viewItem.Id));

            await FileHelper.OpenAsFileAsync(Encoding.UTF8.GetBytes(html), "html");
        }

        public static async Task<byte[]> GetTradeInActInBytesAsync(
            TradeInViewItem viewItem,
            IReadOnlyCollection<TradeInIndicatorValueDto> tradeInWarrantyValues,
            IReadOnlyCollection<TradeInIndicatorValueDto> tradeInPackageValues,
            IReadOnlyCollection<ComboBoxItem> allCategories,
            IWebClient webClient)
        {
            byte[] result = new byte [0];

            Result<TradeInActReportDto> tradeInActResult = await webClient.ExecuteApiRequestAsync(new QueryTradeInActReport(viewItem.Id));

            if (!tradeInActResult.IsSuccess)
            {
                return result;
            }

            string html = await GetHtmlAsync(
                viewItem,
                tradeInWarrantyValues,
                tradeInPackageValues,
                allCategories,
                tradeInActResult.Data);

            using (RichEditDocumentServer richServer = new RichEditDocumentServer())
            {
                richServer.HtmlText = html;

                var document = richServer.Document;

                document.CompatibilitySettings.AllowTablesOutstepMargins = false;
                document.CompatibilitySettings.AllowHyphenationAtTrackBottom = false;
                document.CompatibilitySettings.AllowTextAfterFloatingTableBreak = false;
                document.CompatibilitySettings.DifferentiateMultirowTableHeaders = false;
                document.CompatibilitySettings.SplitPageBreakAndParagraphMark = false;
                document.CompatibilitySettings.SplitTableRowsAroundFloatingTables = false;
                document.CompatibilitySettings.SplitWrappedTablesAcrossPages = false;
                document.CompatibilitySettings.StretchLinesWithLineBreaks = false;

                document.Unit = DevExpress.Office.DocumentUnit.Millimeter;
                document.Sections[0].Page.Width = 210;
                document.Sections[0].Page.Height = 297;
                document.Sections[0].Margins.Top = 1.5f;
                document.Sections[0].Margins.Bottom = 1.5f;
                document.Sections[0].Margins.Left = 2;
                document.Sections[0].Margins.Right = 2;

                using (MemoryStream pdfMemoryStream = new MemoryStream())
                {
                    PdfExportOptions options = new PdfExportOptions();
                    options.DocumentOptions.Author = "Telemart";
                    options.DocumentOptions.Title = "Telemart Act Report";
                    options.Compressed = false;
                    options.ImageQuality = PdfJpegImageQuality.Medium;

                    richServer.ExportToPdf(pdfMemoryStream, options);

                    result = pdfMemoryStream.ToArray();
                }
            }

            return result;
        }

        private static string GetRealBuyoutAmountWithTaxes(TradeInViewItem viewItem)
        {
            if (viewItem.RealBuyoutAmount is null)
            {
                return null;
            }

            decimal amount = viewItem.RealBuyoutAmount.Value + (viewItem.PdfTaxAmount ?? 0) + (viewItem.MilitaryTaxAmount ?? 0);

            return $"{amount:F2} грн";
        }

        private static async Task<string> GetHtmlAsync(
            TradeInViewItem viewItem,
            IReadOnlyCollection<TradeInIndicatorValueDto> tradeInWarrantyValues,
            IReadOnlyCollection<TradeInIndicatorValueDto> tradeInPackageValues,
            IReadOnlyCollection<ComboBoxItem> allCategories,
            TradeInActReportDto tradeInActReportDto)
        {
            const int maxLenghtComment = 410;

            string htmlString = await File.ReadAllTextAsync(@"Reports\TradeIn\TradeInInnReport.html");


            StringBuilder htmlStringBuilder = new StringBuilder(htmlString);

            StringBuilder formattedHtml = htmlStringBuilder
                .Replace("{SerialNumber}", viewItem.SerialNumber)
                .Replace("{WarrantyName}", tradeInWarrantyValues.FirstOrDefault(x => x.Id == viewItem.WarrantyId)?.DescriptionUkr ?? string.Empty)
                .Replace("{PackName}", tradeInPackageValues.FirstOrDefault(x => x.Id == viewItem.PackId)?.DescriptionUkr ?? string.Empty)
                .Replace("{ClassName}", viewItem.ClassDescriptionUkr ?? string.Empty)
                .Replace("{Comment}", viewItem.Comment?.CutString(maxLenghtComment, true).SetSimbolThroughNumberCharacters(105, Environment.NewLine))
                .Replace("{CategoryName}", viewItem.CategoryId.HasValue ? allCategories.First(x => x.Id == viewItem.CategoryId).DisplayValue : string.Empty)
                .Replace("{RealBuyoutAmount}", viewItem.RealBuyoutAmountString)
                .Replace("{RealBuyoutAmountWithTaxes}", GetRealBuyoutAmountWithTaxes(viewItem))
                .Replace("{ProductName}", string.IsNullOrWhiteSpace(viewItem.ProductName) ? $"{viewItem.Brand} {viewItem.ModelOrPn}" : viewItem.ProductName)
                .Replace("{TradeInId}", viewItem.Id.ToString())
                .Replace("{Fio}", viewItem.Fio)
                .Replace("{Inn}", viewItem.Inn)
                .Replace("{MilitaryTaxAmount}", viewItem.MilitaryTaxAmountString)
                .Replace("{MilitaryTaxValue}", viewItem.MilitaryTaxValueString)
                .Replace("{PdfTaxAmount}", viewItem.PdfTaxAmountString)
                .Replace("{NowOrCompletedOn}", (viewItem.CompletedOn ?? DateTime.Now).ToString("dd.MM.yyyy HH.mm"))
                .Replace("{Phone}", viewItem.Phone)
                .Replace("{LegalEntityRequisites}", tradeInActReportDto.LegalEntityRequisites)
                .Replace("{LegalEntityName}", tradeInActReportDto.LegalEntityName)
                .Replace("{Id}", viewItem.Id.ToString());

            return formattedHtml.ToString();
        }
    }
}