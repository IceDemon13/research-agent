using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Task.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Task;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Tasks
{
    public sealed class ProcessPickupProductsViewModel : TelemartDialogViewModelBase
    {
        private readonly IMapper mapper;
        private readonly IErrorHandler errorHandler;
        private readonly DocumentCommands documentCommands;
        private readonly IMessenger messenger;

        private TelemartEnumerableCompareHelper<PickupProductViewItem> compareHelper;
        private int taskId;

        public ProcessPickupProductsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMapper mapper,
            IMessenger messenger,
            DocumentCommands documentCommands,
            ProductInformationViewModel productInformationViewModel)
            : base(webClient, dictionaries, messageFacadeService)
        {
            this.mapper = mapper;
            this.errorHandler = errorHandler;
            this.messenger = messenger;
            this.documentCommands = documentCommands;

            ProductInformation = productInformationViewModel;
        }

        public ReadOnlyObservableCollection<PickupProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> AllCategories
        {
            get { return GetProperty(() => AllCategories); }
            set { SetProperty(() => AllCategories, value); }
        }

        public PickupProductViewItem SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value, SelectedProductChanged); }
        }

        public ReadOnlyObservableCollection<ProductPickupReason> Reasons
        {
            get { return GetProperty(() => Reasons); }
            private set { SetProperty(() => Reasons, value); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            private set { SetProperty(() => ProductInformation, value); }
        }

        public TaskDto TaskDto
        {
            get { return GetProperty(() => TaskDto); }
            private set { SetProperty(() => TaskDto, value); }
        }

        public override void OnClose(CancelEventArgs e)
        {
            if (!IsOk && compareHelper?.IsChanged() == true && !MessageFacadeService.Confirm("Закрыть диалог без сохранения изменений?"))
            {
                e.Cancel = true;
            }
            else
            {
                base.OnClose(e);
            }
        }

        protected override async Task HandleLoadedAsync()
        {
            PagedResult<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

            AllCategories = categories.Data.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();

            Reasons = Dictionaries.GetItems<ProductPickupReason>().ToReadOnlyObservableCollection();

            ProcessPickupProductsParameter parameter = (ProcessPickupProductsParameter)Parameter;

            Products = parameter.Products.Select(x => mapper.Map<PickupProductViewItem>(x)).ToReadOnlyObservableCollection();

            Products.Where(x => x.ReturnOnMainWarehouse || x.KeepOnPickup).ForEach(x => x.Processed = true);

            taskId = parameter.TaskId;

            compareHelper = new TelemartEnumerableCompareHelper<PickupProductViewItem>(Products);

            Title = "Товары по которым нужно принять решение";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            if (!compareHelper.IsChanged())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего обрабатывать");
                return;
            }

            ProcessPickupProductsDto saveDto = new ProcessPickupProductsDto(taskId, new PickupProductsDto(Products.Where(x => !x.Processed).Select(x => mapper.Map<PickupProductDto>(x)).ToList()));

            Result<TaskDto> result = await errorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new ProcessPickupProducts(saveDto)), "обработке товаров", "Товары обработаны", this, true);

            result.IfNotNull(_ =>
            {
                TaskDto = result.Data;
                CloseOk();
            });
        }

        private void SelectedProductChanged()
        {
            ProductInformation.ClearProduct();

            if (SelectedProduct != null)
            {
                ProductInformation.ProductId = new ProductInfoId(SelectedProduct.ProductId, Currency.UahId);
            }
        }
    }
}