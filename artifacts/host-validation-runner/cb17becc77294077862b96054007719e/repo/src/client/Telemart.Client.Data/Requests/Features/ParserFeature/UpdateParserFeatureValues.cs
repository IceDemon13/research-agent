using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ParserFeature
{
    public class UpdateParserFeatureValues : UpdateEntityResultRequestBase<List<ParserFeatureValueDto>, IReadOnlyCollection<ParserFeatureValueSaveDto>>
    {
        public UpdateParserFeatureValues(IReadOnlyCollection<ParserFeatureValueSaveDto> saveDtos)
            : base(saveDtos, "parser", "features", "values")
        {
        }
    }
}