using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.InvoiceBudget;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.InvoiceBudget;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public sealed class InvoiceEditBudgetsViewModel : TelemartDialogViewModelBase
    {
        private readonly IMapper _mapper;
        private readonly IErrorHandler _errorHandler;

        public InvoiceEditBudgetsViewModel(
            IMapper mapper,
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _mapper = mapper;
            _errorHandler = errorHandler;

            AddCommand = new DelegateCommand(Add);
            RemoveCommand = new DelegateCommand(Remove);
        }

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand RemoveCommand { get; }

        public ObservableCollection<InvoiceBudgetViewItem> Budgets
        {
            get { return GetProperty(() => Budgets); }
            set { SetProperty(() => Budgets, value); }
        }

        public InvoiceBudgetViewItem SelectedBudget
        {
            get { return GetProperty(() => SelectedBudget); }
            set { SetProperty(() => SelectedBudget, value); }
        }

        protected override async Task HandleLoadedAsync()
        {
            List<InvoiceBudgetDto> budgets = await WebClient.ExecuteApiRequestAsync(new QueryInvoiceBudgets());

            Budgets = budgets.Select(x => _mapper.Map<InvoiceBudgetViewItem>(x)).OrderByDescending(x => x.DateTo).ToObservableCollection();

            await base.HandleLoadedAsync();

            Title = "Бюджеты";
        }

        protected override async Task HandleOkAsync()
        {
            if (Budgets.Any(x => IDataErrorInfoHelper.HasErrors(x)))
            {
                MessageFacadeService.ShowNotificationError("В табличной части присутствуют ошибки");
                return;
            }

            SaveInvoiceBudgetsDto saveInvoiceBudgetsDto = new SaveInvoiceBudgetsDto(Budgets.Select(x => _mapper.Map<InvoiceBudgetDto>(x)).ToArray());

            Result result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new SaveInvoiceBudgets(saveInvoiceBudgetsDto)),
                "сохранении бюджетов",
                "Бюджеты сохранены",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                CloseOk();
            }
        }

        private void Add()
        {
            InvoiceBudgetViewItem newBudget = new InvoiceBudgetViewItem()
            {
                DateFrom = Budgets.Any(x => x.DateFrom.HasValue) ? Budgets.Where(x => x.DateFrom.HasValue).Select(x => x.DateTo!.Value).Max().AddDays(1) : null
            };

            Budgets.Insert(0, newBudget);
        }

        private void Remove()
        {
            Budgets.Remove(SelectedBudget);
        }
    }
}