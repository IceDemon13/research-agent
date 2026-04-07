using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Business.Audit.Entry;
using Telemart.Client.Business.Audit.Property;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Business.Audit
{
    public sealed class OrderProductAuditEntryProcessorBuilder : IAuditEntryProcessorBuilder
    {
        private Dictionary<int, string> employees;
        private Dictionary<int, string> warehouses;

        private Dictionary<int, string> orderProductStates;
        private Dictionary<int, string> orderProductSources;
        private Dictionary<int, string> currencies;

        public OrderProductAuditEntryProcessorBuilder(IWebClient webClient, IDictionaries dictionaries)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
        }

        private IWebClient WebClient { get; }

        private IDictionaries Dictionaries { get; }

        public async Task<IAuditEntryProcessor> BuildAsync()
        {
            await FetchDictionariesAsync();

            IAuditEntryProcessor processor = new AuditEntryProcessor(
                GetPropertyProcessors(),
                x => GetCaption(x, employees),
                new HashSet<string>(GetIgnoredPropertyNames()));

            return processor;
        }

        private static string GetCaption(AuditEntryDto auditEntry, IReadOnlyDictionary<int, string> employees)
        {
            string createdOn = auditEntry.CreatedOn.ToString("dd.MM.yy HH:mm:ss");
            string createdBy = employees.GetValueOrDefault(auditEntry.CreatedBy ?? 0);

            return $"{createdOn} {createdBy} ({auditEntry.EntityId})";
        }

        private Dictionary<string, IAuditEntryPropertyProcessor> GetPropertyProcessors()
        {
            return new Dictionary<string, IAuditEntryPropertyProcessor>
            {
                ["OrderProduct.StateId"] = new IntDictionaryAuditEntryPropertyProcessor("Статус", orderProductStates),
                ["OrderProduct.SourceId"] = new IntDictionaryAuditEntryPropertyProcessor("Источник", orderProductSources),
                ["OrderProduct.CurrencyOutId"] = new IntDictionaryAuditEntryPropertyProcessor("Валюта", currencies),
                ["OrderProduct.WarehouseId"] = new IntDictionaryAuditEntryPropertyProcessor("Склад", warehouses),
                ["OrderProduct.EmployeeSupId"] = new IntDictionaryAuditEntryPropertyProcessor("Закупил", employees)
            };
        }

        private Task FetchDictionariesAsync()
        {
            orderProductSources = Dictionaries.GetItems<OrderProductSourceType>().ToDictionary(x => x.Id, y => y.Name);
            orderProductStates = Dictionaries.GetItems<OrderProductStatus>().ToDictionary(x => x.Id, y => y.Name);
            currencies = Dictionaries.GetCurrencies().ToDictionary(x => x.Id, y => y.Name);

            return Task.WhenAll(FetchWarehouseAsync(), FetchEmployeesAsync());
        }

        private async Task FetchEmployeesAsync()
        {
            PagedResult<EmployeeDto> result = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);
            employees = result.Data.ToDictionary(x => x.Id, x => x.Name);
        }

        private async Task FetchWarehouseAsync()
        {
            PagedResult<WarehouseDto> result = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);
            warehouses = result.Data.ToDictionary(x => x.Id, x => x.Name);
        }

        private IEnumerable<string> GetIgnoredPropertyNames()
        {
            yield return "Version";
            yield return "CurrencyId";
            yield return "Price";
            yield return "WarrantyId";
            yield return "LabelId";
            yield return "PromoId";
            yield return "PriceUsd";
            yield return "Reserved";
        }
    }
}