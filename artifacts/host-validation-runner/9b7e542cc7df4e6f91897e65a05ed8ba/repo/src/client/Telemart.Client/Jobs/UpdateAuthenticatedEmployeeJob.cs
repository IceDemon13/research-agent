using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using Telemart.Client.Cache;
using Telemart.Client.Data.Authentication;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class UpdateAuthenticatedEmployeeJob : IJob
    {
        private readonly IWebClient _webClient;
        private readonly EmployeeOptions _options;
        private readonly ILogger<UpdateAuthenticatedEmployeeJob> _logger;
        private readonly ICache _cache;
        private readonly IAuthenticationManager _authenticationManager;

        public UpdateAuthenticatedEmployeeJob(
            IWebClient webClient,
            EmployeeOptions options,
            ILogger<UpdateAuthenticatedEmployeeJob> logger,
            ICache cache,
            IAuthenticationManager authenticationManager)
        {
            _webClient = webClient;
            _options = options;
            _logger = logger;
            _cache = cache;
            _authenticationManager = authenticationManager;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                EmployeeContextDto employee = await _webClient.ExecuteApiRequestAsync(new QueryCurrentEmployee());

                if (employee.ClientAccessDenied && _options.Update)
                {
                    await _cache.DeleteAllAsync();

                    _logger.LogWarning("Непредвиденная ошибка. Обратитесь к своему системному администратору");

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.Application.Current.Shutdown();
                        Environment.Exit(0);
                    });
                }

                if (employee.ModifiedOn > _authenticationManager.LastTimeTokenRefreshed)
                {
                    await _authenticationManager.RefreshTokensAsync(true);
                    _webClient.SetAuthenticatedEmployee(employee);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to update authenticated employee");
            }
        }
    }
}