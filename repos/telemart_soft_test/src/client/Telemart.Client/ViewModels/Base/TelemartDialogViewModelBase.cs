using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Base
{
    public abstract class TelemartDialogViewModelBase : TelemartViewModelBase, IDocumentContent, IDataErrorInfo
    {
        private bool closed;
        private readonly IDisposable _disposable;

        protected TelemartDialogViewModelBase(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            ILogger logger = null)
            : base(webClient, dictionaries, messageFacadeService, logger)
        {
            OkCommand = new AsyncCommand(HandleOkAsync, CanOk, false);
            CancelCommand = new DelegateCommand(HandleCancel, false);

            _disposable = LogContext.PushProperty("ViewModelInstanceRef", Guid.NewGuid());
        }

        protected TelemartDialogViewModelBase()
        {
        }

        public IDelegateCommand CancelCommand { get; private set; }

        public IDocumentOwner DocumentOwner { get; set; }

        public bool IsOk { get; protected set; }

        public IAsyncCommand OkCommand { get; }

        public object Title
        {
            get { return GetProperty(() => Title); }
            protected set { SetProperty(() => Title, value); }
        }

        public virtual int Width => 1280;

        public virtual int MinWidth => 720;

        public virtual int MaxWidth => 1920;

        public virtual int Height => 720;

        public virtual int MinHeight => 576;

        public virtual int MaxHeight => 1080;

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        #endregion

        protected IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        protected IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        protected IDocumentManagerService NonModalDialogDocumentManagerService => GetService<IDocumentManagerService>("NonModalDialogDocumentManagerService");

        protected IDocumentManagerService NonModalSizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("NotModalSizeableDocumentManagerService");

        string IDataErrorInfo.this[string columnName] => GetErrorText(columnName);

        public virtual void OnClose(CancelEventArgs e)
        {
            closed = true;
            _disposable?.Dispose();
        }

        public virtual void OnDestroy()
        {
        }

        protected virtual bool CanOk()
        {
            return true;
        }

        protected virtual void Close()
        {
            DocumentOwner.Close(this, false);
        }

        protected virtual void HandleCancel()
        {
            Close();
        }

        protected abstract Task HandleOkAsync();

        protected override void OnHandleLoadedFinished(string error = null)
        {
            if (closed || string.IsNullOrWhiteSpace(error))
            {
                return;
            }

            MessageFacadeService.ShowNotificationError(error);
            Close();
        }

        protected override void OnHandleLoadedStarted()
        {
            if (Title == null)
            {
                Title = "Загрузка ... ";
            }
        }

        protected virtual string GetErrorText(string columnName)
        {
            return IDataErrorInfoHelper.GetErrorText(this, columnName);
        }

        protected bool ShowValidationResultView(string title, IEnumerable<ValidationResultItem> validationItems)
        {
            ValidationResultItem[] validationItemsArray = validationItems as ValidationResultItem[] ?? validationItems.ToArray();

            ValidationResultViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItemsArray),
                this);

            string validationItemsString = string.Join(", ", validationItemsArray.Select(x => x.Message));

            Logger.LogInformation("ValidationItems: {validationItems}", validationItemsString);

            return viewModel.IsOk;
        }

        protected void CloseOk()
        {
            IsOk = true;

            Close();
        }
    }

    public abstract class TelemartDialogViewModelBase<TParameter, TResult> : TelemartDialogViewModelBase
    {
        private TResult result;

        protected TelemartDialogViewModelBase(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        protected TelemartDialogViewModelBase()
        {
        }

        public new TParameter Parameter => (TParameter)base.Parameter;

        public DialogResult<TResult> GetResult()
        {
            return new DialogResult<TResult>(IsOk, result);
        }

        protected void SetResult(TResult result)
        {
            this.result = result;
        }
    }
}