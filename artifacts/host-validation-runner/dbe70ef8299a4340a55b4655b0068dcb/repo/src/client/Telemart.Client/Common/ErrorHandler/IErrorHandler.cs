using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm;

namespace Telemart.Client.Common.ErrorHandler
{
    public interface IErrorHandler
    {
        Task<TResult> HandleErrorsAsync<TResult>(
            Func<CancellationToken, Task<TResult>> sourceFunc,
            string actionInProgressName,
            string actionCompletedName,
            ISupportServices parent,
            bool showWarnings,
            bool showDialog = true,
            bool showNotification = true,
            string confirmText = null,
            CancellationToken cancellationToken = default,
            Func<TResult, CancellationToken, Task> onSuccess = null,
            Func<Exception, CancellationToken, Task> onError = null,
            bool showError = true);

        bool ShowWarnings(IReadOnlyCollection<string> warnings, string actionInProgressName, string actionCompletedName, ISupportServices parent, bool showDialog = true, bool showNotification = true);
    }
}