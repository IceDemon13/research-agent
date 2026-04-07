using Telemart.Client.Common.Services;
using Telemart.Client.Data.HubClient.Base;

namespace Telemart.Client.Common.HubFactory
{
    public interface IHubClientFactory
    {
        IHubClientBase<T> Create<T>(IMessageFacadeService messageFacadeService, bool exceptionHandling = true, bool stateHandling = false)
            where T : HubClientBase<T>;
    }
}