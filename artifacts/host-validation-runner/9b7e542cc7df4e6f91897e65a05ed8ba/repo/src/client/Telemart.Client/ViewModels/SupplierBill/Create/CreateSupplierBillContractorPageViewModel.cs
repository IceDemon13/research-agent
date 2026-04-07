using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.SupplierBill;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.SupplierBill;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.SupplierBill.Create
{
    public sealed class CreateSupplierBillContractorPageViewModel : WizardPageViewModelBase<CreateSupplierBillModel>, ISupportWizardNextCommand
    {
        public CreateSupplierBillContractorPageViewModel(IWebClient webClient, IMessageFacadeService messageFacadeService, ILogger<CreateSupplierBillContractorPageViewModel> logger)
        {
            WebClient = webClient;
            MessageFacadeService = messageFacadeService;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            Logger = logger;
        }

        public CreateSupplierBillContractorPageViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        #endregion

        public bool CanGoForward { get; } = true;

        public override string Description { get; } = "Данные счета";

        public override string Header { get; } = "Шаг 1";

        private IMessageFacadeService MessageFacadeService { get; }

        private IWebClient WebClient { get; }

        private ILogger Logger { get; }

        private IWizardService WizardService => GetService<IWizardService>();

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private CurrentWindowService CurrentWindowService => (CurrentWindowService)GetService<ICurrentWindowService>(ServiceSearchMode.PreferParents);

        private string[] ValidatableProperties { get; } =
        {
            nameof(CreateSupplierBillModel.Contractor),
            nameof(CreateSupplierBillModel.Date),
            nameof(CreateSupplierBillModel.Number)
        };

        public void OnGoForward(CancelEventArgs e)
        {
            if (OnGoForwardInternal())
            {
                WizardService.NavigateToView<CreateSupplierBillTextPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            }

            e.Cancel = true;
        }

        protected override bool GetCanCancel()
        {
            return !IsLongOperationInProgress;
        }

        protected override void OnInitializeInDesignMode()
        {
        }

        private void ActualWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private bool OnGoForwardInternal()
        {
            if (IDataErrorInfoHelper.HasErrors(Model, false, propertyFilter: x => ValidatableProperties.Contains(x.Name)))
            {
                return false;
            }

            bool success = false;

            CurrentWindowService.ActualWindow.Closing += ActualWindowClosing;

            IsLongOperationInProgress = true;

            try
            {
                IFilteringItem filteringItem = new SupplierBillFilteringItem(
                    null,
                    null,
                    Model.Number,
                    new List<int> { Model.Contractor.Id },
                    new List<int> { SupplierBillState.New.Id, SupplierBillState.Processed.Id, SupplierBillState.Completed.Id });

                PagedResult<SupplierBillDto> bills = WebClient.ExecuteApiRequest(new QuerySupplierBills(filteringItem));
                InvoiceDto invoice = WebClient.ExecuteApiRequest(new QueryInvoice(Model.InvoiceId.Value));

                if (bills.Data.Any())
                {
                    MessageFacadeService.ShowNotificationError($"Счет c номером \"{Model.Number}\" уже создан");
                }
                else if (invoice == null)
                {
                    MessageFacadeService.ShowNotificationError($"Накладная №{Model.InvoiceId} не найдена");
                }
                else if (invoice.StateId != InvoiceState.Received.Id)
                {
                    MessageFacadeService.ShowNotificationError("Накладная должна быть в статусе принята");
                }
                else if (invoice.SupplierId != Model.Contractor.Id)
                {
                    MessageFacadeService.ShowNotificationError($"Поставщик в накладной должен быть \"{Model.Contractor.Name}\"");
                }
                else
                {
                    success = true;
                }
            }
            catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.Unauthorized)
            {
                MessageFacadeService.ShowNotificationError("Не хватает прав для выполнения операции");
            }
            catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.NotFound)
            {
                MessageFacadeService.ShowNotificationError("Накладная не найдена");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при запросе накладной");
                ShowValidationResultView("Ошибки при запросе накладной", exception.GetErrorItems());
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
            }
            finally
            {
                IsLongOperationInProgress = false;
                CurrentWindowService.ActualWindow.Closing -= ActualWindowClosing;
            }

            return success;
        }

        private bool ShowValidationResultView(string title, IEnumerable<ValidationResultItem> validationItems)
        {
            ValidationResultViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);

            return viewModel.IsOk;
        }

        private async Task HandleLoadedAsync()
        {
            PagedResult<ContractorDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true);

            Model.Contractors = pagedResult.Data
                .Where(x => !x.IsFolder && x.IsSupplier && x.Active)
                .OrderBy(x => x.SubdivisionId)
                .ThenBy(x => x.Name)
                .ToReadOnlyObservableCollection();

            if (Model.ContractorId.HasValue)
            {
                Model.Contractor = Model.Contractors.FirstOrDefault(x => x.Id == Model.ContractorId);
            }
        }
    }
}