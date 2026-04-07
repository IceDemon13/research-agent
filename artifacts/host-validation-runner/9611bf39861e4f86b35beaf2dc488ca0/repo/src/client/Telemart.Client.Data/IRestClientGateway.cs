using System.Threading.Tasks;
using Telemart.Client.Data.Requests;

namespace Telemart.Client.Data
{
    public interface IRestClientGateway
    {
        void AddHeader(string key, string value);

        void RemoveHeader(string key);

        Task<byte[]> ExecuteAsBytesAsync(IRestClientGatewayRequest request, Services service);

        Task<T> ExecuteAsync<T>(IRestClientGatewayRequest<T> request, Services service)
            where T : class;

        Task ExecuteAsync(IRestClientGatewayRequest request, Services service);
    }
}