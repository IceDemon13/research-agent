using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Validators
{
    public sealed class ParserProductPriceValidator : IParserProductPriceValidator
    {
        public Result<IReadOnlyCollection<ProductSaveDto>> ValidateAsync(IReadOnlyCollection<ProductSaveDto> parserProducts)
        {
            if (parserProducts == null || parserProducts.Count == 0)
            {
                return Result.Error<IReadOnlyCollection<ProductSaveDto>>("Failed to validate parser products", "Не получилось спарсить ни одной строки");
            }

            (IReadOnlyCollection<ProductSaveDto> ValidParserProducts, IReadOnlyCollection<string> Errors) result = GetResultAfterValid(parserProducts);

            return Result.Success(result.ValidParserProducts, result.Errors);
        }

        private (IReadOnlyCollection<ProductSaveDto> ValidParserProducts, IReadOnlyCollection<string> Errors) GetResultAfterValid(IReadOnlyCollection<ProductSaveDto> parserProducts)
        {
            List<string> errors = new List<string>();
            List<ProductSaveDto> validParserProducts = new List<ProductSaveDto>();

            foreach (ProductSaveDto parserProduct in parserProducts)
            {
                string[] productErrors = GetErrors(parserProduct)?.ToArray() ?? Array.Empty<string>();

                if (productErrors?.Any() != true)
                {
                    validParserProducts.Add(parserProduct);
                    continue;
                }

                errors.AddRange(productErrors);
            }

            return (validParserProducts, errors.GroupBy(x => x).Select(x => x.Key + " (к-во строк: " + x.Count() + ")").ToList());
        }

        private IEnumerable<string> GetErrors(ProductSaveDto parserProduct)
        {
            if (parserProduct.Prices?.Any() != true)
            {
                yield return "У товара не заполнена цена.";
            }

            if (parserProduct.Prices?.All(p => p.Currency != default && p.PriceType != default) != true)
            {
                yield return "В настройках не указана валюта и тип цены для парсера даного поставщика.";
            }

            if (parserProduct.SupplierWarehouseAvails != null && parserProduct.SupplierWarehouseAvails.All(a => a.PriceAvail is not null && a.PriceAvail.Length < 100) != true)
            {
                yield return "Не указано наличие.";
            }

            if (parserProduct.SupplierWarehouseAvails != null && parserProduct.SupplierWarehouseAvails.All(a => a.SupplierWarehouseId > 0) != true)
            {
                yield return "В настройках неккоректно указаны склады поставщиков.";
            }

            if (string.IsNullOrEmpty(parserProduct.Name))
            {
                yield return "Не заполнено название товара.";
            }
        }
    }
}