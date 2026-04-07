using System.Collections.Generic;
using System.Threading.Tasks;
using Telemart.Client.Data.Authentication;
using Telemart.Client.Data.Requests;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;

namespace Telemart.Client.Data.WebClient
{
    public interface IWebClient
    {
        int? WorkPlaceId { get; }

        EmployeeContextDto AuthenticatedEmployee { get; }

        EmployeeRichDto AuthenticatedEmployeeFullData { get; }

        Task<AuthResponse> AuthenticateAsync(AuthRequest request);

        void ClearAuthenticationInfo();

        Task QueryAndSetAuthenticatedEmployeeAsync();

        void AddHeader(string key, string value);

        void RemoveHeader(string key);

        void SetAuthenticatedEmployee(EmployeeContextDto value);

        T ExecuteApiRequest<T>(IRestClientGatewayRequest<T> request)
            where T : class;

        List<T> ExecuteApiRequest<T>(QueryEntitiesRequestBase<T> request, bool useCache = false)
            where T : class, new();

        Task<T> ExecuteApiRequestAsync<T>(IRestClientGatewayRequest<T> request)
            where T : class;

        Task<PagedResult<T>> ExecuteApiRequestAsync<T>(QueryEntitiesPagedRequestBase<T> request, bool useCache = false)
            where T : class, new();

        Task<List<T>> ExecuteApiRequestAsync<T>(QueryEntitiesRequestBase<T> request, bool useCache = false)
            where T : class, new();

        Task ExecuteApiRequestAsync(IRestClientGatewayRequest request);

        Task<byte[]> ExecuteApiRequestAsBytesAsync(IRestClientGatewayRequest request);

        Task<byte[]> ExecuteCatalogApiRequestAsBytesAsync(IRestClientGatewayRequest request);

        Task<T> ExecuteCatalogApiRequestAsync<T>(IRestClientGatewayRequest<T> request)
            where T : class, new();

        Task ExecuteCatalogApiRequestAsync(IRestClientGatewayRequest request);

        Task<T> ExecuteTelegramApiRequestAsync<T>(IRestClientGatewayRequest<T> request)
            where T : class, new();

        Task ExecuteTelegramApiRequestAsync(IRestClientGatewayRequest request);

        Task<T> ExecuteReportApiRequestAsync<T>(IRestClientGatewayRequest<T> request)
            where T : class, new();

        Task ExecuteReportApiRequestAsync(IRestClientGatewayRequest request);

        Task<T> ExecuteCallApiRequestAsync<T>(IRestClientGatewayRequest<T> request)
            where T : class, new();

        bool IsOperationAllowed(BusinessOperation operation);

        void SetWorkPlaceId(int? value);

        Task InstallationPosComObjectAsync();
    }
}