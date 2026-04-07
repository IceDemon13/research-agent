using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using DevExpress.XtraReports;
using DevExpress.XtraReports.UI;
using Telemart.Client.Common;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Data.Requests.Features.LegalEntity;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ReportDesigner;
using Telemart.Client.Reports.Order;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Business.Order
{
    internal sealed class OrderReportBuilder : IOrderReportBuilder
    {
        public OrderReportBuilder(IDictionaries dictionaries, IPrintingSettingsStore printingSettingsStore, IWebClient webClient)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            PrintingSettingsStore = printingSettingsStore ?? throw new ArgumentNullException(nameof(printingSettingsStore));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
        }

        private IDictionaries Dictionaries { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IWebClient WebClient { get; }

        public async Task<IReport> BuildAcceptanceProtocolReportAsync(OrderDto order, string contractorName, string cityName, bool todayAsIssueDate)
        {
            ReportBuilderBase builder = new AcceptanceProtocolReportBuilder(PrintingSettingsStore, WebClient);
            IReport report = await builder.GetReportAsync(order, null, contractorName, cityName, todayAsIssueDate);
            return report;
        }

        public async Task<IReport> BuildChequeReportAsync(OrderDto order, int[] productIds, string contractorName, string cityName, bool todayAsIssueDate)
        {
            Subdivision subdivision = Dictionaries.GetItemById<Subdivision>(order.SubdivisionId);

            ReportBuilderBase builder = subdivision.IsRetail
                ? new ChequeReportBuilder(PrintingSettingsStore, WebClient)
                : new WaybillReportBuilder(PrintingSettingsStore, WebClient);

            IReport report = await builder.GetReportAsync(order, productIds, contractorName, cityName, todayAsIssueDate);

            return report;
        }

        public async Task<IReport> BuildTapeChequeReportAsync(OrderDto order, int[] productIds, string contractorName, string cityName, bool todayAsIssueDate)
        {
            ReportBuilderBase builder = new TapeChequeReportBuilder(PrintingSettingsStore, WebClient);

            order.Products = order.Products.Where(x => x.CurrencyOutId == Currency.Uah.Id).ToList();

            IReport report = await builder.GetReportAsync(order, productIds, contractorName, cityName, todayAsIssueDate);
            return report;
        }

        public async Task<IReport> BuildProductWarrantyCardReportAsync(OrderDto order, int productId, string serialNumber, IDictionary<int, string> warranties, bool todayAsIssueDate)
        {
            OrderProductDto orderProduct = order.Products.First(x => x.Product.Id == productId);

            OrderProductWarrantyCardReportData reportData = new OrderProductWarrantyCardReportData(
                1,
                orderProduct.Product.NameFullUkr,
                1,
                new[] { serialNumber },
                warranties.GetValueOrDefault(orderProduct.WarrantyId, string.Empty));

            return await BuildWarrantyCardReportAsync(order, new[] { reportData }, todayAsIssueDate, false);
        }

        public async Task<IReport> BuildWarrantyCardReportAsync(OrderDto order, int[] orderProductsIds, IDictionary<int, string> warranties, bool todayAsIssueDate, bool separateWarrantyCards)
        {
            OrderProductWarrantyCardReportData[] reportDatas = order.Products
                 .Where(p => (orderProductsIds == null || !orderProductsIds.Any() || orderProductsIds.Contains(p.Id))
                    && !p.IsAdditionalService)
                 .GroupBy(p => new KeyCollectionItem<int, int>(p.Product.Id, order.Products.Where(x => x.ParentRecordId == p.Id && x.IsAdditionalService).Select(x => x.Product.Id).ToArray()))
                 .Select(g => new { Grouping = g, OrderProduct = g.First() })
                 .Select((y, i) => new OrderProductWarrantyCardReportData(
                     i + 1,
                     y.OrderProduct.Product.NameFullUkr,
                     y.Grouping.Sum(x => x.Quantity),
                     y.Grouping.SelectMany(x => x.SerialNumbers.Select(z => z.SerialNumber)).ToArray(),
                     warranties.GetValueOrDefault(y.OrderProduct.WarrantyId, string.Empty))
                 {
                     AdditionalServices = GetReportProducts(
                         order.Products.Where(x => x.IsAdditionalService
                            && y.Grouping.Any(gc => x.ParentRecordId == gc.Id)),
                         warranties)
                     .ToArray().NullIfEmpty()
                 })
                 .ToArray();

            return await BuildWarrantyCardReportAsync(order, reportDatas, todayAsIssueDate, separateWarrantyCards);

            static IEnumerable<OrderProductWarrantyCardReportData> GetReportProducts(IEnumerable<OrderProductDto> orderProducts, IDictionary<int, string> warranties)
            {
                return orderProducts.GroupBy(p => new { p.Product.Id })
                 .Select(g => new { Grouping = g, OrderProduct = g.First() })
                 .Select((y, i) => new OrderProductWarrantyCardReportData(
                     i + 1,
                     y.OrderProduct.Product.NameFullUkr,
                     y.Grouping.Sum(x => x.Quantity),
                     y.Grouping.SelectMany(x => x.SerialNumbers.Select(z => z.SerialNumber)).ToArray(),
                     warranties.GetValueOrDefault(y.OrderProduct.WarrantyId, string.Empty)));
            }
        }

        public async Task<IReport> BuildWarrantyCardReportAsync(
           OrderDto order,
           IReadOnlyCollection<OrderProductWarrantyCardReportData> products,
           bool todayAsIssueDate,
           bool separateWarrantyCards)
        {
            DateTime issueDate = GetIssueDate(order);

            List<OrderWarrantyCardReportData> dataSource = GetOrderWarrantyCardDataSource(order, products, issueDate, separateWarrantyCards);

            return await GetOrderWarrantyCardReportAsync(dataSource);
        }

        private static List<OrderWarrantyCardReportData> GetOrderWarrantyCardDataSource(
            OrderDto order,
            IReadOnlyCollection<OrderProductWarrantyCardReportData> products,
            DateTime issueDate,
            bool separeteWarrantyCards)
        {
            List<OrderWarrantyCardReportData> dataSource;

            if (separeteWarrantyCards)
            {
                dataSource = new List<OrderWarrantyCardReportData>(products.Sum(x => x.Quantity));

                foreach (OrderProductWarrantyCardReportData product in products.OrderBy(x => x.Position).ThenBy(x => x.NameUa))
                {
                    for (int i = 0; i < product.Quantity; i++)
                    {
                        List<string> serials = product.SerialNumbers.Count > i
                            ? new List<string> { product.SerialNumbers.ElementAt(i) }
                            : new List<string>();

                        OrderProductWarrantyCardReportData clone = new OrderProductWarrantyCardReportData(
                            1,
                            product.NameUa,
                            1,
                            serials,
                            product.WarrantyUa)
                        {
                            AdditionalServices = product.AdditionalServices
                                ?.Select(x => new OrderProductWarrantyCardReportData(
                                    x.Position,
                                    x.NameUa,
                                    x.Quantity / product.Quantity,
                                    x.SerialNumbers,
                                    x.WarrantyUa))
                                .ToArray()
                        };

                        dataSource.Add(new OrderWarrantyCardReportData(order.Id, issueDate, new[] { clone }));
                    }
                }
            }
            else
            {
                OrderWarrantyCardReportData reportData = new OrderWarrantyCardReportData(
                    order.Id,
                    issueDate,
                    products);

                dataSource = new List<OrderWarrantyCardReportData> { reportData };
            }

            return dataSource;
        }

        private static DateTime GetIssueDate(OrderDto order)
        {
            return order.CompletedOn ?? DateTime.Today;
        }

        private async Task<IReport> GetOrderWarrantyCardReportAsync(IReadOnlyList<OrderWarrantyCardReportData> dataSource)
        {
            PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

            XtraReport report;

            if (printSettings.WarrantyFormat == PrintingSettingsWarrantyFormat.A5.Id)
            {
                report = new OrderWarrantyCardReport { DataSource = new List<OrderWarrantyCardReportData> { dataSource[0] } };
            }
            else
            {
                report = new OrderWarrantyCardTapeReport { DataSource = new List<OrderWarrantyCardReportData> { dataSource[0] } };
            }

            await report.CreateDocumentAsync();

            for (int i = 1; i < dataSource.Count; i++)
            {
                XtraReport reportToMerge;

                if (printSettings.WarrantyFormat == PrintingSettingsWarrantyFormat.A5.Id)
                {
                    reportToMerge = new OrderWarrantyCardReport() { DataSource = new List<OrderWarrantyCardReportData> { dataSource[i] } };
                }
                else
                {
                    reportToMerge = new OrderWarrantyCardTapeReport() { DataSource = new List<OrderWarrantyCardReportData> { dataSource[i] } };
                }

                await reportToMerge.CreateDocumentAsync();

                report.Pages.AddRange(reportToMerge.Pages);
            }

            report.PrintingSystem.ContinuousPageNumbering = true;

            return report;
        }

        private abstract class ReportBuilderBase
        {
            protected ReportBuilderBase(IPrintingSettingsStore printingSettingsStore, IWebClient webClient)
            {
                WebClient = webClient;
                PrintingSettingsStore = printingSettingsStore;
                UaCulture = new CultureInfo("uk-Ua");
            }

            protected CultureInfo UaCulture { get; }

            private IPrintingSettingsStore PrintingSettingsStore { get; }

            private IWebClient WebClient { get; }

            public async Task<IReport> GetReportAsync(OrderDto order, int[] productIds, string contractorName, string cityName, bool todayAsIssueDate)
            {
                Tuple<string, string> header = GetHeader(order, todayAsIssueDate);
                IReadOnlyCollection<OrderPropertyReportData> properties = GetProperties(order, contractorName, cityName).ToArray();

                IReadOnlyCollection<OrderProductReportData> products = GetProducts(order, productIds);

                decimal prepaymentUah;
                decimal prepaymentUsd;

                if (productIds == null || productIds.Length == 0)
                {
                    OrderPrices orderPries = order.GetPrices();

                    prepaymentUah = orderPries.Payed.Uah;
                    prepaymentUsd = orderPries.Payed.Usd;
                }
                else
                {
                    prepaymentUah = 0;
                    prepaymentUsd = 0;
                }

                LegalEntityDto teleSvit = await WebClient.ExecuteApiRequestAsync(new QueryLegalEntity(LegalEntityIds.TeleSvitId));

                OrderReportData reportData = new OrderReportData(
                    order.Id,
                    order.PackageDeliveryCost,
                    prepaymentUah,
                    prepaymentUsd,
                    header.Item1,
                    header.Item2,
                    order.LegalEntity?.ReportName ?? teleSvit.ReportName,
                    order.LegalEntity?.Id != LegalEntityIds.TelemartId,
                    properties,
                    products,
                    order.Fio);

                IReport report = await GetReportAsync(reportData);

                return report;
            }

            protected abstract Tuple<string, string> GetHeader(OrderDto order, bool todayAsIssueDate);

            protected abstract IEnumerable<OrderPropertyReportData> GetProperties(OrderDto order, string contractorName, string cityName);

            protected virtual async Task<IReport> GetReportAsync(OrderReportData reportData)
            {
                PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

                switch (printSettings.InvoiceFormat)
                {
                    case PrintingSettingsInvoiceFormat.A4Id:
                        return new OrderReport { DataSource = new[] { reportData } };
                    case PrintingSettingsInvoiceFormat.A5Id:
                        return new OrderReportA5 { DataSource = new[] { reportData } };
                    case PrintingSettingsInvoiceFormat.TapeId:
                        return new OrderTapeReport { DataSource = new[] { reportData } };
                    default:
                        return new OrderTapeReport { DataSource = new[] { reportData } };
                }
            }

            private static IReadOnlyCollection<OrderProductReportData> GetProducts(OrderDto order, int[] productIds)
            {
                List<OrderProductReportData> productReportDatas = order.Products
                    .Where(x => (productIds?.Any() != true || productIds.Contains(x.Product.Id)) && x.ProductTypeId != (int)Telemart.Common.Dictionaries.ProductType.GuestProduct)
                    .GroupBy(x => new { x.Product.Id, x.PriceOut, x.CurrencyOutId })
                    .Select(g => new { Product = g.Select(x => new { x.Product.Id, x.Product.NameFullRu, x.Product.NameFullUkr, x.PriceOut, x.CurrencyOutId }).First(), Quantity = g.Sum(z => z.Quantity) })
                    .OrderBy(x => x.Product.NameFullRu)
                    .Select((x, index) => new OrderProductReportData(
                        index + 1,
                        x.Product.Id,
                        x.Product.NameFullRu,
                        x.Product.NameFullUkr,
                        productIds?.Any(y => y == x.Product.Id) == true ? productIds.Count(y => y == x.Product.Id) : x.Quantity,
                        x.Product.PriceOut,
                        Currency.GetById(x.Product.CurrencyOutId)))
                    .ToList();

                return productReportDatas;
            }
        }

        private sealed class AcceptanceProtocolReportBuilder : ReportBuilderBase
        {
            public AcceptanceProtocolReportBuilder(IPrintingSettingsStore printingSettingsStore, IWebClient webClient)
                : base(printingSettingsStore, webClient)
            {
            }

            protected override Tuple<string, string> GetHeader(OrderDto order, bool todayAsIssueDate)
            {
                DateTime issueDate = GetIssueDate(order);

                string documentName = order.LegalEntity?.Id == LegalEntityIds.TelemartId
                    ? "Замовлення"
                    : "Видаткова накладна";

                string headerFirstLine = $"{documentName} №{order.Id.ToString(CultureInfo.InvariantCulture)}";
                FormattableString headerSecondLine = $"від {issueDate:d MMMM yyyy}";

                return new Tuple<string, string>(headerFirstLine, headerSecondLine.ToString(UaCulture));
            }

            protected override IEnumerable<OrderPropertyReportData> GetProperties(OrderDto order, string contractorName, string cityName)
            {
                string d1 = order.DeliveryTime?.ToString("d", UaCulture) ?? "?";
                string d2 = order.DeliveryTimeTo?.ToString("d", UaCulture) ?? "?";
                string deliveryTime = $"{d1} - {d2}";
                string address = string.IsNullOrWhiteSpace(order.Address)
                    ? cityName
                    : $"{cityName}, {order.Address}";

                yield return new OrderPropertyReportData("Покупець:", order.Fio);
                yield return new OrderPropertyReportData("Телефон:", string.Join(", ", new[] { order.Phone, order.Phone2 }.Where(x => !string.IsNullOrWhiteSpace(x))));
                yield return new OrderPropertyReportData("Адреса:", address);
                yield return new OrderPropertyReportData("Час доставки:", deliveryTime);
            }
        }

        private sealed class ChequeReportBuilder : ReportBuilderBase
        {
            public ChequeReportBuilder(IPrintingSettingsStore printingSettingsStore, IWebClient webClient)
                : base(printingSettingsStore, webClient)
            {
            }

            protected override Tuple<string, string> GetHeader(OrderDto order, bool todayAsIssueDate)
            {
                DateTime issueDate = GetIssueDate(order);
                return new Tuple<string, string>($"Товарний чек №{order.Id.ToString(CultureInfo.InvariantCulture)}", $"від {issueDate.ToString("D", UaCulture)}");
            }

            protected override IEnumerable<OrderPropertyReportData> GetProperties(OrderDto order, string contractorName, string cityName)
            {
                yield return new OrderPropertyReportData("Одержувач:", order.Fio);
            }
        }

        private sealed class WaybillReportBuilder : ReportBuilderBase
        {
            public WaybillReportBuilder(IPrintingSettingsStore printingSettingsStore, IWebClient webClient)
               : base(printingSettingsStore, webClient)
            {
            }

            protected override Tuple<string, string> GetHeader(OrderDto order, bool todayAsIssueDate)
            {
                DateTime issueDate = GetIssueDate(order);
                return new Tuple<string, string>($"Накладна №{order.Id.ToString(CultureInfo.InvariantCulture)}", $"від {issueDate.ToString("D", UaCulture)}");
            }

            protected override IEnumerable<OrderPropertyReportData> GetProperties(OrderDto order, string contractorName, string cityName)
            {
                yield return new OrderPropertyReportData("Покупець:", contractorName);
                yield return new OrderPropertyReportData("Одержувач:", order.Fio);
            }
        }

        private sealed class TapeChequeReportBuilder : ReportBuilderBase
        {
            public TapeChequeReportBuilder(IPrintingSettingsStore printingSettingsStore, IWebClient webClient)
               : base(printingSettingsStore, webClient)
            {
            }

            protected override Tuple<string, string> GetHeader(OrderDto order, bool todayAsIssueDate)
            {
                return new Tuple<string, string>($"ТОВАРНИЙ ЧЕК №{order.Id.ToString(CultureInfo.InvariantCulture)}", null);
            }

            protected override IEnumerable<OrderPropertyReportData> GetProperties(OrderDto order, string contractorName, string cityName)
            {
                yield return new OrderPropertyReportData("Замовлення:", order.Id.ToString(CultureInfo.InvariantCulture));
                yield return new OrderPropertyReportData("Дата:", GetIssueDate(order).ToString("dd.MM.yyyy HH:mm:ss"));
                yield return new OrderPropertyReportData("Покупець:", order.Fio);
            }

            protected override Task<IReport> GetReportAsync(OrderReportData reportData)
            {
                return Task.FromResult<IReport>(new OrderTapeChequeReport { DataSource = new[] { reportData } });
            }
        }
    }
}