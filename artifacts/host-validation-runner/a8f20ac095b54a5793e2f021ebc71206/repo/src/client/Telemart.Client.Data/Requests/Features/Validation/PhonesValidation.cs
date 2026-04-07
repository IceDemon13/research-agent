using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Validation
{
    public sealed class PhonesValidation : QueryRequestBase<Result>
    {
        public PhonesValidation(IReadOnlyCollection<string> phones)
        : base(ApiResources.Validation, "phone")
        {
            UrlParameters = new (string Name, object Value)[] { ("phones", phones) };
        }
    }
}