using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Cities
{
    public class CityViewItem : TelemartEditorViewItemBase
    {
        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string NameUkr
        {
            get { return GetProperty(() => NameUkr); }
            set { SetProperty(() => NameUkr, value); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value); }
        }

        public ObservableCollection<CityCarryViewItem> CityCarries
        {
            get { return GetProperty(() => CityCarries); }
            set { SetProperty(() => CityCarries, value); }
        }

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public string NpCityRef
        {
            get { return GetProperty(() => NpCityRef); }
            set { SetProperty(() => NpCityRef, value); }
        }

        public string MeCityRef
        {
            get { return GetProperty(() => MeCityRef); }
            set { SetProperty(() => MeCityRef, value); }
        }

        public int? UpCityId
        {
            get { return GetProperty(() => UpCityId); }
            set { SetProperty(() => UpCityId, value, () => { UpCityIdStr = UpCityId?.ToString(); }); }
        }

        public int? UklonCityId
        {
            get { return GetProperty(() => UklonCityId); }
            set { SetProperty(() => UklonCityId, value, () => { UklonCityIdStr = UklonCityId?.ToString(); }); }
        }

        public string UpCityIdStr
        {
            get { return GetProperty(() => UpCityIdStr); }
            set { SetProperty(() => UpCityIdStr, value, () => { UpCityId = UpCityIdStr is null ? null : int.Parse(UpCityIdStr); }); }
        }

        public string UklonCityIdStr
        {
            get { return GetProperty(() => UklonCityIdStr); }
            set { SetProperty(() => UklonCityIdStr, value, () => { UklonCityId = UklonCityIdStr is null ? null : int.Parse(UklonCityIdStr); }); }
        }

        public int? AreaId
        {
            get { return GetProperty(() => AreaId); }
            set { SetProperty(() => AreaId, value); }
        }

        public int? DistrictId
        {
            get { return GetProperty(() => DistrictId); }
            set { SetProperty(() => DistrictId, value); }
        }

        public static void BuildMetadata(MetadataBuilder<CityViewItem> builder)
        {
            builder.Property(x => x.Name).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameUkr).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameEn).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.AreaId).Required(() => Resources.RequiredErrorMessage);
        }

        public override object Clone()
        {
            CityViewItem item = ReflectionObjectCloner.Clone(this);

            item.CityCarries = CityCarries.Select(x =>
            {
                CityCarryViewItem viewItem = ReflectionObjectCloner.Clone(x);
                return viewItem;
            }).ToObservableCollection();

            return item;
        }
    }
}