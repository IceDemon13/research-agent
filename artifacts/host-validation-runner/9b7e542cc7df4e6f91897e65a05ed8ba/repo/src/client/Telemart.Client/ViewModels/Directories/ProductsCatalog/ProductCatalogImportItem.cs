using Telemart.Client.Common;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Directories.ProductsCatalog
{
    public class ProductCatalogImportItem : IProductItem
    {
        public ProductCatalogImportItem(
            int id,
            string yandexId,
            string color,
            string colorPrimary,
            string colorSecondary,
            string keywords,
            string manufactor,
            string model,
            string modelUkr,
            string modelEn,
            string modific,
            string name,
            string nameUkr,
            string nameEn,
            string partNumber,
            string prefixRus,
            string prefixUkr,
            string prefixEn,
            int categoryId,
            int warrantyRetailId,
            int warrantyWholesaleId,
            int warrantyTypeId,
            int typeId,
            string groupName,
            int? groupFeatureId)
        {
            Id = id;
            YandexId = yandexId;
            Color = color;
            ColorPrimary = colorPrimary;
            ColorSecondary = colorSecondary;
            Keywords = keywords;
            Manufactor = manufactor;
            Model = model;
            ModelUkr = modelUkr;
            ModelEn = modelEn;
            Modific = modific;
            Name = name;
            NameUkr = nameUkr;
            NameEn = nameEn;
            PartNumber = partNumber;
            PrefixRus = prefixRus;
            PrefixUkr = prefixUkr;
            PrefixEn = prefixEn;
            CategoryId = categoryId;
            WarrantyRetailId = warrantyRetailId;
            WarrantyWholesaleId = warrantyWholesaleId;
            WarrantyTypeId = warrantyTypeId;
            TypeId = typeId;
            GroupName = groupName;
            GroupFeatureId = groupFeatureId;
        }

        public int Id { get; }

        public string YandexId { get; }

        public string Color { get; }

        public string ColorPrimary { get; }

        public string ColorSecondary { get; }

        public string Keywords { get; }

        public string Manufactor { get; }

        public string Model { get; }

        public string ModelUkr { get; }

        public string ModelEn { get; }

        public string Modific { get; }

        public string Name { get; }

        public string NameUkr { get; }

        public string NameEn { get; }

        public string PartNumber { get; }

        public string PrefixRus { get; }

        public string PrefixUkr { get; }

        public string PrefixEn { get; }

        public int CategoryId { get; }

        public int WarrantyRetailId { get; }

        public int WarrantyWholesaleId { get; }

        public int WarrantyTypeId { get; set; }

        public int TypeId { get; set; }

        public string GroupName { get; }

        public int? GroupFeatureId { get; }

        public ProductCatalogDto Dto { get; set; }
    }
}