using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ParserSearchTemplate;

namespace Telemart.Client.Data.Requests.Features.ParserSearchTemplate
{
    public class CreateParserSearchTemplateRequest : CreateEntityResultRequestBase<ParserSearchTemplateDto, CreateParserSearchTemplateRequest.CreateParserSearchTemplateDto>
    {
        public CreateParserSearchTemplateRequest(int categoryId, string name)
            : base(new CreateParserSearchTemplateDto(categoryId, name), $"{ApiResources.ParserSearchTemplates}")
        {
        }

        public class CreateParserSearchTemplateDto
        {
            public CreateParserSearchTemplateDto(int categoryId, string name)
            {
                CategoryId = categoryId;
                Name = name;
            }

            [JsonProperty("category_id")]
            public int CategoryId { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }
    }
}