using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Telemart.Client.Cache;
using Telemart.Client.Common.Services;
using Telemart.Client.Core;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Core.IO;
using Telemart.Client.Core.Update;
using Telemart.Client.Data;
using Telemart.Client.Data.Authentication;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Common.ErrorHandling;
using Telemart.Common.Extensions;

namespace Telemart.Client.WebClient
{
    public sealed class TelemartWebClient : IWebClient
    {
        private readonly ICache _cache;
        private readonly IAuthenticationManager _authenticationManager;
        private readonly IRestClientGateway _restClientGateway;
        private readonly IMessageFacadeService _messageFacadeService;
        private readonly IFileDownloader _fileDownloader;
        private readonly IOptionsMonitor<UpdateManagerOptions> _updateManagerOptions;
        private readonly IOptionsMonitor<ChunkOptions> _chunkOptions;
        private readonly ILogger<TelemartWebClient> _logger;
        private int? _workPlaceId;

        public TelemartWebClient(
            IAuthenticationManager authenticationManager,
            IRestClientGateway restClientGateway,
            IMessageFacadeService messageFacadeService,
            IFileDownloader fileDownloader,
            IOptionsMonitor<UpdateManagerOptions> updateManagerOptions,
            IOptionsMonitor<ChunkOptions> chunkOptions,
            ILogger<TelemartWebClient> logger,
            ICache cache)
        {
            _authenticationManager = authenticationManager;
            _restClientGateway = restClientGateway;
            _messageFacadeService = messageFacadeService;
            _fileDownloader = fileDownloader;
            _updateManagerOptions = updateManagerOptions;
            _chunkOptions = chunkOptions;
            _logger = logger;
            _cache = cache;
        }

        public int? WorkPlaceId
        {
            get => _workPlaceId;
            private set
            {
                _workPlaceId = value;

                if (_workPlaceId.HasValue)
                {
                    AddHeader(nameof(WorkPlaceId), _workPlaceId!.Value.ToString());
                }
                else
                {
                    RemoveHeader(nameof(WorkPlaceId));
                }
            }
        }

        // TODO: migrate AuthenticatedEmployee to AuthenticatedEmployeeFullData
        public EmployeeContextDto AuthenticatedEmployee { get; private set; }

        public EmployeeRichDto AuthenticatedEmployeeFullData { get; private set; }

        public async Task<AuthResponse> AuthenticateAsync(AuthRequest request)
        {
            AuthResponse response = await _authenticationManager.AuthenticateAsync(request).ConfigureAwait(false);

            if (response.Success && AuthenticatedEmployee?.Id != response.UserId)
            {
                Task<EmployeeContextDto> contextDtoTask = HandleErrorsAsync(() => ExecuteAsync(new QueryCurrentEmployee(), Services.Main));
                Task<EmployeeRichDto> richDtoTask = HandleErrorsAsync(() => ExecuteAsync(new QueryEmployee(response.UserId), Services.Main));

                (EmployeeContextDto ContextDto, EmployeeRichDto RichDto) result = await Helpers.TaskExt.WhenAll(
                    contextDtoTask,
                    richDtoTask);

                AuthenticatedEmployeeFullData = result.RichDto;
                SetAuthenticatedEmployee(result.ContextDto);
            }

            return response;
        }

        public async Task QueryAndSetAuthenticatedEmployeeAsync()
        {
            EmployeeContextDto employeeContextDto = await HandleErrorsAsync(() => ExecuteAsync(new QueryCurrentEmployee(), Services.Main)).ConfigureAwait(false);

            SetAuthenticatedEmployee(employeeContextDto);
        }

        public void ClearAuthenticationInfo()
        {
            AuthenticatedEmployee = null;
            AuthenticatedEmployeeFullData = null;
            _authenticationManager.ClearTokens();
            WorkPlaceId = null;
        }

        public void SetAuthenticatedEmployee(EmployeeContextDto value)
        {
            value.AllowedOperations = _authenticationManager.ClaimsPrincipal!.GetOperationIds().ToHashSet();
            value.Roles = _authenticationManager.ClaimsPrincipal!.GetRoles().Select(x => x.Name).ToArray();
            AuthenticatedEmployee = value;
        }

        public T ExecuteApiRequest<T>(IRestClientGatewayRequest<T> request)
            where T : class
        {
            return AsyncHelper.RunSync(() => ExecuteApiRequestAsync(request));
        }

        public List<T> ExecuteApiRequest<T>(QueryEntitiesRequestBase<T> request, bool useCache)
            where T : class, new()
        {
            return AsyncHelper.RunSync(() => ExecuteApiRequestAsync(request, useCache));
        }

        public async Task<T> ExecuteApiRequestAsync<T>(IRestClientGatewayRequest<T> request)
            where T : class
        {
            return await HandleErrorsAsync(async () => await ExecuteAsync(request, Services.Main).ConfigureAwait(false));
        }

        public async Task<PagedResult<T>> ExecuteApiRequestAsync<T>(QueryEntitiesPagedRequestBase<T> request, bool useCache)
            where T : class, new()
        {
            if (_chunkOptions.CurrentValue.Enable && request is IChunkSupport)
            {
                return await ExecuteCashedAsync(
                    request,
                    () => HandleErrorsAsync(
                        async () => await ExecuteChunkedAsync(request, Services.Main).ConfigureAwait(false)),
                    useCache);
            }
            else
            {
                return await ExecuteCashedAsync(
                    request,
                    () => ExecuteApiRequestAsync(request),
                    useCache);
            }
        }

        public async Task<List<T>> ExecuteApiRequestAsync<T>(QueryEntitiesRequestBase<T> request, bool useCache)
            where T : class, new()
        {
            return await ExecuteCashedAsync(
                request,
                () => ExecuteApiRequestAsync(request),
                useCache);
        }

        public async Task ExecuteApiRequestAsync(IRestClientGatewayRequest request)
        {
            await HandleErrorsAsync<object>(async () =>
            {
                await ExecuteAsBytesAsync(request, Services.Main).ConfigureAwait(false);

                return null;
            }).ConfigureAwait(false);
        }

        public async Task<byte[]> ExecuteApiRequestAsBytesAsync(IRestClientGatewayRequest request)
        {
            return await HandleErrorsAsync(() => ExecuteAsBytesAsync(request, Services.Main)).ConfigureAwait(false);
        }

        public async Task<T> ExecuteReportApiRequestAsync<T>(IRestClientGatewayRequest<T> request)
            where T : class, new()
        {
            return await ExecuteAsync(request, Services.Report).ConfigureAwait(false);
        }

        public async Task ExecuteReportApiRequestAsync(IRestClientGatewayRequest request)
        {
            await ExecuteAsBytesAsync(request, Services.Report).ConfigureAwait(false);
        }

        public async Task<T> ExecuteCatalogApiRequestAsync<T>(IRestClientGatewayRequest<T> request)
            where T : class, new()
        {
            return await ExecuteAsync(request, Services.Catalog).ConfigureAwait(false);
        }

        public async Task ExecuteCatalogApiRequestAsync(IRestClientGatewayRequest request)
        {
            await ExecuteAsBytesAsync(request, Services.Catalog).ConfigureAwait(false);
        }

        public async Task<T> ExecuteTelegramApiRequestAsync<T>(IRestClientGatewayRequest<T> request)
            where T : class, new()
        {
            return await ExecuteAsync(request, Services.Telegram).ConfigureAwait(false);
        }

        public async Task ExecuteTelegramApiRequestAsync(IRestClientGatewayRequest request)
        {
            await ExecuteAsBytesAsync(request, Services.Telegram).ConfigureAwait(false);
        }

        public async Task<byte[]> ExecuteCatalogApiRequestAsBytesAsync(IRestClientGatewayRequest request)
        {
            return await ExecuteAsBytesAsync(request, Services.Catalog).ConfigureAwait(false);
        }

        public async Task<T> ExecuteCallApiRequestAsync<T>(IRestClientGatewayRequest<T> request)
            where T : class, new()
        {
            return await ExecuteAsync(request, Services.Call).ConfigureAwait(false);
        }

        public bool IsOperationAllowed(BusinessOperation operation)
        {
            bool allowed = false;

            if (AuthenticatedEmployee != null)
            {
                allowed = AuthenticatedEmployee.AllowedOperations.Contains((int)operation);
            }

            return allowed;
        }

        public void SetWorkPlaceId(int? value)
        {
            WorkPlaceId = value;
        }

        public void AddHeader(string key, string value)
        {
            _restClientGateway.AddHeader(key, value);
        }

        public void RemoveHeader(string key)
        {
            _restClientGateway.RemoveHeader(key);
        }

        public async Task InstallationPosComObjectAsync()
        {
            try
            {
                string downloadUrl = $"{_updateManagerOptions.CurrentValue.BaseAddress}/lib/{Common.Constants.IngenicoComObjectName}";

                string filePathSave = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Common.Constants.IngenicoComObjectName);

                if (await _fileDownloader.DownloadExeFileAsync(downloadUrl, filePathSave).ConfigureAwait(false))
                {
                    ProcessHelper.Start(filePathSave);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private async Task<T> HandleErrorsAsync<T>(Func<Task<T>> func)
        {
            try
            {
                return await func().ConfigureAwait(false);
            }
            catch (UnexpectedSatusException e) when (e.Args?.Error?.ErrorCode == ErrorCode.OldClient)
            {
                _messageFacadeService.ShowMessageBoxError(e.Args.Error.ErrorMessage, "Ошибка");
                throw;
            }
        }

        private async Task<TResponse> ExecuteAsync<TResponse>(IRestClientGatewayRequest<TResponse> request, Services service)
            where TResponse : class
        {
            return await _restClientGateway.ExecuteAsync(request, service).ConfigureAwait(false);
        }

        private async Task<byte[]> ExecuteAsBytesAsync(IRestClientGatewayRequest request, Services service)
        {
            return await _restClientGateway.ExecuteAsBytesAsync(request, service).ConfigureAwait(false);
        }

        private async Task<List<T>> ExecuteCashedAsync<T>(QueryEntitiesRequestBase<T> request, Func<Task<List<T>>> requestFunc, bool useCache)
            where T : class, new()
        {
            List<T> result;

            if (useCache)
            {
                var cachedItems = await _cache.GetAllAsync<T>();

                if (cachedItems == null)
                {
                    _logger.LogWarning("Cache does not exists. Url: {Url}", request.BuildRequest().RequestUri);

                    result = await requestFunc();
                }
                else
                {
                    result = cachedItems.ToList();
                }
            }
            else
            {
                result = await requestFunc();
            }

            return result;
        }

        private async Task<PagedResult<T>> ExecuteCashedAsync<T>(QueryEntitiesPagedRequestBase<T> request, Func<Task<PagedResult<T>>> requestFunc, bool useCache)
            where T : class, new()
        {
            PagedResult<T> result;

            if (useCache)
            {
                List<T> items = (await _cache.GetAllAsync<T>())?.ToList();

                if (items == null)
                {
                    _logger.LogWarning("Cache does not exists. Url: {Url}", request.BuildRequest().RequestUri);

                    result = await requestFunc();
                }
                else
                {
                    result = new PagedResult<T>
                    {
                        Data = items,
                        Pagination = new PagingInfo()
                        {
                            Skip = request.Skip ?? 0,
                            Take = request.Take ?? int.MaxValue,
                            Returned = items.Count,
                            TotalCount = items.Count
                        }
                    };
                }
            }
            else
            {
                result = await requestFunc();
            }

            return result;
        }

        private async Task<PagedResult<T>> ExecuteChunkedAsync<T>(QueryEntitiesPagedRequestBase<T> request, Services services)
            where T : class, new()
        {
            _logger.LogInformation("Start chunk query");

            AsyncRetryPolicy retryPolicy = Policy
                .Handle<Exception>()
                .RetryAsync(_chunkOptions.CurrentValue.Retries);

            int pageSize = _chunkOptions.CurrentValue.Size;

            int skip = request.Skip ?? 0;
            int take;
            int totalCount;

            PagedResult<T> firstPage = await FetchPageAsync(skip);

            take = request.Take ?? firstPage.Pagination.TotalCount;
            totalCount = firstPage.Pagination.TotalCount;
            skip += firstPage.Pagination.Returned;

            int pages = (int)Math.Ceiling((double)(take - skip) / pageSize);

            IEnumerable<Task<PagedResult<T>>> fetchPageTasks = Enumerable.Range(0, pages).Select(p => skip + (p * pageSize))
                .Select(x => FetchPageAsync(x));

            var result = (await Task.WhenAll(fetchPageTasks)).SelectMany(x => x.Data).ToList();

            _logger.LogInformation("End chunk query");

            return new PagedResult<T>
            {
                Data = result,
                Pagination = new PagingInfo
                {
                    Returned = result.Count,
                    Skip = request.Skip ?? 0,
                    Take = request.Take ?? totalCount,
                    TotalCount = totalCount
                }
            };

            async Task<PagedResult<T>> FetchPageAsync(int pageSkip)
            {
                ChunkedRequest<T> chunkedRequest = new ChunkedRequest<T>(
                    request,
                    pageSkip,
                    _chunkOptions.CurrentValue.Size);

                PagedResult<T> pageResult = await retryPolicy.ExecuteAsync(() => ExecuteAsync(chunkedRequest, services)).ConfigureAwait(false);

                return pageResult;
            }
        }
    }
}