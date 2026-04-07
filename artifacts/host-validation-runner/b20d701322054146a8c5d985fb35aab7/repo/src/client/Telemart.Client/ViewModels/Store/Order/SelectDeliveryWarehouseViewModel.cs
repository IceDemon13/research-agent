using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Carry;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class SelectDeliveryWarehouseViewModel : TelemartDialogViewModelBase
    {
        public SelectDeliveryWarehouseViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Title = "Выберите отделение";
            RowDoubleClickCommand = new DelegateCommand<RowDoubleClickEventArgs>(RowDoubleClick);
            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);
        }

        public SelectDeliveryWarehouseViewModel()
        {
        }

        #region Commands

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        public IDelegateCommand RowDoubleClickCommand { get; }

        #endregion

        public string OldAddressValue
        {
            get { return GetProperty(() => OldAddressValue); }
            set { SetProperty(() => OldAddressValue, value); }
        }

        public DeliveryServicePlaceDto Place
        {
            get { return GetProperty(() => Place); }
            set { SetProperty(() => Place, value); }
        }

        public ReadOnlyObservableCollection<DeliveryServicePlaceDto> Places
        {
            get { return GetProperty(() => Places); }
            private set { SetProperty(() => Places, value); }
        }

        public bool IsVisibleColumnLimit
        {
            get { return GetProperty(() => IsVisibleColumnLimit); }
            private set { SetProperty(() => IsVisibleColumnLimit, value); }
        }

        #region DialogSettings

        public override int Height => 600;

        public override int MinHeight => 480;

        public override int MinWidth => 640;

        public override int Width => 800;

        #endregion

        public DeliveryDataDto GetDeliveryServiceData()
        {
            return new DeliveryDataDto
            {
                CityId = Place.CityId,
                PlaceId = Place.PlaceId,
                Street = null,
                House = null,
                Flat = null,
                Extra = null,
                MaxAllowedWeight = Place.TotalMaxWeightAllowed,
                Address = Place.PlaceName,
                AddressUkr = Place.PlaceNameUkr,
                AddressEn = Place.PlaceNameEn
            };
        }

        protected override async Task HandleLoadedAsync()
        {
            SelectDeliveryDataParameter parameter = (SelectDeliveryDataParameter)Parameter;

            List<DeliveryServicePlaceDto> places = await WebClient.ExecuteApiRequestAsync(new QueryCarryPlaces(parameter.CarryId, parameter.CityId));

            Places = places.ToReadOnlyObservableCollection();

            OldAddressValue = parameter.AddressOld;

            if (parameter.DeliveryDataOld != null)
            {
                Place = Places.FirstOrDefault(x => string.Equals(x.PlaceId, parameter.DeliveryDataOld.PlaceId, StringComparison.OrdinalIgnoreCase))
                        ?? Places.FirstOrDefault();
            }

            IsVisibleColumnLimit = parameter.CarryId == CarryType.NpWarehouseId || parameter.CarryId == CarryType.NpPostBoxId;
        }

        protected override bool CanOk()
        {
            return Place != null;
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            return Task.CompletedTask;
        }

        private void HandlePreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkCommand.Execute(null);
            }
            else if (e.Key == Key.Escape)
            {
                CancelCommand.Execute(null);
            }
        }

        private void RowDoubleClick(RowDoubleClickEventArgs args)
        {
            if (args.ChangedButton == MouseButton.Left)
            {
                OkCommand.Execute(null);
            }
        }
    }
}