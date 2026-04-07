using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.ParserFeature
{
    public class DeleteParserFeatureValues : DeleteEntityResultRequestBase<object>
    {
        public DeleteParserFeatureValues(IReadOnlyCollection<int> ids)
            : base("parser", "features", "values")
        {
            Body = ids;
        }
    }
}