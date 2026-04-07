using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class SetOrderProductPriceViewModel : TelemartDialogViewModelBase
    {
        public SetOrderProductPriceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Products = new ObservableCollection<SetOrderProductPriceViewItem>();

            TotalDeltaStr = "Итоговая скидка: 0";
        }

        public SetOrderProductPriceViewModel()
        {
        }

        public ReadOnlyObservableCollection<ProductPriceKind> PriceKinds
        {
            get { return GetProperty(() => PriceKinds); }
            private set { SetProperty(() => PriceKinds, value); }
        }

        public string TotalDeltaStr
        {
            get { return GetProperty(() => TotalDeltaStr); }
            private set { SetProperty(() => TotalDeltaStr, value); }
        }

        public ProductPriceKind SelectedPriceKind
        {
            get { return GetProperty(() => SelectedPriceKind); }
            set { SetProperty(() => SelectedPriceKind, value, SelectedPriceKindChanged); }
        }

        public ObservableCollection<SetOrderProductPriceViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public static void BuildMetadata(MetadataBuilder<SetOrderProductPriceViewModel> builder)
        {
            builder.Property(x => x.SelectedPriceKind)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            SetOrderProductPriceParameter parameter = (SetOrderProductPriceParameter)Parameter;

            PriceKinds = Dictionaries
                .GetItems<ProductPriceKind>()
                .Where(x => x.CanSwitchInOrders)
                .ToReadOnlyObservableCollection();

            Products = parameter.OrderProducts
                .Where(x => !x.IsGift
                            && !x.IsVirtualProduct
                            && !x.IsAdditionalService
                            && (x.OrderFolder is null ||
                                x.OrderFolder.TypeId != OrderFolderType.AssembledComputerRuleId))
                .Select(x => new SetOrderProductPriceViewItem(
                    x.Id,
                    x.Product.Name,
                    x.PriceOut,
                    x.PriceId,
                    x.Product.Prices,
                    x.CurrencyOutId,
                    x.Quantity))
                .ToObservableCollection();

            Title = "Изменение типа цен по всем товарам";

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            if (Products.All(x => x.PriceNew == x.PriceOld))
            {
                MessageFacadeService.ShowNotificationWarning("Ничего не поменялось");
                return Task.CompletedTask;
            }

            CloseOk();

            return Task.CompletedTask;
        }

        private void SelectedPriceKindChanged()
        {
            if (SelectedPriceKind != null)
            {
                List<string> errors = new();

                foreach (SetOrderProductPriceViewItem product in Products)
                {
                    string errorText = product.RecalculatePriceNew(SelectedPriceKind.Id);

                    if (errorText != null)
                    {
                        errors.Add(errorText);
                    }
                }

                if (errors.Any())
                {
                    MessageFacadeService.ShowValidationResultView("Ошибки при изменении типа цены", errors.Select(x => new ValidationResultItem(x, false)).ToArray(), this);
                }

                TotalDeltaStr = $"Итоговая скидка: {Products.Sum(x => x.Delta)}";
            }
        }
    }
}