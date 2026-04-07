using System.Net;
using System.Net.Http;

namespace Telemart.Client.Data.Requests.Base
{
    public abstract class CreateEntityRequestBase<T, TCreateDto> : RestClientGatewayRequestBase<T>
        where T : class, new()
    {
        protected CreateEntityRequestBase(TCreateDto dto, params object[] pathParameters)
            : base(HttpMethod.Post)
        {
            Body = dto;
            PathParameters = pathParameters;

            SuccessStatusCode = HttpStatusCode.Created;
        }
    }
}