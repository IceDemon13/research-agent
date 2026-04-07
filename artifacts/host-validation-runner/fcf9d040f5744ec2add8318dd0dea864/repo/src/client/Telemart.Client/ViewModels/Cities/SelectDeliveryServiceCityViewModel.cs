using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business.Delivery.Data;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.MeestExpress;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.MeestExpress;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Cities
{
    public class SelectDeliveryServiceCityViewModel : TelemartDialogViewModelBase
    {
        public SelectDeliveryServiceCityViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ReadOnlyObservableCollection<DeliveryServiceCity> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public DeliveryServiceCity SelectedCity
        {
            get { return GetProperty(() => SelectedCity); }
            set { SetProperty(() => SelectedCity, value); }
        }

        protected override async Task HandleLoadedAsync()
        {
            CityParameter parameter = (CityParameter)Parameter;

            CarryType carryType = parameter.Carry;

            IReadOnlyCollection<DeliveryServiceCity> cities = await carryType.GetCityProvider().GetCitiesAsync(parameter.DistrictRef, parameter.AreaRef);

            Cities = cities
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();

            Title = "Выберите город";
        }

        protected override bool CanOk() => SelectedCity != null;

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();

            return Task.CompletedTask;
        }
    }
}
