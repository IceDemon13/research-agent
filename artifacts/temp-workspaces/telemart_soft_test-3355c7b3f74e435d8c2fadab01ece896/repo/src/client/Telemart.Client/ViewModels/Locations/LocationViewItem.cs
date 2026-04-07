 using DevExpress.Mvvm.DataAnnotations;
 using Telemart.Client.Dictionaries;
 using Telemart.Client.Properties;
 using Telemart.Client.ViewModels.Base;

 namespace Telemart.Client.ViewModels.Locations
 {
    public sealed class LocationViewItem : TelemartEditorViewItemBase
    {
        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string Address
        {
            get { return GetProperty(() => Address); }
            set { SetProperty(() => Address, value); }
        }

        public int? CityId
        {
            get { return GetProperty(() => CityId); }
            set { SetProperty(() => CityId, value); }
        }

        public int[] WarehouseIds
        {
            get { return GetProperty(() => WarehouseIds); }
            set { SetProperty(() => WarehouseIds, value); }
        }

        public int? WorkScheduleTypeId
        {
            get { return GetProperty(() => WorkScheduleTypeId); }
            set { SetProperty(() => WorkScheduleTypeId, value); }
        }

        public int? AdditionalScheduleTypeId
        {
            get { return GetProperty(() => AdditionalScheduleTypeId); }
            set { SetProperty(() => AdditionalScheduleTypeId, value); }
        }

        public int LocationTypeId
        {
            get { return GetProperty(() => LocationTypeId); }
            set { SetProperty(() => LocationTypeId, value, () => RaisePropertiesChanged(nameof(WorkScheduleTypeId), nameof(ClusterId))); }
        }

        public bool SyncDeliverySchedules
        {
            get { return GetProperty(() => SyncDeliverySchedules); }
            set { SetProperty(() => SyncDeliverySchedules, value); }
        }

        public int? GoogleExternalLocationId
        {
            get { return GetProperty(() => GoogleExternalLocationId); }
            set { SetProperty(() => GoogleExternalLocationId, value); }
        }

        public int? ClusterId
        {
            get { return GetProperty(() => ClusterId); }
            set { SetProperty(() => ClusterId, value); }
        }

        public static void BuildMetadata(MetadataBuilder<LocationViewItem> b)
        {
            b.Property(x => x.Name).MatchesRule(x => !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage)
                .MaxLength(100, () => "Длина поля должна быть не больше чем 100  символов");
            b.Property(x => x.Address).MatchesRule(x => !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage)
                .MaxLength(250, () => "Длина поля должна быть не больше чем 250  символов");
            b.Property(x => x.CityId).MatchesRule(x => x > 0, () => Resources.RequiredErrorMessage);
            b.Property(x => x.WorkScheduleTypeId).MatchesInstanceRule((x, y) => y.LocationTypeId != LocationType.ShopId || x > 0, () => "Выберите основной график");
            b.Property(x => x.LocationTypeId).Required(() => "Выберите тип локации");
            b.Property(x => x.ClusterId).MatchesInstanceRule((x, y) => y.LocationTypeId != LocationType.ShopId || x > 0, () => "Выберите кластер");
        }
    }
}