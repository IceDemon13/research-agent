using System;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Base
{
    public abstract class UnlockRequestBase<T> : RestClientGatewayRequestBase<LockResponse<T>>
    {
        protected UnlockRequestBase(bool force, params object[] pathParameters)
            : base(HttpMethod.Post)
        {
            PathParameters = pathParameters.Concat(new object[] { "actions/unlock" });

            Body = new UnLockRequest { Force = force };
        }

        public class UnLockRequest
        {
            [JsonProperty("force")]
            public bool Force { get; set; }
        }
    }
}