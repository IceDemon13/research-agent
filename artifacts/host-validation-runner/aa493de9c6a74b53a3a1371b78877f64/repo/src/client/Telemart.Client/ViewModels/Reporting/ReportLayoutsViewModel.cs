using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Report;
using Telemart.Client.Data.Requests.Features.ReportLayout;
using Telemart.Client.Data.Requests.Features.ReportLayout.TransferObjects;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Reporting.ViewItems;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Reporting
{
    internal sealed class ReportLayoutsViewModel : TelemartDialogViewModelBase
    {
        private IReadOnlyDictionary<int, EmployeeDto> employees;
        private ReportLayoutsParameter parameter;

        public ReportLayoutsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            Mapper = mapper;

            AddCommand = new AsyncCommand(AddLayoutAsync);
            LoadCommand = new DelegateCommand<ReportLayoutViewItem>(LoadLayout, x => x != null);
            SaveCommand = new AsyncCommand<ReportLayoutViewItem>(SaveLayoutAsync, x => x != null);
            RemoveCommand = new AsyncCommand<ReportLayoutViewItem>(RemoveLayoutAsync, x => x != null);
            HandleRowDoubleClickCommand = new DelegateCommand(HandleRowDoubleClick);
        }

        public ReportLayoutsViewModel()
        {
        }

        #region Commands

        public IAsyncCommand AddCommand { get; }

        public IDelegateCommand LoadCommand { get; }

        public IAsyncCommand SaveCommand { get; }

        public IAsyncCommand RemoveCommand { get; }

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        #endregion

        #region INPC

        public bool IsLoad
        {
            get { return GetProperty(() => IsLoad); }
            set { SetProperty(() => IsLoad, value); }
        }

        public ObservableCollection<ReportLayoutViewItem> Items
        {
            get { return GetProperty(() => Items); }
            private set { SetProperty(() => Items, value); }
        }

        public ReportLayoutViewItem CurrentReportLayout
        {
            get { return GetProperty(() => CurrentReportLayout); }
            set { SetProperty(() => CurrentReportLayout, value); }
        }

        public ReadOnlyObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        #endregion

        #region DialogSettings

        public override int Height => 432;

        public override int MinHeight => 360;

        public override int MinWidth => 640;

        public override int Width => 768;

        #endregion

        private IMessenger Messenger { get; }

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            parameter = (ReportLayoutsParameter)Parameter;

            List<EmployeeDto> employeesResult = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            employees = employeesResult.ToDictionary(x => x.Id);
            Employees = employeesResult.ToReadOnlyObservableCollection();

            List<ReportLayoutDto> layouts = await WebClient.ExecuteReportApiRequestAsync(new QueryReportLayouts(parameter.ReportId));

            Items = layouts
                .OrderByDescending(x => x.View == (int)parameter.LayoutType)
                .ThenByDescending(x => x.CreatedBy == WebClient.AuthenticatedEmployee.Id)
                .ThenByDescending(x => employees.GetValueOrDefault(x.CreatedBy ?? -1)?.Active == true)
                .ThenBy(x => x.Name)
                .Select(x => Mapper.Map<ReportLayoutViewItem>(x))
                .ToObservableCollection();

            IsLoad = parameter.IsLoad;

            Title = "Настройки";
        }

        protected override Task HandleOkAsync()
        {
            if (CurrentReportLayout == null)
            {
                MessageFacadeService.ShowNotificationWarning("Настройка не выбрана");
            }
            else
            {
                if (IsLoad)
                {
                    LoadCommand.Execute(CurrentReportLayout);
                }
                else
                {
                    SaveCommand.Execute(CurrentReportLayout);
                }
            }

            return Task.CompletedTask;
        }

        private static ReportLayoutParameter[] DeserializeParameters(string parametersJson)
        {
            return JsonConvert.DeserializeObject<ReportLayoutParameter[]>(parametersJson ?? "[]");
        }

        private static string SerializeParameters(ReportLayoutParameter[] parameters)
        {
            return JsonConvert.SerializeObject(parameters);
        }

        private async Task RemoveLayoutAsync(ReportLayoutViewItem layoutViewItem)
        {
            if (layoutViewItem.CreatedBy != WebClient.AuthenticatedEmployee.Id)
            {
                MessageFacadeService.ShowNotificationWarning("Запрещено удалять настройки других пользователей");
                return;
            }

            if (!MessageFacadeService.Confirm($"Удалить настройку \"{layoutViewItem.Name}\"?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteReportApiRequestAsync(new DeleteReportLayout(layoutViewItem.Id));

                Items.Remove(layoutViewItem);

                MessageFacadeService.ShowNotificationInfo("Настройки успешно удалены");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to delete report layout");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении настроек");
            }
        }

        private async Task SaveLayoutAsync(ReportLayoutViewItem layoutViewItem)
        {
            if (layoutViewItem.View != parameter.LayoutType)
            {
                string allowedLayoutType = parameter.LayoutType == ReportLayoutType.Grid
                    ? "Отчет"
                    : "Анализ";

                MessageFacadeService.ShowNotificationWarning($"Тип настройки должен быть \"{allowedLayoutType}\"");
                return;
            }

            if (layoutViewItem.CreatedBy != WebClient.AuthenticatedEmployee.Id)
            {
                MessageFacadeService.ShowNotificationWarning("Запрещено перезаписывать настройки других пользователей");
                return;
            }

            if (!MessageFacadeService.Confirm($"Сохранить вид в настройку \"{layoutViewItem.Name}\"?"))
            {
                return;
            }

            try
            {
                ReportLayoutSaveDto saveDto = new ReportLayoutSaveDto(
                    layoutViewItem.Id,
                    layoutViewItem.ReportId,
                    (int)parameter.LayoutType,
                    layoutViewItem.Name,
                    parameter.CurrentLayout,
                    SerializeParameters(parameter.CurrentParameters));

                await WebClient.ExecuteReportApiRequestAsync(new UpdateReportLayout(layoutViewItem.Id, saveDto));

                MessageFacadeService.ShowNotificationInfo("Настройки успешно сохранены");

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении настроек");
                ShowValidationResultView("Ошибки при сохранении настроек", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to save report layout");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to save report layout");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении настроек");
            }
        }

        private async Task AddLayoutAsync()
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                "Название",
                "Введите название настройки",
                @"^(?!\s*$).+",
                Resources.RequiredErrorMessage);

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (fromUserViewModel.IsOk)
            {
                try
                {
                    ReportLayoutSaveDto saveDto = new ReportLayoutSaveDto(
                        0,
                        parameter.ReportId,
                        (int)parameter.LayoutType,
                        fromUserViewModel.Content.Trim(),
                        parameter.CurrentLayout,
                        SerializeParameters(parameter.CurrentParameters));

                    Result<ReportLayoutDto> result = await WebClient.ExecuteReportApiRequestAsync(new CreateReportLayout(saveDto));

                    Items.Add(Mapper.Map<ReportLayoutViewItem>(result.Data));

                    MessageFacadeService.ShowNotificationInfo("Настройки сохранены успешно");

                    IsOk = true;
                    Close();
                }
                catch (UnexpectedSatusException exception)
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при сохранении настроек");
                    ShowValidationResultView("Ошибки при сохранении настроек", exception.GetErrorItems());
                }
                catch (UnexpectedErrorException exception)
                {
                    Logger.LogError(exception, "Failed to create report layout");
                    ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed to create report layout");
                    MessageFacadeService.ShowNotificationError("Ошибка при сохранении настроек");
                }
            }
        }

        private void LoadLayout(ReportLayoutViewItem layoutViewItem)
        {
            LoadReportLayoutMessage message = new LoadReportLayoutMessage(
                layoutViewItem.ReportId,
                parameter.ReportEditorId,
                layoutViewItem.View,
                layoutViewItem.Layout,
                DeserializeParameters(layoutViewItem.Parameters));

            Messenger.Send(message);

            IsOk = true;
            Close();
        }

        private void HandleRowDoubleClick()
        {
            if (CurrentReportLayout != null)
            {
                if (IsLoad)
                {
                    if (MessageFacadeService.Confirm($"Загрузить настройку \"{CurrentReportLayout.Name}\"?"))
                    {
                        LoadCommand.Execute(CurrentReportLayout);
                    }
                }
                else
                {
                    SaveCommand.Execute(CurrentReportLayout);
                }
            }
        }
    }
}