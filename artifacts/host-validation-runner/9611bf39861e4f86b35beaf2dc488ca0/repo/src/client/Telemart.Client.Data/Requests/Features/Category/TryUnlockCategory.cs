using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Category
{
    public sealed class TryUnlockCategory : UnlockRequestBase<List<CategoryLockInfo>>
    {
        public TryUnlockCategory(int id, bool force = false)
            : base(force, ApiResources.Categories, id)
        {
        }
    }
}