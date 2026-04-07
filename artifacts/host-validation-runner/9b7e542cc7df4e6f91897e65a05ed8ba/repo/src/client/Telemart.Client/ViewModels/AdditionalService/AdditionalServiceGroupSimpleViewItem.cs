using DevExpress.Mvvm.DataAnnotations;
using Newtonsoft.Json;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AdditionalService
{
    public class AdditionalServiceGroupSimpleViewItem : TelemartEditorViewItemBase
    {
        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string NameUa
        {
            get { return GetProperty(() => NameUa); }
            set { SetProperty(() => NameUa, value); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public string DescriptionUa
        {
            get { return GetProperty(() => DescriptionUa); }
            set { SetProperty(() => DescriptionUa, value); }
        }

        public string DescriptionEn
        {
            get { return GetProperty(() => DescriptionEn); }
            set { SetProperty(() => DescriptionEn, value); }
        }

        public bool MultiSelect
        {
            get { return GetProperty(() => MultiSelect); }
            set { SetProperty(() => MultiSelect, value); }
        }

        public string ParentName
        {
            get { return GetProperty(() => ParentName); }
            set { SetProperty(() => ParentName, value); }
        }

        public int? ParentId
        {
            get { return GetProperty(() => ParentId); }
            set { SetProperty(() => ParentId, value); }
        }

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public static void BuildMetadata(MetadataBuilder<AdditionalServiceGroupSimpleViewItem> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameUa)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameEn)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Description)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DescriptionUa)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DescriptionEn)
                .Required(() => Resources.RequiredErrorMessage);
        }
    }
}