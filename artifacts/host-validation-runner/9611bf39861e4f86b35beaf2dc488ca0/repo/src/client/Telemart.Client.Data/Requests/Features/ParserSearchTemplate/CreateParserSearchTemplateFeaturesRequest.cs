using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ParserSearchTemplate;

namespace Telemart.Client.Data.Requests.Features.ParserSearchTemplate
{
    public class CreateParserSearchTemplateFeaturesRequest : CreateEntityResultRequestBase<ParserSearchTemplateFeatureDto, CreateParserSearchTemplateFeaturesRequest.CreateParserSearchTemplateFeaturesDto>
    {
        public CreateParserSearchTemplateFeaturesRequest(int parserSearchTemplateId, int featureId)
            : base(new CreateParserSearchTemplateFeaturesDto(parserSearchTemplateId, featureId), $"{ApiResources.ParserSearchTemplates}/features")
        {
        }

        public class CreateParserSearchTemplateFeaturesDto
        {
            public CreateParserSearchTemplateFeaturesDto(int temlateId, int featureId)
            {
                ParserSearchTemplateId = temlateId;
                FeatureId = featureId;
            }

            [JsonProperty("parser_search_template_id")]
            public int ParserSearchTemplateId { get; set; }

            [JsonProperty("feature_id")]
            public int FeatureId { get; set; }
        }
    }
}