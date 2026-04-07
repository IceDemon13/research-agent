using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.ParserFeature
{
    public class DeleteParserFeatures : DeleteEntityResultRequestBase<object>
    {
        public DeleteParserFeatures(IReadOnlyCollection<int> ids)
            : base("parser", "features")
        {
            Body = ids;
        }
    }
}