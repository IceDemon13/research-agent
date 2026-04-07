using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Create
{
    public sealed class SelectOrderPageViewModel : WizardPageViewModelBase<CreateServiceRequestModel>, ISupportWizardNextCommand
    {
        private readonly ILogger _logger;
        private IReadOnlyCollection<ContractorDto> _contractors;

        public SelectOrderPageViewModel(IWebClient webClient, IDictionaries dictionaries, IMessageFacadeService messageFacadeService, ILogger<SelectOrderPageViewModel> logger)
        {
            WebClient = webClient;
            Dictionaries = dictionaries;
            MessageFacadeService = messageFacadeService;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            SelectProductCommand = new DelegateCommand(SelectProduct);
            ClearProductCommand = new DelegateCommand(ClearProduct);

            _logger = logger;
        }

        public SelectOrderPageViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        public IDelegateCommand ClearProductCommand { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<KeyValuePair<string, SearchOrderMode>> SearchModes
        {
            get { return GetProperty(() => SearchModes); }
            private set { SetProperty(() => SearchModes, value); }
        }

        public ReadOnlyObservableCollection<ContractorDto> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        #endregion

        public bool CanGoForward { get; } = true;

        public override string Description { get; } = "Уточните параметры поиска";

        public override string Header { get; } = "Поиск заказа";

        private IDictionaries Dictionaries { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IWebClient WebClient { get; }

        private IWizardService WizardService => GetService<IWizardService>();

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private CurrentWindowService CurrentWindowService => (CurrentWindowService)GetService<ICurrentWindowService>(ServiceSearchMode.PreferParents);

        private string[] ValidatableProperties { get; } =
        {
            nameof(CreateServiceRequestModel.SearchMode),
            nameof(CreateServiceRequestModel.SearchOrderId),
            nameof(CreateServiceRequestModel.SearchSerialNumber),
            nameof(CreateServiceRequestModel.SearchContractorId),
            nameof(CreateServiceRequestModel.SearchProductName)
        };

        public void OnGoForward(CancelEventArgs e)
        {
            if (OnGoForwardInternal())
            {
                WizardService.NavigateToView<SelectProductPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);
            }

            e.Cancel = true;
        }

        protected override bool GetCanCancel()
        {
            return !IsLongOperationInProgress;
        }

        protected override void OnInitializeInDesignMode()
        {
            SearchModes = GetSearchModes().ToReadOnlyObservableCollection();
        }

        private static IEnumerable<KeyValuePair<string, SearchOrderMode>> GetSearchModes()
        {
            yield return new KeyValuePair<string, SearchOrderMode>("Заказ", SearchOrderMode.OrderNumber);
            yield return new KeyValuePair<string, SearchOrderMode>("SN", SearchOrderMode.SerialNumber);
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
                switch (Model.SearchMode)
                {
                    case SearchOrderMode.OrderNumber:
                        {
                            success = SetOrder(Model.SearchOrderId.Value);
                            break;
                        }

                    case SearchOrderMode.SerialNumber:
                        {
                            OrderSaleDto dto = SearchOrders();

                            if (dto != null)
                            {
                                success = SetOrder(dto.OrderId);

                                if (success)
                                {
                                    Model.ProductId = dto.ProductId;
                                    Model.SerialNumber = dto.SerialNumber;
                                }
                            }

                            break;
                        }

                    default:
                        throw new NotSupportedException();
                }
            }
            catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.Unauthorized)
            {
                MessageFacadeService.ShowNotificationError("Не хватает прав для выполнения операции");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
            finally
            {
                IsLongOperationInProgress = false;
                CurrentWindowService.ActualWindow.Closing -= ActualWindowClosing;
            }

            return success;
        }

        private OrderSaleDto SearchOrders()
        {
            OrderSaleDto dto = null;

            QuerySales gatewayRequest = new QuerySales(Model.SearchSerialNumber, Model.SearchContractorId, Model.SearchProductId);

            List<OrderSaleDto> orders = WebClient.ExecuteApiRequest(gatewayRequest);

            if (orders.Any())
            {
                if (orders.Count == 1)
                {
                    dto = orders.First();
                }
                else if (orders.Select(x => x.ProductId).Distinct().Count() > 1)
                {
                    MessageFacadeService.ShowNotificationWarning("Такой SN есть у нескольких товаров, уточните запрос");
                }
                else if (orders.Select(x => x.OrderClientId).Distinct().Count() > 1)
                {
                    MessageFacadeService.ShowNotificationWarning("Такой SN продавался нескольким контрагентам, уточните запрос");
                }
                else
                {
                    dto = orders.OrderByDescending(x => x.OrderCreatedOn).First();
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Заказ с такими параметрами не найден");
            }

            return dto;
        }

        private bool SetOrder(int orderId)
        {
            bool success = false;

            try
            {
                OrderDto order = WebClient.ExecuteApiRequest(new QueryOrder(orderId));
                IReadOnlyCollection<ProductAttributesDto> attributes = WebClient.ExecuteApiRequest(new QueryOrderProductAttributes(order.Id));
                List<CashboxDto> cashboxes = WebClient.ExecuteApiRequest(new QueryCashboxes(), true);

                ContractorDto contractor = _contractors.First(x => x.Id == order.ClientId);

                if (order.LegalEntity != null)
                {
                    cashboxes = cashboxes
                        .ForLegalEntity(order.LegalEntity)
                        .ToList();
                }

                Model.SetOrderData(Dictionaries, order, contractor, attributes, cashboxes);

                success = true;
            }
            catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.NotFound)
            {
                MessageFacadeService.ShowNotificationError($"Заказ №{orderId.ToString(CultureInfo.InvariantCulture)} не найден");
            }

            return success;
        }

        private async Task HandleLoadedAsync()
        {
            SearchModes = GetSearchModes().ToReadOnlyObservableCollection();

            _contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            Contractors = _contractors
                .Where(x => !x.IsFolder && x.IsClient && x.Active && WebClient.AuthenticatedEmployee.AllowSubdivisions.Contains(x.SubdivisionId))
                .OrderBy(x => x.SubdivisionId)
                .ThenBy(x => x.Name)
                .ToReadOnlyObservableCollection();

            Model.ClearOrderData();
        }

        private void SelectProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem product = nomenclatureViewModel.GetSelectedItems().First();

                Model.SearchProductId = product.Id;
                Model.SearchProductName = product.Name;
            }
        }

        private void ClearProduct()
        {
            Model.SearchProductId = null;
            Model.SearchProductName = null;
        }
    }
}