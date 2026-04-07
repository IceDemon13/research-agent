using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.XtraReports.Serialization;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Delivery;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Validation;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Carry;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store
{
    internal sealed class PackagePropertiesViewModel : TelemartDialogViewModelBase, ICarrySupport
    {
        private PackageMaxDimensionsParameter _maxDimensionsParameter;
        private PackagePropertiesParameter _parameter;
        private const decimal teksVolumeWeightOnPlace = 2040;

        public PackagePropertiesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            PackagePlaceItems = new ObservableCollection<PackagePlaceViewItem>();
        }

        public PackagePropertiesViewModel()
        {
        }

        public decimal PackagePlaces
        {
            get { return GetProperty(() => PackagePlaces); }
            set { SetProperty(() => PackagePlaces, value, PackagePlacesChangedCallback); }
        }

        public bool DimensionsSupport
        {
            get { return GetProperty(() => DimensionsSupport); }
            private set { SetProperty(() => DimensionsSupport, value); }
        }

        public bool AllowEditPlacesQuantity
        {
            get { return GetProperty(() => AllowEditPlacesQuantity); }
            private set { SetProperty(() => AllowEditPlacesQuantity, value); }
        }

        public decimal TotalWeight
        {
            get { return GetProperty(() => TotalWeight); }
            set { SetProperty(() => TotalWeight, value, AssignTotalWeight); }
        }

        public int CarryId
        {
            get { return GetProperty(() => CarryId); }
            set { SetProperty(() => CarryId, value, () => RaisePropertiesChanged(nameof(TotalWeight), nameof(VisibleVolumeWeight))); }
        }

        public bool NotAddToNpApplication
        {
            get { return GetProperty(() => NotAddToNpApplication); }
            set { SetProperty(() => NotAddToNpApplication, value); }
        }

        public int? VolumeWeightCoef
        {
            get { return GetProperty(() => VolumeWeightCoef); }
            set { SetProperty(() => VolumeWeightCoef, value); }
        }

        public bool AllowEditInsurance
        {
            get { return GetProperty(() => AllowEditInsurance); }
            private set { SetProperty(() => AllowEditInsurance, value); }
        }

        public decimal Insurance
        {
            get { return GetProperty(() => Insurance); }
            private set { SetProperty(() => Insurance, value, AssignInsurance); }
        }

        public decimal? VolumeWeight
        {
            get { return GetProperty(() => VolumeWeight); }
            set { SetProperty(() => VolumeWeight, value); }
        }

        public ObservableCollection<PackagePlaceViewItem> PackagePlaceItems
        {
            get { return GetProperty(() => PackagePlaceItems); }
            private init { SetProperty(() => PackagePlaceItems, value); }
        }

        #region DialogSettings

        public override int Height => 264;

        public override int MinHeight => 264;

        public override int MinWidth => 360;

        public override int Width => 360;

        public override int MaxHeight => 600;

        public override int MaxWidth => 800;

        public bool VisibleVolumeWeight => CarryId == CarryType.TeksId;

        #endregion

        public static void BuildMetadata(MetadataBuilder<PackagePropertiesViewModel> builder)
        {
            builder.Property(x => x.PackagePlaces).NpPackagePlaces();

            builder.Property(x => x.TotalWeight).PackageWeight();

            builder.Property(x => x.VolumeWeight).MatchesInstanceRule(
                    (x, y) => y.CarryId != CarryType.TeksId || x.HasValue,
                    () => Resources.RequiredErrorMessage)
                .MatchesInstanceRule(
                    (x, y) => y.CarryId != CarryType.TeksId ||
                              (x >= 0.1m && x <= (teksVolumeWeightOnPlace * y.PackagePlaces)),
                    (x, y) => $"Сумарный объемный вес должен быть в пределах 0.1...{teksVolumeWeightOnPlace * y.PackagePlaces}");
        }

        protected override Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this) || PackagePlaceItems.Any(x => IDataErrorInfoHelper.HasErrors(x)))
            {
                MessageFacadeService.ShowNotificationWarning("Данные заполнены некорректно");
            }
            else
            {
                IsOk = true;
                Close();
            }

            return Task.CompletedTask;
        }

        protected override Task HandleLoadedAsync()
        {
            Title = "Параметры посылки";
            return Task.CompletedTask;
        }

        protected override async void OnParameterChanged(object parameter)
        {
            try
            {
                if (IsInDesignMode)
                {
                    return;
                }

                _parameter = (PackagePropertiesParameter)parameter;
                _maxDimensionsParameter = _parameter.MaxDimensionsParameter;

                CarryId = _parameter.CarryId;

                await FetchVolumeWeightCoefAsync();

                if (_parameter.TotalWeight.HasValue)
                {
                    TotalWeight = _parameter.TotalWeight.Value;
                }

                AllowEditPlacesQuantity = true;
                PackagePlaces = Math.Max(_parameter.Places, 1);

                if (_parameter.CarryId is CarryType.UpDeliveryId or CarryType.UpWarehouseId or CarryType.NpPostBoxId)
                {
                    DimensionsSupport = true;
                }

                if (_parameter.CarryId == CarryType.NpPostBoxId)
                {
                    PackagePlaces = NovaposhtaConstants.PostboxPlaceCount;
                    TotalWeight = NovaposhtaConstants.DefaultWeight;
                    AllowEditPlacesQuantity = false;
                }

                AllowEditInsurance = _parameter.AllowEditInsurance;
                Insurance = _parameter.Insurance;

                if (DimensionsSupport && _parameter.Products?.Count == 1)
                {
                    PackagePlaceViewItem firstPlace = PackagePlaceItems?.FirstOrDefault();

                    if (firstPlace != null)
                    {
                        PackagePropertiesProductParameter firstProduct = _parameter.Products.First();

                        firstPlace.Height = ConvertToСentimeters(firstProduct.Heigth);
                        firstPlace.Length = ConvertToСentimeters(firstProduct.Length);
                        firstPlace.Width = ConvertToСentimeters(firstProduct.Width);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to init package properties model");
                MessageFacadeService.ShowNotificationError("Ошибка при загрузке данных");
            }
        }

        private static int? ConvertToСentimeters(double? millimeters)
        {
            if (millimeters is null)
            {
                return null;
            }

            return (int)Math.Round(millimeters.Value / 10);
        }

        private async Task FetchVolumeWeightCoefAsync()
        {
            CarryDto carry = await WebClient.ExecuteApiRequestAsync(new QueryCarry(CarryId));

            VolumeWeightCoef = carry.VolumeWeightCoef;
        }

        private void PackagePlacesChangedCallback()
        {
            while (PackagePlaceItems.Count != PackagePlaces)
            {
                if (PackagePlaceItems.Count < PackagePlaces)
                {
                    if (CarryId == CarryType.NpPostBoxId)
                    {
                        PackagePlaceItems.Add(new PackagePlaceViewItem(PackagePlaceItems.Count + 1, 0, 0, CarryId, NovaposhtaConstants.DefaultLength, NovaposhtaConstants.DefaultWidth, NovaposhtaConstants.DefaultHeight));
                    }
                    else if (CarryId == CarryType.UpDeliveryId || CarryId == CarryType.UpWarehouseId)
                    {

                        PackagePlaceItems.Add(new PackagePlaceViewItem(PackagePlaceItems.Count + 1, 0, 0, CarryId, null, null, null));
                    }
                    else
                    {
                        if (PackagePlaceItems.Count == 1 && CarryId == CarryType.TeksId)
                        {
                            break;
                        }

                        PackagePlaceItems.Add(new PackagePlaceViewItem(PackagePlaceItems.Count + 1, 0, 0, CarryId));
                    }
                }
                else
                {
                    PackagePlaceItems.RemoveAt(PackagePlaceItems.Count - 1);
                }
            }

            if (VolumeWeightCoef > 0 && PackagePlaceItems.All(x => x.Height > 0 && x.Width > 0 && x.Length > 0))
            {
                int totalVolumeWeight = PackagePlaceItems.Sum(x => (x.Height!.Value * x.Length!.Value * x.Width!.Value) / VolumeWeightCoef.Value);

                TotalWeight = Math.Max(TotalWeight, totalVolumeWeight);
            }

            AssignInsurance();
            AssignTotalWeight();

            RaisePropertyChanged(nameof(VolumeWeight));
        }

        private void AssignInsurance()
        {
            if (PackagePlaceItems.Any())
            {
                decimal insurancePart = Math.Floor(Insurance / PackagePlaceItems.Count);

                for (int i = 0; i < PackagePlaceItems.Count - 1; i++)
                {
                    PackagePlaceItems[i].Insurance = insurancePart;
                }

                PackagePlaceItems[PackagePlaceItems.Count - 1].Insurance = Insurance - (insurancePart * (PackagePlaceItems.Count - 1));
            }
        }

        private void AssignTotalWeight()
        {
            if (PackagePlaceItems.Any())
            {
                decimal totalWeightPart = Math.Floor((TotalWeight / PackagePlaceItems.Count) * 10) / 10;

                for (int i = 0; i < PackagePlaceItems.Count - 1; i++)
                {
                    PackagePlaceItems[i].Weight = totalWeightPart;
                }

                PackagePlaceItems[PackagePlaceItems.Count - 1].Weight = Math.Round(TotalWeight - (totalWeightPart * (PackagePlaceItems.Count - 1)), 1);
            }
        }
    }
}