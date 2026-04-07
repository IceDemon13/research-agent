using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Content.ProductEquipments
{
    public class ProductEquipmentsExcelImportEngine : ExcelImportEngineBase<ProductEquipmentViewItem, ProductInfoType>
    {
        private const int MinEquipmentLength = 1;

        protected override ProductEquipmentViewItem MapRow(object[] row, ProductInfoType type, int rowNumber)
        {
            ProductEquipmentViewItem mappedRow = null;

            if (!string.IsNullOrWhiteSpace(row[0].ToString()))
            {
                mappedRow = new ProductEquipmentViewItem
                {
                    Type = type,
                    ExcelProductName = row[0].ToString().Trim(),
                    Equipment = row[1].ToString().Trim(),
                    EquipmentUkr = row[2].ToString().Trim(),
                    EquipmentEn = row[3].ToString().Trim()
                };
            }

            return mappedRow;
        }

        protected override int GetHeaderRowCount(ProductInfoType settings)
        {
            return 0;
        }

        protected override IEnumerable<ValidationResultItem> ValidateFile(ExcelReadResult excelData, ProductInfoType type)
        {
            if (excelData.DataRows.All(x => x.Length < 4))
            {
                yield return new ValidationResultItem("Файл должен содержать 4 колонки", true);
            }

            if (excelData.DataRows.All(x => x.Length < 1))
            {
                yield return new ValidationResultItem("В первой колонке листа ожидается название товара", true);
            }

            if (excelData.DataRows.All(x => x.Length < 2))
            {
                yield return new ValidationResultItem($"Во второй колонке листа ожидается {type.Name.ToLower()} на русском", true);
            }

            if (excelData.DataRows.All(x => x.Length < 3))
            {
                yield return new ValidationResultItem($"В третьей колонке листа ожидается {type.Name.ToLower()} на украинском", true);
            }

            if (excelData.DataRows.All(x => x.Length < 4))
            {
                yield return new ValidationResultItem($"В четвертой колонке листа ожидается {type.Name.ToLower()} на английском", true);
            }
        }

        protected override IEnumerable<ValidationResultItem> ValidateRowAfterMap(ProductEquipmentViewItem row, ProductInfoType type, int rowNumber)
        {
            if (row.Equipment.Length <= MinEquipmentLength)
            {
                yield return new ValidationResultItem($"Комплектация товара \"{row.ExcelProductName}\" не должна быть пустой", true);
            }

            if (row.EquipmentUkr.Length <= MinEquipmentLength)
            {
                yield return new ValidationResultItem($"Комплектация товара \"{row.ExcelProductName}\" (укр.) не должна быть пустой", true);
            }

            if (row.EquipmentEn.Length <= MinEquipmentLength)
            {
                yield return new ValidationResultItem($"Комплектация товара \"{row.ExcelProductName}\" (англ.) не должна быть пустой", true);
            }

            if (Rows.Any(x => row.ExcelProductName.Equals(x.ExcelProductName, StringComparison.OrdinalIgnoreCase)))
            {
                yield return new ValidationResultItem($"Товар \"{row.ExcelProductName}\" повторяется более одного раза", true);
            }
        }
    }
}