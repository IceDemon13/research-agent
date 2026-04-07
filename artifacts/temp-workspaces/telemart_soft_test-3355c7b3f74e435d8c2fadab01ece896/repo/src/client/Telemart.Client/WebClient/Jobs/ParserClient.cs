using System;
using System.Threading.Tasks;
using AutoMapper;
using Grpc.Core;
using Grpc.Net.Client;
using Telemart.Client.Data.Authentication;
using Telemart.Client.Data.Options;
using Telemart.Client.TransferObjects.ParserRequests;

namespace Telemart.Client.WebClient.Jobs
{
    public sealed class ParserClient : IParserClient
    {
        private readonly IAuthenticationManager _authenticationManager;
        private readonly ParserServiceOptions _options;
        private readonly IMapper _mapper;

        public ParserClient(
            IAuthenticationManager authenticationManager,
            IMapper mapper,
            ParserServiceOptions options)
        {
            _authenticationManager = authenticationManager;
            _mapper = mapper;
            _options = options;
        }

        public async Task<ResultMessage> SaveProductsAsync(SaveProductsRequest request)
        {
            SaveProductsMessage message = _mapper.Map<SaveProductsMessage>(request);

            return await ExecuteAsync(service => service.SaveAsync(message));
        }

        public async Task<ResultMessage> ClearAvailableBySupplierWarehouseIdAsync(ClearAvailableBySupplierWarehouseIdRequest request)
        {
            ClearAvailableMessage message = _mapper.Map<ClearAvailableMessage>(request);

            return await ExecuteAsync(service => service.ClearAvailableBySupplierWarehouseIdAsync(message));
        }

        private async Task<TResponse> ExecuteAsync<TResponse>(Func<ProductService.ProductServiceClient, AsyncUnaryCall<TResponse>> func)
            where TResponse : class
        {
            CallCredentials credentials = BuildCredentials();

            using GrpcChannel channel = GrpcChannel.ForAddress(_options.BaseAddress, new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Create(ChannelCredentials.SecureSsl, credentials)
            });

            ProductService.ProductServiceClient client = new ProductService.ProductServiceClient(channel);

            return await func.Invoke(client);
        }

        private CallCredentials BuildCredentials()
        {
            return CallCredentials.FromInterceptor(async (_, metadata) =>
            {
                _authenticationManager.ThrowIfNotAuthenticated();

                await _authenticationManager.RefreshTokensAsync();

                metadata.Add("Authorization", $"Bearer {_authenticationManager.AccessToken}");
            });
        }
    }
}