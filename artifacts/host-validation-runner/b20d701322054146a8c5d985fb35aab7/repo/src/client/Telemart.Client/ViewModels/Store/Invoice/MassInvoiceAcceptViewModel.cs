using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public sealed class MassInvoiceAcceptViewModel : TelemartDialogViewModelBase
    {
        public MassInvoiceAcceptViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            SelectedInvoices = new ObservableCollection<MassInvoiceAcceptViewItem>();
        }

        public ReadOnlyObservableCollection<MassInvoiceAcceptViewItem> Invoices
        {
            get { return GetProperty(() => Invoices); }
            private set { SetProperty(() => Invoices, value); }
        }

        public ObservableCollection<MassInvoiceAcceptViewItem> SelectedInvoices
        {
            get { return GetProperty(() => SelectedInvoices); }
            set { SetProperty(() => SelectedInvoices, value); }
        }

        public int OrganizationGroupIndex
        {
            get { return GetProperty(() => OrganizationGroupIndex); }
            set { SetProperty(() => OrganizationGroupIndex, value); }
        }

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            OrganizationGroupIndex = 0;

            InvoiceFilteringItem filteringItem = new InvoiceFilteringItem()
            {
                InvoiceStatesIds = new[] { InvoiceState.Arrived.Id },
            };

            PagedResult<InvoiceDto> invoices = await WebClient.ExecuteApiRequestAsync(new QueryInvoices(filteringItem));

            MassInvoiceAcceptParameter parameter = (MassInvoiceAcceptParameter)Parameter;

            Invoices = invoices.Data
                .GroupBy(x => x.SupplierOrganization)
                .Where(x => x.Count() > 1)
                .SelectMany(x => x)
                .Select(x => Mapper.Map<MassInvoiceAcceptViewItem>(x))
                .ToReadOnlyObservableCollection();

            if (!Invoices.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Накладных для массовой приемки не обнаружено");
                Close();
            }

            Invoices = parameter.Invoices.ToReadOnlyObservableCollection();

            Title = "Выберите накладные для массовой сверки";
        }

        protected override Task HandleOkAsync()
        {
            if (!SelectedInvoices.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Ничего не выбрано");
            }
            else if (SelectedInvoices.GroupBy(x => x.SupplierOrganization).Count() > 1)
            {
                MessageFacadeService.ShowNotificationError("У выбранных накладных разные организации поставщиков");
            }
            else
            {
                IsOk = true;
                Close();
            }

            return Task.CompletedTask;
        }
    }
}