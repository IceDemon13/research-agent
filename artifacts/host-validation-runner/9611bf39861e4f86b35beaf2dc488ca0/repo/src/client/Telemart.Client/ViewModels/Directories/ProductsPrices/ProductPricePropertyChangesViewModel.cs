using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;
using Telemart.PriceCalculation.Context;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public sealed class ProductPricePropertyChangesViewModel : TelemartDialogViewModelBase
    {
        public ProductPricePropertyChangesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ReadOnlyObservableCollection<PropertyChangeDto> PropertyChanges
        {
            get { return GetProperty(() => PropertyChanges); }
            set { SetProperty(() => PropertyChanges, value); }
        }

        protected override Task HandleLoadedAsync()
        {
            ProductPricePropertyChangesParameter parameter = (ProductPricePropertyChangesParameter)Parameter;

            PropertyChanges = parameter.PropertyChanges.OrderBy(x => x.Order).ToReadOnlyObservableCollection();

            Title = $"Анализ рассчета робота по товару '{parameter.ProductName}'";

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();
            return Task.CompletedTask;
        }
    }
}