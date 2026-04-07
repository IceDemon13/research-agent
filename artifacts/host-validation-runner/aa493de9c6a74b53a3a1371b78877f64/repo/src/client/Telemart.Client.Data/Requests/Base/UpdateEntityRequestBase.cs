using System.Net.Http;

namespace Telemart.Client.Data.Requests.Base
{
    public abstract class UpdateEntityRequestBase<T, TUpdateDto> : RestClientGatewayRequestBase<T>
       where T : class, new()
    {
        protected UpdateEntityRequestBase(TUpdateDto dto, params object[] pathParameters)
            : base(HttpMethod.Put)
        {
            PathParameters = pathParameters;
            Body = dto;
        }
    }
}