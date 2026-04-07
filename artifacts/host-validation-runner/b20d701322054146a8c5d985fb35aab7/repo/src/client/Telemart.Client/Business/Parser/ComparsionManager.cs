using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Telemart.Client.Core.Extensions;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Business.Parser
{
    //// TODO: Cover comparsion logic by unit tests
    public sealed class ComparsionManager : IComparsionManager
    {
        private readonly string[] nameSeparators = { " ", "(", ")" };

        public ComparsionResult Compare(ParserAliasDto parserAlias, IReadOnlyCollection<ProductComparsionDto> products)
        {
            List<ProductComparsionDto> productsByAliasCategories = products
                .Where(x => parserAlias.CategoryIds.Contains(x.ParentCategoryId))
                .ToList();

            ComparsionResult comparsionResult = CompareByPartNumber(parserAlias.PartNumber, productsByAliasCategories);

            if (comparsionResult == null)
            {
                string[] nameParts = parserAlias.Name.Split(nameSeparators, StringSplitOptions.RemoveEmptyEntries);
                List<ProductComparsionDto> mathedByName = productsByAliasCategories
                    .Where(x => nameParts.Any(y => y.Equals(x.PartNumber, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (mathedByName.Any())
                {
                    ProductComparsionDto guaranteedValue = mathedByName.Count == 1
                        ? mathedByName.First()
                        : null;

                    comparsionResult = new ComparsionResult(mathedByName, guaranteedValue);
                }
                else
                {
                    List<ProductComparsionDto> comparedByKey = productsByAliasCategories
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x.Key) &&
                            IsKeyCompared(x.Key, parserAlias.Name) &&
                            (string.IsNullOrEmpty(x.Manufactor) || Regex.IsMatch(parserAlias.Name, $@"\b{x.Manufactor}\b", RegexOptions.IgnoreCase)))
                        .ToList();

                    comparsionResult = new ComparsionResult(comparedByKey);
                }
            }

            return comparsionResult;
        }

        private static ComparsionResult CompareByPartNumber(string partNumber, List<ProductComparsionDto> products)
        {
            if (string.IsNullOrWhiteSpace(partNumber))
            {
                return null;
            }

            ComparsionResult comparsionResult = null;

            ProductComparsionDto guaranteedValue = products
                .Where(x => !string.IsNullOrWhiteSpace(x.PartNumber))
                .SingleOrDefault(x => x.PartNumber.Equals(partNumber, StringComparison.OrdinalIgnoreCase));

            if (guaranteedValue != null)
            {
                comparsionResult = new ComparsionResult(new List<ProductComparsionDto> { guaranteedValue }, guaranteedValue);
            }

            return comparsionResult;
        }

        private static bool IsKeyCompared(string key, string name)
        {
            string[] keyParts = key.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);

            return keyParts.All(x => name.Contains(x, StringComparison.OrdinalIgnoreCase));
        }
    }
}