using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common.Product
{
    public class ProductDimensionsViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int? Width
        {
            get { return GetProperty(() => Width); }
            set { SetProperty(() => Width, value); }
        }

        public int? Height
        {
            get { return GetProperty(() => Height); }
            set { SetProperty(() => Height, value); }
        }

        public int? Depth
        {
            get { return GetProperty(() => Depth); }
            set { SetProperty(() => Depth, value); }
        }

        public double? Weight
        {
            get { return GetProperty(() => Weight); }
            set { SetProperty(() => Weight, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ProductDimensionsViewItem> builder)
        {
            builder.Property(x => x.Width).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Height).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Depth).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Weight).Required(() => Resources.RequiredErrorMessage);
        }
    }
}
