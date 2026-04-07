using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.Showcase;
using Telemart.Client.Data.Requests.Features.Showcase.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Showcase;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Showcase
{
    public class ShowcaseViewModel : TelemartEditorViewModelBase<ShowcaseDto, ShowcaseParameter, ShowcaseViewItem>
    {
        private readonly bool canEdit;

        public ShowcaseViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            SelectProductCommand = new AsyncCommand(SelectProductAsync);
            RefreshQuantityCommand = new AsyncCommand<int>(RefreshQuantityAsync);

            canEdit = webClient.IsOperationAllowed(BusinessOperation.ShowcaseUpdate);
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public int PlanQuantity
        {
            get { return GetProperty(() => PlanQuantity); }
            set { SetProperty(() => PlanQuantity, value); }
        }

        public int FactQuantity
        {
            get { return GetProperty(() => FactQuantity); }
            set { SetProperty(() => FactQuantity, value); }
        }

        public IAsyncCommand SelectProductCommand { get; }

        protected override string CreatedActionMessage { get; } = "создан";

        protected override string EntityName { get; } = "Товар на витрине";

        protected override string UpdatedActionMessage { get; } = "сохранен";

        private IAsyncCommand RefreshQuantityCommand { get; }

        protected override bool CanEdit()
        {
            return base.CanEdit() && canEdit;
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChangedInternal(sender, e);

            switch (e.PropertyName)
            {
                case nameof(Model.WarehouseId):
                    try
                    {
                        RefreshQuantityCommand.Execute(Model.ProductParentCategoryId);
                    }
                    catch (Exception exception)
                    {
                        Logger.LogError(exception, "Failed to refresh showcase quantity");
                    }

                    break;
            }
        }

        protected override async Task HandleLoadedAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            Warehouses = warehouses
                .OrderByDescending(x => x.Position)
                .ThenBy(x => x.Name)
                .Where(x => x.Active == 1 && (x.TypeId == WarehouseKind.ShowCase.Id || x.TypeId == WarehouseKind.Pickup.Id))
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            if (EditorParameter.ProductId.HasValue)
            {
                ProductAttributesDto product = await WebClient.ExecuteApiRequestAsync(new QueryProductAttributes(EditorParameter.ProductId.Value));

                Model.ProductId = product.ProductId;
                Model.ProductName = product.Name;
                Model.ProductParentCategoryId = product.ParentCategoryId;
            }

            if (Model.ProductId.HasValue && Model.WarehouseId.HasValue)
            {
                await RefreshQuantityAsync(Model.ProductParentCategoryId);
            }
        }

        protected override Task<Result<ShowcaseDto>> CreateEntityAsync()
        {
            return WebClient.ExecuteApiRequestAsync(new CreateShowcase(Model.ProductId.Value, Model.WarehouseId.Value, Model.Capacity.Value, Model.Active));
        }

        protected override object CreateEntityMessage(ShowcaseDto dto, MessageType messageType)
        {
            return new ShowcaseMessage(dto, messageType);
        }

        protected override Task<ShowcaseDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryShowcase(id));
        }

        protected override Task<LockResponse<ShowcaseDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockShowcase(id));
        }

        protected override Task<LockResponse<ShowcaseDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockShowcase(id));
        }

        protected override void SetCreateTitle()
        {
            Title = "Добавление товара на витрину";
        }

        protected override void SetEditTitle()
        {
            Title = $"Товар на витрине №{Model.Id}";
        }

        protected override Task<Result<ShowcaseDto>> UpdateEntityAsync()
        {
            return WebClient.ExecuteApiRequestAsync(new UpdateShowcase(Model.Id, Model.Capacity.Value, Model.Active));
        }

        private async Task SelectProductAsync()
        {
            if (Model.WarehouseId is null)
            {
                MessageFacadeService.ShowNotificationWarning("Сначала выберите склад");
                return;
            }

            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (!nomenclatureViewModel.IsOk)
            {
                return;
            }

            NomenclatureViewItem result = nomenclatureViewModel.GetSelectedItems().First();

            Model.ProductId = result.Id;
            Model.ProductName = result.Name;
            Model.ProductParentCategoryId = result.ParentCategoryId;

            await RefreshQuantityAsync(result.ParentCategoryId);
        }

        private async Task RefreshQuantityAsync(int parentCategoryId)
        {
            List<ShowcaseCategoryDto> showcaseCategories = await WebClient.ExecuteApiRequestAsync(new QueryShowcaseCategories());

            object factQuantity = await WebClient.ExecuteApiRequestAsync(new QueryShowcaseCategoriesLeftoversQuantity(parentCategoryId, Model.WarehouseId.Value));

            PlanQuantity = showcaseCategories.Where(x => x.WarehouseId == Model.WarehouseId.Value && x.CategoryId == parentCategoryId).Sum(x => x.Quantity);
            FactQuantity = Convert.ToInt32(factQuantity);
        }
    }
}