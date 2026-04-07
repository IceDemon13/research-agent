using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.TreeList;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Report;
using Telemart.Client.Data.Requests.Features.Report.TransferObjects;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Reporting.ViewItems;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Reporting
{
    internal sealed class ReportSelectionViewModel : TelemartDialogViewModelBase
    {
        public ReportSelectionViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            Mapper = mapper;

            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);
            HandleRowDoubleClickCommand = new DelegateCommand<RowDoubleClickEventArgs>(HandleRowDoubleClick);

            AddCommand = new AsyncCommand<ReportSimpleViewItem>(AddAsync, x => x != null);
            EditCommand = new AsyncCommand<ReportSimpleViewItem>(EditAsync, x => x != null && !x.IsFolder);
            DeleteCommand = new AsyncCommand<ReportSimpleViewItem>(DeleteAsync, x => x != null && !x.IsFolder);

            Messenger.Register<ReportMessage>(this, OnReportMessage);
        }

        public ReportSelectionViewModel()
        {
        }

        #region Commands

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        public IAsyncCommand AddCommand { get; set; }

        public IAsyncCommand EditCommand { get; set; }

        public IAsyncCommand DeleteCommand { get; set; }

        #endregion

        #region INPC

        public ObservableCollection<ReportSimpleViewItem> Reports
        {
            get { return GetProperty(() => Reports); }
            private set { SetProperty(() => Reports, value); }
        }

        public ReportSimpleViewItem SelectedReport
        {
            get { return GetProperty(() => SelectedReport); }
            set { SetProperty(() => SelectedReport, value); }
        }

        public bool CanCreate => WebClient.IsOperationAllowed(BusinessOperation.AllowReportSettings);

        public bool CanDelete => WebClient.IsOperationAllowed(BusinessOperation.AllowReportSettings);

        public bool CanUpdate => WebClient.IsOperationAllowed(BusinessOperation.AllowReportSettings);

        #endregion

        #region DialogSettings

        public override int Height => 462;

        public override int MinHeight => 400;

        public override int MinWidth => 600;

        public override int Width => 700;

        #endregion

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        protected override async Task HandleLoadedAsync()
        {
            List<ReportSimpleDto> reports = await WebClient.ExecuteReportApiRequestAsync(new QueryReports());

            Reports = reports
                .OrderBy(x => x.Name)
                .Select(x => Mapper.Map<ReportSimpleViewItem>(x))
                .ToObservableCollection();
        }

        protected override Task HandleOkAsync()
        {
            if (SelectedReport == null)
            {
                MessageFacadeService.ShowNotificationWarning("Отчет не выбран");
                return Task.CompletedTask;
            }

            if (Reports.Any(x => x.ParentId == SelectedReport.Id))
            {
                MessageFacadeService.ShowNotificationWarning("Выберите отчет последнего уровня");
                return Task.CompletedTask;
            }

            Messenger.Send(new ShowReportMessage(SelectedReport.Id, SelectedReport.Name));

            IsOk = true;
            Close();

            return Task.CompletedTask;
        }

        private void HandlePreviewKeyDown(KeyEventArgs eventArgs)
        {
            if (eventArgs.Key == Key.Enter && SelectedReport != null)
            {
                OkCommand.Execute(null);
                eventArgs.Handled = true;
            }
        }

        private void HandleRowDoubleClick(RowDoubleClickEventArgs e)
        {
            TreeListViewHitInfo hitInfo = (TreeListViewHitInfo)e.HitInfo;

            if (hitInfo.InRowCell)
            {
                OkCommand.Execute(null);
            }
        }

        private async Task DeleteAsync(ReportSimpleViewItem report)
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteReportApiRequestAsync(new DeleteReport(report.Id));

                MessageFacadeService.ShowNotificationInfo($"Отчет №{report.Id.ToString()} удален успешно");
                Reports.Remove(report);
            }
            catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.Forbidden)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
                MessageFacadeService.ShowValidationResultView("Ошибка при удалении отчета", new[] { new ValidationResultItem(Resources.ErrorForbidden, true) }, this);
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError(Resources.ServerConnectError);
            }
            catch (Exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении отчета");
            }
        }

        private Task EditAsync(ReportSimpleViewItem report)
        {
            Messenger.Send(new ReportViewMessage(report.Id));
            return Task.CompletedTask;
        }

        private Task AddAsync(ReportSimpleViewItem report)
        {
            Messenger.Send(new ReportViewMessage(0) { ParentId = report.IsFolder ? report.Id : report.ParentId });
            return Task.CompletedTask;
        }

        private void OnReportMessage(ReportMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    {
                        ReportSimpleViewItem viewItem = Mapper.Map<ReportSimpleViewItem>(message.Entity);
                        Reports.Add(viewItem);
                        SelectedReport = viewItem;
                        break;
                    }

                case MessageType.Changed:
                    {
                        Reports.DoActionWithItem(x => x.Id == message.Entity.Id, x => Mapper.Map(message.Entity, x));
                        break;
                    }
            }
        }
    }
}