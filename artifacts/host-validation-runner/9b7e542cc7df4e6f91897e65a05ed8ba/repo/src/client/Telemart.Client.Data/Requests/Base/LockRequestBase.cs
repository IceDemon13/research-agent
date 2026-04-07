using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Base
{
    public abstract class LockRequestBase<T> : RestClientGatewayRequestBase<LockResponse<T>>
    {
        protected LockRequestBase(bool allowLockedByMe, bool checkPermissions, params object[] pathParameters)
            : base(HttpMethod.Post)
        {
            Body = new LockRequest
            {
                AllowLockedByMe = allowLockedByMe,
                CheckPermissions = checkPermissions
            };

            PathParameters = pathParameters.Concat(new object[] { "actions/lock" });
        }

        public class LockRequest
        {
            [JsonProperty("allow_locked_by_me")]
            public bool AllowLockedByMe { get; set; }

            [JsonProperty("check_permissions")]
            public bool CheckPermissions { get; set; }
        }
    }
}