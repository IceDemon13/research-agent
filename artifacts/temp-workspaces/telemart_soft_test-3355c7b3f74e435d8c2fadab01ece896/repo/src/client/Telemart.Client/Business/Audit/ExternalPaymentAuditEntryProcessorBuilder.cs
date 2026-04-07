using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telemart.Client.Business.Audit.Entry;
using Telemart.Client.Business.Audit.Property;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;

namespace Telemart.Client.Business.Audit
{
    public class ExternalPaymentAuditEntryProcessorBuilder : IAuditEntryProcessorBuilder
    {
        private readonly IWebClient _webClient;
        private readonly IDictionaries _dictionaries;
        private Dictionary<int, string> _externalPaymentStates;
        private Dictionary<int, string> _employees;

        public ExternalPaymentAuditEntryProcessorBuilder(IWebClient webClient, IDictionaries dictionaries)
        {
            _webClient = webClient;
            _dictionaries = dictionaries;
        }

        public async Task<IAuditEntryProcessor> BuildAsync()
        {
            await FetchDictionariesAsync();

            IAuditEntryProcessor processor = new AuditEntryProcessor(
                GetPropertyProcessors(),
                x => GetCaption(x, _employees),
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
                ["ExternalPayment.PaymentStateId"] = new IntDictionaryAuditEntryPropertyProcessor("Статус", _externalPaymentStates),
                ["ExternalPayment.CreatedBy"] = new IntDictionaryAuditEntryPropertyProcessor("Создал", _employees),
                ["ExternalPayment.ModifiedBy"] = new IntDictionaryAuditEntryPropertyProcessor("Изменил", _employees),
                ["ExternalPayment.Params"] = new TitleAuditEntryPropertyProcessor("Параметры"),
                ["ExternalPayment.CreatedAmount"] = new TitleAuditEntryPropertyProcessor("Сумма при создании"),
                ["ExternalPayment.ReceivedAmount"] = new TitleAuditEntryPropertyProcessor("Сумма, которая была списана с карты"),
                ["ExternalPayment.Id"] = new TitleAuditEntryPropertyProcessor("Код"),
                ["ExternalPayment.CreatedOn"] = new TitleAuditEntryPropertyProcessor("Дата создания"),
                ["ExternalPayment.ModifiedOn"] = new TitleAuditEntryPropertyProcessor("Дата изменения")
            };
        }

        private Task FetchDictionariesAsync()
        {
            _externalPaymentStates = _dictionaries.GetItems<PaymentState>().ToDictionary(x => x.Id, y => y.Name);

            return Task.WhenAll(FetchEmployeesAsync());
        }

        private async Task FetchEmployeesAsync()
        {
            PagedResult<EmployeeDto> result = await _webClient.ExecuteApiRequestAsync(new QueryEmployees(), true);
            _employees = result.Data.ToDictionary(x => x.Id, x => x.Name);
        }

        private IEnumerable<string> GetIgnoredPropertyNames()
        {
            yield return "OrderId";
            yield return "PaymentId";
        }
    }
}