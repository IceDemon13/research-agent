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
    public sealed class ChangeCarryViewModel : TelemartDialogViewModelBase
    {
        public ChangeCarryViewModel(
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

        public int? OldCarryId
        {
            get { return GetProperty(() => OldCarryId); }
            set { SetProperty(() => OldCarryId, value); }
        }

        public int? NewCarryId
        {
            get { return GetProperty(() => NewCarryId); }
            set { SetProperty(() => NewCarryId, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ChangeCarryViewModel> builder)
        {
            builder.Property(x => x.NewCarryId).Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            ChangeCarryParameter parameter = (ChangeCarryParameter)Parameter;

            Title = "Выберите тип доставки";

            IEnumerable<CarryType> carries = Dictionaries.GetItems<CarryType>().Where(x => x.Active && parameter.IgnoreCarryIds?.Contains(x.Id) == false);

            CarryTypes = carries.ToReadOnlyObservableCollection();

            OldCarryId = parameter.CarryId;

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            if (NewCarryId.HasValue || MessageFacadeService.Confirm("Вы не выбрали способ доставки", "Продолжить?"))
            {
                CloseOk();
            }

            return Task.CompletedTask;
        }
    }
}