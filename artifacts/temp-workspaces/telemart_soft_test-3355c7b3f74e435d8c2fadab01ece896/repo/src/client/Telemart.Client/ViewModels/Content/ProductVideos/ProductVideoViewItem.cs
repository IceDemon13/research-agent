using System.Collections.Generic;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;

namespace Telemart.Client.ViewModels.Content.ProductVideos
{
    public class ProductVideoViewItem : BindableBase
    {
        public ProductVideoViewItem()
        {
            Hashes = new HashSet<string>();
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public string ExcelProductName
        {
            get { return GetProperty(() => ExcelProductName); }
            set { SetProperty(() => ExcelProductName, value); }
        }

        public HashSet<string> Hashes
        {
            get { return GetProperty(() => Hashes); }
            set { SetProperty(() => Hashes, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ProductVideoViewItem> builder)
        {
            builder.Property(x => x.ProductId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ProductName).Required(() => Resources.RequiredErrorMessage);
        }
    }
}
