using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.CheckCompatibility
{
    public class CheckCompatibilityResponse
    {
        [JsonProperty("validated_products")]
        public IReadOnlyCollection<ValidatedProductDto> ValidatedProducts { get; set; }

        [JsonProperty("required_categories")]
        public IReadOnlyCollection<CategoryDto> RequiredCategories { get; set; }

        public IEnumerable<(int NotificationImageId, string Message)> GetValidationResults()
        {
            foreach (ValidatedProductDto validatedProduct in ValidatedProducts)
            {
                foreach (string message in validatedProduct.Messages)
                {
                    yield return (NotificationImage.ErrorId, message);
                }

                foreach (ProductValidationRelationDto validationDataItem in validatedProduct.ValidationData)
                {
                    foreach (string message in validationDataItem.Messages)
                    {
                        yield return (validationDataItem.NotificationImageId, message);
                    }
                }
            }

            if (RequiredCategories.Any())
            {
                string message = RequiredCategories.Count == 1
                    ? $"Категория \"{RequiredCategories.First().Name}\" обязательная для соблюдения комплектности сборки"
                    : $"Категории \"{string.Join(", ", RequiredCategories.Select(x => x.Name))}\" обязательны для соблюдения комплектности сборки";
                yield return (NotificationImage.ErrorId, message);
            }
        }
    }
}