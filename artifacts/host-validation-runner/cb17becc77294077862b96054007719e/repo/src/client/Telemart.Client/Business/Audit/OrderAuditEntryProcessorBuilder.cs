using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Business.Audit.Entry;
using Telemart.Client.Business.Audit.Property;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Business.Audit
{
    public sealed class OrderAuditEntryProcessorBuilder : IAuditEntryProcessorBuilder
    {
        private Dictionary<int, string> cities;
        private Dictionary<int, string> contractors;
        private Dictionary<int, string> employees;
        private Dictionary<int, string> languages;
        private Dictionary<int, string> stateChangeReasons;
        private Dictionary<int, string> warehouses;
        private Dictionary<int, string> orderStates;
        private Dictionary<int, string> subdivisions;
        private Dictionary<int, string> carries;
        private Dictionary<int, string> workPlaces;
        private Dictionary<int, string> paymentStates;
        private Dictionary<int, string> orderSources;
        private Dictionary<int, string> eventTypes;

        public OrderAuditEntryProcessorBuilder(IWebClient webClient, IDictionaries dictionaries)
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
                new HashSet<string>(GetIgnoredPropertyNames()),
                new HashSet<(string, string)>(GetIgnorePropertyByEntryTypeName()));

            return processor;
        }

        private static string GetCaption(AuditEntryDto auditEntry, IReadOnlyDictionary<int, string> employees)
        {
            string createdOn = auditEntry.CreatedOn.ToString("dd.MM.yy HH:mm:ss");
            string createdBy = employees.GetValueOrDefault(auditEntry.CreatedBy ?? 0);

            return $"{createdOn} {createdBy}";
        }

        private static IEnumerable<string> GetIgnoredPropertyNames()
        {
            yield return "Version";
            yield return "TotalCostUsd";
            yield return "EmployeeLockId";
        }

        private static IEnumerable<(string, string)> GetIgnorePropertyByEntryTypeName()
        {
            yield return ("TaskEntity", "Deadline");
            yield return ("TaskEntity", "Description");
            yield return ("TaskEntity", "EmployeeId");
            yield return ("TaskEntity", "ModifiedOn");
            yield return ("TaskEntity", "ModifiedBy");
            yield return ("TaskEntity", "StateId");
            yield return ("TaskEntity", "CreatedBy");
            yield return ("TaskEntity", "CreatedOn");
            yield return ("TaskEntity", "TypeId");
            yield return ("TaskEntity", "Task");
            yield return ("Event", "Comment");
            yield return ("Event", "CreatedBy");
            yield return ("Event", "CreatedOn");
            yield return ("Event", "CreatedOn");
            yield return ("Event", "EntityId");
            yield return ("Event", "TypeId");
        }

        private Dictionary<string, IAuditEntryPropertyProcessor> GetPropertyProcessors()
        {
            IAuditEntryPropertyProcessor createdByPropertyProcessor = new IntDictionaryAuditEntryPropertyProcessor("Создал", employees);
            IAuditEntryPropertyProcessor languageIdPropertyProcessor = new IntDictionaryAuditEntryPropertyProcessor("Язык", languages);
            IAuditEntryPropertyProcessor subdivisionPropertyProcessor = new IntDictionaryAuditEntryPropertyProcessor("Подразделение", subdivisions);
            IAuditEntryPropertyProcessor confirmedByPropertyProcessor = new IntDictionaryAuditEntryPropertyProcessor("Ответственный", employees);
            IAuditEntryPropertyProcessor courierEmployeeIdPropertyProcessor = new IntDictionaryAuditEntryPropertyProcessor("Курьер", employees);
            IAuditEntryPropertyProcessor stateChangeReasonPropertyProcessor = new IntDictionaryAuditEntryPropertyProcessor("Причина изменения статуса", stateChangeReasons);
            IAuditEntryPropertyProcessor receiveTimePropertyProcessor = new DateAuditEntryPropertyProcessor("Заберет", DateFormattingRules.FullDateTimeFormat);
            IAuditEntryPropertyProcessor createdOnPropertyProcessor = new DateAuditEntryPropertyProcessor("Создан", DateFormattingRules.FullDateTimeFormat);

            return new Dictionary<string, IAuditEntryPropertyProcessor>
            {
                ["Order.PackedOn"] = new DateAuditEntryPropertyProcessor("Упакован", DateFormattingRules.FullDateTimeFormat),
                ["Order.DateComplete"] = receiveTimePropertyProcessor,
                ["Order.ReceiveTime"] = receiveTimePropertyProcessor,
                ["Order.Date"] = createdOnPropertyProcessor,
                ["Order.CreatedOn"] = createdOnPropertyProcessor,
                ["Order.DeliveryTime"] = new DateAuditEntryPropertyProcessor("Дата доставки с", DateFormattingRules.FullDateTimeFormat),
                ["Order.DeliveryTimeTo"] = new DateAuditEntryPropertyProcessor("Дата доставки по", DateFormattingRules.FullDateTimeFormat),
                ["Order.CompletedOn"] = new DateAuditEntryPropertyProcessor("Завершен", DateFormattingRules.FullDateTimeFormat),
                ["Order.WarehouseId"] = new IntDictionaryAuditEntryPropertyProcessor("Склад", warehouses),
                ["Order.AssemblyWarehouseId"] = new IntDictionaryAuditEntryPropertyProcessor("Склад сборки", warehouses),
                ["Order.AdditionalServiceWarehouseId"] = new IntDictionaryAuditEntryPropertyProcessor("Склад услуг", warehouses),
                ["Order.BufferWarehouseId"] = new IntDictionaryAuditEntryPropertyProcessor("Буферный склад", warehouses),
                ["Order.EmployeeCreateId"] = createdByPropertyProcessor,
                ["Order.CreatedBy"] = createdByPropertyProcessor,
                ["Order.PackedBy"] = new IntDictionaryAuditEntryPropertyProcessor("Упаковал", employees),
                ["Order.EmployeeResponsibleId"] = confirmedByPropertyProcessor,
                ["Order.ConfirmedBy"] = confirmedByPropertyProcessor,
                ["Order.CourierEmployeeId"] = courierEmployeeIdPropertyProcessor,
                ["Order.OrderStateChangeReasonId"] = stateChangeReasonPropertyProcessor,
                ["Order.CompletedBy"] = new IntDictionaryAuditEntryPropertyProcessor("Завершил", employees),
                ["Order.StateId"] = new IntDictionaryAuditEntryPropertyProcessor("Статус", orderStates),
                ["Order.SubDivisionId"] = subdivisionPropertyProcessor,
                ["Order.SubdivisionId"] = subdivisionPropertyProcessor,
                ["Order.CarryId"] = new IntDictionaryAuditEntryPropertyProcessor("Доставка", carries),
                ["Order.CityId"] = new IntDictionaryAuditEntryPropertyProcessor("Город", cities),
                ["Order.ClientId"] = new IntDictionaryAuditEntryPropertyProcessor("Клиент", contractors),
                ["Order.CreditStateId"] = new IntDictionaryAuditEntryPropertyProcessor("Статус оплаты", paymentStates),
                ["Order.PaymentStateId"] = new IntDictionaryAuditEntryPropertyProcessor("Статус оплаты", paymentStates),
                ["Order.LanguageId"] = languageIdPropertyProcessor,
                ["Order.CustomerComment"] = new TitleAuditEntryPropertyProcessor("Комментарий покупателя"),
                ["Order.SystemComment"] = new TitleAuditEntryPropertyProcessor("Комментарий системы"),
                ["Order.EmployeeComment"] = new TitleAuditEntryPropertyProcessor("Комментарий сотрудника"),
                ["Order.ReadyForPacking"] = new TitleAuditEntryPropertyProcessor("Готов к упаковке"),
                ["Order.Phone"] = new TitleAuditEntryPropertyProcessor("Телефон"),
                ["Order.PackageTtn"] = new TitleAuditEntryPropertyProcessor("ТТН"),
                ["Order.PackageReturnTtn"] = new TitleAuditEntryPropertyProcessor("Возвратная ТТН"),
                ["Order.PackageWeight"] = new TitleAuditEntryPropertyProcessor("Вес посылки"),
                ["Order.ReferralCustomerId"] = new IntDictionaryAuditEntryPropertyProcessor("Реферал", employees),
                ["Order.Address"] = new TitleAuditEntryPropertyProcessor("Адрес"),
                ["Order.PackageDeliveryCost"] = new TitleAuditEntryPropertyProcessor("Стоимость доставки"),
                ["Order.SmsSent"] = new TitleAuditEntryPropertyProcessor("СМС отправлено"),
                ["Order.LastName"] = new TitleAuditEntryPropertyProcessor("Фамилия"),
                ["Order.FirstName"] = new TitleAuditEntryPropertyProcessor("Имя"),
                ["Order.MiddleName"] = new TitleAuditEntryPropertyProcessor("Отчество"),
                ["Order.BasedOnServiceRequestId"] = new TitleAuditEntryPropertyProcessor("Основано по СЗ"),
                ["Order.MinInvoiceCloseDate"] = new DateAuditEntryPropertyProcessor("Мин. время закрытия накл.", DateFormattingRules.FullDateTimeFormat),
                ["Order.CompletedOnFiscalRegistrar"] = new TitleAuditEntryPropertyProcessor("Проведено по РРО"),
                ["Order.WorkPlaceId"] = new TitleAuditEntryPropertyProcessor("Место работы"),
                ["Order.DeliveryData"] = new TitleAuditEntryPropertyProcessor("Параметры доставки"),
                ["Order.Options"] = new TitleAuditEntryPropertyProcessor("Опции"),
                ["Order.PaymentData"] = new TitleAuditEntryPropertyProcessor("Параметры оплаты"),
                ["Order.OrderSourceId"] = new IntDictionaryAuditEntryPropertyProcessor("Источник", orderSources),
                ["TaskEntity.Documents"] = new JsonDictionaryAuditEntryPropertyProcessor<int>("Причина инициирования распаковки", stateChangeReasons, "order_state_change_reason_id", "Task"),
                ["TaskEntity.Id"] = new TitleAuditEntryPropertyProcessor("Задача"),
                ["Event.Documents"] = new JsonDictionaryAuditEntryPropertyProcessor<int>("Причина инициирования распаковки", stateChangeReasons, "order_state_change_reason_id", "TypeId", eventTypes),
                ["Event.Id"] = new TitleAuditEntryPropertyProcessor("Событие")
            };
        }

        private Task FetchDictionariesAsync()
        {
            orderStates = Dictionaries.GetItems<OrderStatus>().ToDictionary(x => x.Id, y => y.Name);
            subdivisions = Dictionaries.GetItems<Subdivision>().ToDictionary(x => x.Id, y => y.Name);
            carries = Dictionaries.GetItems<CarryType>().ToDictionary(x => x.Id, y => y.Name);
            paymentStates = Dictionaries.GetItems<PaymentState>().ToDictionary(x => x.Id, y => y.Name);
            languages = Dictionaries.GetItems<Language>().ToDictionary(x => x.Id, x => x.Name);
            orderSources = Dictionaries.GetItems<OrderSourceType>().ToDictionary(x => x.Id, x => x.Name);
            eventTypes = Dictionaries.GetItems<EventType>().ToDictionary(x => x.Id, x => x.Name);

            return Task.WhenAll(
                FetchWarehouseAsync(),
                FetchEmployeesAsync(),
                FetchStateChangeReasonsAsync(),
                FetchCitiesAsync(),
                FetchContractorsAsync());
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

        private async Task FetchStateChangeReasonsAsync()
        {
            List<OrderStateChangeReasonDto> stateChangeReasonsDto = await WebClient.ExecuteApiRequestAsync(new QueryOrderStateChangeReasons(true));
            stateChangeReasons = stateChangeReasonsDto.ToDictionary(x => x.Id, x => x.Name);
        }

        private async Task FetchWarehouseAsync()
        {
            PagedResult<WarehouseDto> result = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);
            warehouses = result.Data.ToDictionary(x => x.Id, x => x.Name);
        }
    }
}