using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.District
{
    public class DistrictViewItem : TelemartEditorViewItemBase
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

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public string MeDistrictRef
        {
            get { return GetProperty(() => MeDistrictRef); }
            set { SetProperty(() => MeDistrictRef, value); }
        }

        public int? UpDistrictId
        {
            get { return GetProperty(() => UpDistrictId); }
            set { SetProperty(() => UpDistrictId, value); }
        }

        public int? AreaId
        {
            get { return GetProperty(() => AreaId); }
            set { SetProperty(() => AreaId, value); }
        }

        public static void BuildMetadata(MetadataBuilder<DistrictViewItem> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(60, () => "Длина поля должна быть короче 60 символов");
            builder.Property(x => x.NameUkr)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(60, () => "Длина поля должна быть короче 60 символов");
            builder.Property(x => x.NameEn)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(60, () => "Длина поля должна быть короче 60 символов");
            builder.Property(x => x.AreaId)
                .Required(() => Resources.RequiredErrorMessage);
        }
    }
}