using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business.Audit.Entry;
using Telemart.Client.Business.Audit.Property;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Business.Audit
{
    public class ServiceRequestAuditEntryProcessorBuilder : IAuditEntryProcessorBuilder
    {
        private Dictionary<int, string> cities;
        private Dictionary<int, string> contractors;
        private Dictionary<int, string> employees;
        private Dictionary<int, string> warehouses;
        private Dictionary<int, string> cashboxes;

        private Dictionary<int, string> serviceRequestStates;
        private Dictionary<int, string> subdivisions;
        private Dictionary<int, string> carrys;
        private Dictionary<int, string> paymentTypes;
        private Dictionary<string, string> serviceRequestRequirements;
        private Dictionary<string, string> serviceRequestResolutions;
        private Dictionary<string, string> serviceRequestLocations;

        public ServiceRequestAuditEntryProcessorBuilder(IWebClient webClient, IDictionaries dictionaries)
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
                a => GetCaption(a, employees),
                new HashSet<string>(GetIgnoredPropertyNames()));

            return processor;
        }

        private static string GetCaption(AuditEntryDto auditEntry, IDictionary<int, string> employees)
        {
            string createdOn = auditEntry.CreatedOn.ToString("yyyy-MM-dd HH:mm");
            string createdBy = employees.GetValueOrDefault(auditEntry.CreatedBy ?? 0);

            return $"{createdOn} {createdBy}";
        }

        private Dictionary<string, IAuditEntryPropertyProcessor> GetPropertyProcessors()
        {
            return new Dictionary<string, IAuditEntryPropertyProcessor>
            {
                ["ServiceRequest.ContractorId"] = new IntDictionaryAuditEntryPropertyProcessor("Клиент", contractors),
                ["ServiceRequest.SubdivisionId"] = new IntDictionaryAuditEntryPropertyProcessor("Подразделение", subdivisions),
                ["ServiceRequest.StateId"] = new IntDictionaryAuditEntryPropertyProcessor("Статус", serviceRequestStates),
                ["ServiceRequest.Requirement"] = new StringDictionaryAuditEntryPropertyProcessor("Требование", serviceRequestRequirements),
                ["ServiceRequest.RequirementPaymentId"] = new IntDictionaryAuditEntryPropertyProcessor("Оплата", paymentTypes),
                ["ServiceRequest.RequirementCashboxId"] = new IntDictionaryAuditEntryPropertyProcessor("Касса", cashboxes),
                ["ServiceRequest.RequirementResolution"] = new StringDictionaryAuditEntryPropertyProcessor("Решение", serviceRequestResolutions),
                ["ServiceRequest.EmployeeId"] = new IntDictionaryAuditEntryPropertyProcessor("Ответственный", employees),
                ["ServiceRequest.CityId"] = new IntDictionaryAuditEntryPropertyProcessor("Город", cities),
                ["ServiceRequest.WarehouseInId"] = new IntDictionaryAuditEntryPropertyProcessor("Склад", warehouses),
                ["ServiceRequest.CarryInId"] = new IntDictionaryAuditEntryPropertyProcessor("Доставка", carrys),
                ["ServiceRequest.Location"] = new StringDictionaryAuditEntryPropertyProcessor("Расположение", serviceRequestLocations),
                ["ServiceRequest.WarehouseLocationId"] = new IntDictionaryAuditEntryPropertyProcessor("Склад расположения", warehouses),
            };
        }

        private async Task FetchDictionariesAsync()
        {
            await Task.WhenAll(FetchWarehouseAsync(), FetchEmployeesAsync(), FetchCitiesAsync(), FetchContractorsAsync(), FetchCashboxesAsync());

            serviceRequestStates = Dictionaries.GetItems<ServiceRequestState>().ToDictionary(x => x.Id, y => y.Name);
            subdivisions = Dictionaries.GetItems<Subdivision>().ToDictionary(x => x.Id, y => y.Name);
            carrys = Dictionaries.GetItems<CarryType>().ToDictionary(x => x.Id, y => y.Name);
            serviceRequestRequirements = Dictionaries.GetItems<ServiceRequestRequirement>().ToDictionary(x => x.Value, y => y.Name);
            paymentTypes = Dictionaries.GetItems<Payment>().ToDictionary(x => x.Id, y => y.Name);
            serviceRequestResolutions = Dictionaries.GetItems<ServiceRequestResolution>().ToDictionary(x => x.Value, y => y.Name);
            serviceRequestLocations = Dictionaries.GetItems<ServiceRequestLocation>().ToDictionary(x => x.Value, y => y.Name);
        }

        private async Task FetchCitiesAsync()
        {
            PagedResult<CityDto> result = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true);
            cities = result.Data.ToDictionary(x => x.Id, x => x.Name);
        }

        private async Task FetchContractorsAsync()
        {
            PagedResult<ContractorDto> result = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true);
            contractors = result.Data.ToDictionary(x => x.Id, x => x.Name);
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

        private async Task FetchCashboxesAsync()
        {
            List<CashboxDto> cashboxDtos = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);
            cashboxes = cashboxDtos.ToDictionary(x => x.Id, x => x.Name);
        }

        private IEnumerable<string> GetIgnoredPropertyNames()
        {
            yield return "Id";
            yield return "ModifiedOn";
            yield return "ModifiedBy";
            yield return "EmployeeLockId";
        }
    }
}
