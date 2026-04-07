using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Common.Extensions;

namespace Telemart.Client.Common.ErrorHandler
{
    public class ResultErrorHandler : IErrorHandler
    {
        private readonly IMessageFacadeService _messageFacadeService;
        private readonly ILogger<ResultErrorHandler> _logger;

        public ResultErrorHandler(IMessageFacadeService messageFacadeService, ILogger<ResultErrorHandler> logger)
        {
            _messageFacadeService = messageFacadeService;
            _logger = logger;
        }

        public async Task<TResult> HandleErrorsAsync<TResult>(
            Func<CancellationToken, Task<TResult>> sourceFunc,
            string actionInProgressName,
            string actionCompletedName,
            ISupportServices parent,
            bool showWarnings,
            bool showDialog,
            bool showNotification,
            string confirmText,
            CancellationToken cancellationToken,
            Func<TResult, CancellationToken, Task> onSuccess,
            Func<Exception, CancellationToken, Task> onError,
            bool showError)
        {
            if (confirmText is null || _messageFacadeService.Confirm("Вы уверены?", confirmText))
            {
                Exception exception = null;

                try
                {
                    TResult actionResult = await sourceFunc(cancellationToken);

                    bool anyWarnings = false;
                    bool isSuccess = true;

                    if (actionResult is Result result)
                    {
                        isSuccess = result.IsSuccess;
                        if (showWarnings && result.IsSuccess && result.Warnings?.Any() == true)
                        {
                            anyWarnings = ShowWarnings(result.Warnings, actionInProgressName, actionCompletedName, parent, showDialog, showNotification);
                        }

                        if (!result.IsSuccess)
                        {
                            exception = new UnexpectedSatusException(null, result.ErrorObj);

                            if (showError)
                            {
                                _messageFacadeService.ShowNotificationError($"Ошибка при {actionInProgressName}");
                            }

                            if (showDialog)
                            {
                                _messageFacadeService.ShowValidationResultView($"Ошибки при {actionInProgressName}", result.ErrorObj.GetMessages().Select(x => new ValidationResultItem(x, true)), parent);
                            }
                        }
                    }

                    if (!anyWarnings && showNotification && !string.IsNullOrEmpty(actionCompletedName))
                    {
                        _messageFacadeService.ShowNotificationInfo($"{actionCompletedName} успешно");
                    }

                    if (isSuccess)
                    {
                        if (onSuccess != null)
                        {
                            await onSuccess(actionResult, cancellationToken);
                        }
                    }

                    return actionResult;
                }
                catch (UnexpectedSatusException ex)
                {
                    if (showError)
                    {
                        _messageFacadeService.ShowNotificationError($"Ошибка при {actionInProgressName}");
                    }

                    if (showDialog)
                    {
                        _messageFacadeService.ShowValidationResultView($"Ошибки при {actionInProgressName}", ex.GetErrorItems(), parent);
                    }

                    exception = ex;
                }
                catch (UnexpectedErrorException ex)
                {
                    _logger.LogError(ex, "Failed to execute action \"{ActionName}\"", actionInProgressName);

                    if (showError)
                    {
                        _messageFacadeService.ShowNotificationError(Resources.ServerUnavailable);
                    }

                    if (showDialog)
                    {
                        _messageFacadeService.ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }, parent);
                    }

                    exception = ex;
                }
                catch (Exception ex)
                {
                    if (showError)
                    {
                        _messageFacadeService.ShowNotificationError($"Ошибка при {actionInProgressName}");
                    }

                    _logger.LogError(ex, "Error while executing action \"{ActionName}\"", actionInProgressName);

                    exception = ex;
                }

                if (onError != null)
                {
                    await onError(exception, cancellationToken);
                }
            }

            return default;
        }

        public bool ShowWarnings(IReadOnlyCollection<string> warnings, string actionInProgressName, string actionCompletedName, ISupportServices parent, bool showDialog, bool showNotification)
        {
            bool anyWarnings = warnings?.Count > 0;

            if (anyWarnings)
            {
                IReadOnlyCollection<ValidationResultItem> validationResultItems = warnings
                    .Select(x => new ValidationResultItem(x, false))
                    .ToList();

                if (showDialog)
                {
                    _messageFacadeService.ShowValidationResultView($"Предупреждения при {actionInProgressName}", validationResultItems, parent);
                }

                if (showNotification && !string.IsNullOrEmpty(actionCompletedName))
                {
                    _messageFacadeService.ShowNotificationWarning($"{actionCompletedName} с предупреждениями");
                }
            }

            return anyWarnings;
        }
    }
}