using System;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.ServiceRequest.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    public class ServiceRequestChangeProductViewModel : TelemartDialogViewModelBase
    {
        private int newProductId;
        private int serviceRequestId;

        public ServiceRequestChangeProductViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            SelectProductCommand = new DelegateCommand(SelectProduct);
        }

        public IDelegateCommand SelectProductCommand { get; }

        public string CurrentProductName
        {
            get { return GetProperty(() => CurrentProductName); }
            private set { SetProperty(() => CurrentProductName, value); }
        }

        public string NewProductName
        {
            get { return GetProperty(() => NewProductName); }
            set { SetProperty(() => NewProductName, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ServiceRequestChangeProductViewModel> builder)
        {
            builder.Property(x => x.NewProductName)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            ServiceRequestChangeProductParameter parameter = (ServiceRequestChangeProductParameter)Parameter;

            CurrentProductName = parameter.ProductName;
            serviceRequestId = parameter.ServiceRequestId;

            Title = "Выберите товар";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                await WebClient.ExecuteApiRequestAsync(new ChangeProductServiceRequest(serviceRequestId, newProductId));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении товара");
                ShowValidationResultView("Ошибки при изменении товара", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to change service request product");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении товара");
                Logger.LogError(exception, "Error while changing service request product");
            }
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

                newProductId = product.Id;
                NewProductName = product.Name;
            }
        }
    }
}