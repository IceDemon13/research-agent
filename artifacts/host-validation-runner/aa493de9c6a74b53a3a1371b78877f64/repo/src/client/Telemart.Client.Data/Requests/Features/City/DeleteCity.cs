using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.City
{
    public sealed class DeleteCity : DeleteEntityResultRequestBase<Result>
    {
        public DeleteCity(int cityId)
            : base(ApiResources.Cities, cityId)
        {
        }
    }
}