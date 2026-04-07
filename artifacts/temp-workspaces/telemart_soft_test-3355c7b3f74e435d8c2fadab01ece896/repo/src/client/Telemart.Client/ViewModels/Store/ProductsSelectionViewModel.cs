using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store
{
    internal sealed class ProductsSelectionViewModel : TelemartDialogViewModelBase
    {
        public ProductsSelectionViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ObservableCollection<ProductSelectionItem> Items { get; } = new ObservableCollection<ProductSelectionItem>();

        public ObservableCollection<ProductSelectionItem> SelectedItems { get; } = new ObservableCollection<ProductSelectionItem>();

        public bool SeparateWarrantyCards
        {
            get { return GetProperty(() => SeparateWarrantyCards); }
            set { SetProperty(() => SeparateWarrantyCards, value); }
        }

        #region DialogSettings

        public override int Height => 480;

        public override int MinHeight => 300;

        public override int MinWidth => 520;

        public override int Width => 640;

        #endregion

        protected override Task HandleOkAsync()
        {
            if (SelectedItems.Any())
            {
                IsOk = true;
                Close();
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Выберите хотя бы один товар");
            }

            return Task.CompletedTask;
        }

        protected override async Task HandleLoadedAsync()
        {
            ProductsSelectionParameter parameter = (ProductsSelectionParameter)Parameter;

            IReadOnlyCollection<ProductAttributesDto> products = await WebClient.ExecuteApiRequestAsync(new QueryOrderProductAttributes(parameter.OrderId));

            SeparateWarrantyCards = parameter.SeparateWarrantyCards;

            foreach (ProductAttributesDto product in products.OrderBy(x => x.GetLocalName(LocalizableNameType.Ukr)))
            {
                ProductSelectionItem item = new ProductSelectionItem(product.ProductId, product.GetLocalName(LocalizableNameType.Ukr));

                Items.Add(item);

                if (product.PrintWarrantyCard)
                {
                    SelectedItems.Add(item);
                }
            }

            Title = "Выберите товары для печати гарантийного талона";
        }

        public class ProductSelectionItem : BindableBase
        {
            public ProductSelectionItem(int id, string fullName)
            {
                Id = id;
                FullName = fullName;
            }

            public int Id
            {
                get { return GetProperty(() => Id); }
                private set { SetProperty(() => Id, value); }
            }

            public string FullName
            {
                get { return GetProperty(() => FullName); }
                private set { SetProperty(() => FullName, value); }
            }
        }
    }
}