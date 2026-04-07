using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ParserFeature
{
    public class UpdateParserFeatures : UpdateEntityResultRequestBase<List<ParserFeatureDto>, IReadOnlyCollection<ParserFeatureSaveDto>>
    {
        public UpdateParserFeatures(IReadOnlyCollection<ParserFeatureSaveDto> saveDtos)
            : base(saveDtos, "parser", "features")
        {
        }
    }
}