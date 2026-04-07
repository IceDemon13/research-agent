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

namespace Telemart.Client.ViewModels.Common
{
    public sealed class SelectCarryViewModel : TelemartDialogViewModelBase
    {
        public SelectCarryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ReadOnlyObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public int? CarryId
        {
            get { return GetProperty(() => CarryId); }
            set { SetProperty(() => CarryId, value); }
        }

        public static void BuildMetadata(MetadataBuilder<SelectCarryViewModel> builder)
        {
             builder.Property(x => x.CarryId).Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            SelectCarryParameter parameter = (SelectCarryParameter)Parameter;

            Title = "Выберите тип доставки";

            IEnumerable<CarryType> carries = Dictionaries.GetItems<CarryType>().Where(x => x.Active && parameter.IgnoreCarryIds?.Contains(x.Id) == false);

            CarryTypes = carries.ToReadOnlyObservableCollection();

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            if (CarryId.HasValue || MessageFacadeService.Confirm("Вы не выбрали способ доставки", "Продолжить?"))
            {
                CloseOk();
            }

            return Task.CompletedTask;
        }
    }
}