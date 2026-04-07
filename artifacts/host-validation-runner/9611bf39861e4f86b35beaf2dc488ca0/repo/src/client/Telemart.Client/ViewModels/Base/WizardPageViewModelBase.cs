using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Base
{
    public abstract class WizardPageViewModelBase<TModel> :
        ViewModelBase,
        ISupportWizardCancelCommand,
        IDataErrorInfo
    {
        protected WizardPageViewModelBase()
        {
        }

        #region INPC

        public bool IsLongOperationInProgress
        {
            get { return GetProperty(() => IsLongOperationInProgress); }
            protected set { SetProperty(() => IsLongOperationInProgress, value); }
        }

        #endregion

        public bool CanCancel => GetCanCancel();

        public abstract string Description { get; }

        public abstract string Header { get; }

        public TModel Model
        {
            get { return GetProperty(() => Model); }
            protected set { SetProperty(() => Model, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public void OnCancel(CancelEventArgs e)
        {
        }

        protected virtual bool GetCanCancel()
        {
            return true;
        }

        protected override void OnParameterChanged(object parameter)
        {
            Model = (TModel)Parameter;
        }

        protected IEnumerable<ValidationResultItem> GetValidationItemsFromException(Exception exception)
        {
            if (exception is UnexpectedErrorException)
            {
                yield return new ValidationResultItem(Resources.ServerConnectError, true);
                yield break;
            }

            if (exception is UnexpectedSatusException unexpectedSatusException)
            {
                IReadOnlyCollection<Error> errorDetails = unexpectedSatusException.Args.Error.Details;

                IEnumerable<ValidationResultItem> resultItems = errorDetails != null && errorDetails.Any()
                    ? errorDetails.Select(x => new ValidationResultItem(x.ErrorMessage, true))
                    : new[] { new ValidationResultItem("Внутрення ошибка сервера", true) };

                foreach (ValidationResultItem resultItem in resultItems)
                {
                    yield return resultItem;
                }

                yield break;
            }

            yield return new ValidationResultItem("Непредвиденная ошибка", true);
        }
    }
}