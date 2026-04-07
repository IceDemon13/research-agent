using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Newtonsoft.Json;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.ModuleLayout;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Layouts
{
    public sealed class ModuleLayoutsViewModel : TelemartDialogViewModelBase
    {
        private ModuleLayoutParameter parameter;

        public ModuleLayoutsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;

            AddCommand = new AsyncCommand(AddLayoutAsync);
            SaveCommand = new AsyncCommand(SaveLayoutAsync, () => SelectedLayout != null);
            RemoveCommand = new AsyncCommand(RemoveLayoutAsync, () => SelectedLayout != null);
            HandleRowDoubleClickCommand = new DelegateCommand(HandleRowDoubleClick);
            ErrorHandler = errorHandler;
        }

        public ModuleLayoutsViewModel()
        {
        }

        #region Commands

        public IAsyncCommand AddCommand { get; }

        public IAsyncCommand SaveCommand { get; }

        public IAsyncCommand RemoveCommand { get; }

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        #endregion

        public ObservableCollection<ModuleLayoutViewItem> Layouts
        {
            get { return GetProperty(() => Layouts); }
            private set { SetProperty(() => Layouts, value); }
        }

        public ModuleLayoutViewItem SelectedLayout
        {
            get { return GetProperty(() => SelectedLayout); }
            set { SetProperty(() => SelectedLayout, value); }
        }

        public bool IsLoad
        {
            get { return GetProperty(() => IsLoad); }
            private set { SetProperty(() => IsLoad, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        #region DialogSettings

        public override int Height => 432;

        public override int MinHeight => 360;

        public override int MinWidth => 640;

        public override int Width => 768;

        #endregion

        private IMapper Mapper { get; }

        private IErrorHandler ErrorHandler { get; }

        protected override async Task HandleLoadedAsync()
        {
            parameter = (ModuleLayoutParameter)Parameter;

            IsLoad = parameter.IsLoad;

            List<ModuleLayoutDto> layouts = await WebClient.ExecuteApiRequestAsync(new QueryModuleLayouts(parameter.ModuleId));

            Layouts = layouts.Select(x => Mapper.Map<ModuleLayoutViewItem>(x)).ToObservableCollection();

            List<EmployeeDto> employeesResult = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employeesResult.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();

            Title = IsLoad ? "Выбор шаблона модуля" : "Сохранение шаблона модуля";

            await base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            if (SelectedLayout == null)
            {
                MessageFacadeService.ShowNotificationWarning("Шаблон модуля не выбран");
            }
            else
            {
                if (IsLoad)
                {
                    CloseOk();
                }
                else
                {
                    SaveCommand.Execute(null);
                }
            }

            return Task.CompletedTask;
        }

        private static string SerializeParameters((string name, object value)[] parameters)
        {
            return JsonConvert.SerializeObject(parameters);
        }

        private async Task AddLayoutAsync()
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                "Название",
                "Введите название шаблона",
                @"^(?!\s*$).+",
                "Не валидное значение. ");

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (fromUserViewModel.IsOk)
            {
                ModuleLayoutCreateDto createDto = new ModuleLayoutCreateDto(
                    fromUserViewModel.Content,
                    parameter.Layout,
                    parameter.ModuleId,
                    SerializeParameters(parameter.Parameters));

                (await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new CreateModuleLayout(createDto)), "создании шаблона модуля", "Шаблон модуля создан", this, true))
                    .IfNotNull(x =>
                    {
                        Layouts.Add(Mapper.Map<ModuleLayoutViewItem>(x.Data));
                    });
            }
        }

        private async Task SaveLayoutAsync()
        {
            if (SelectedLayout.CreatedBy != WebClient.AuthenticatedEmployee.Id)
            {
                MessageFacadeService.ShowNotificationWarning("Запрещено перезаписывать шаблон модуля другого пользователя");
                return;
            }

            ModuleLayoutUpdateDto saveDto = new ModuleLayoutUpdateDto(SelectedLayout.Id, parameter.Layout, SerializeParameters(parameter.Parameters));

            (await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new UpdateModuleLayout(SelectedLayout.Id, saveDto)), "сохранении шаблона модуля", "Шаблон модуля сохранен", this, true, confirmText: "Вы уверены?")).IfNotNull(x =>
            {
               CloseOk();
            });
        }

        private async Task RemoveLayoutAsync()
        {
            if (SelectedLayout.CreatedBy != WebClient.AuthenticatedEmployee.Id)
            {
                MessageFacadeService.ShowNotificationWarning("Запрещено удалять шаблон модуля другого пользователя");
                return;
            }

            await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new DeleteModuleLayout(SelectedLayout.Id)),
                "удалении шаблона модуля",
                "Шаблон модуля удален",
                this,
                true,
                confirmText: "Вы уверены?",
                onSuccess: (_, _) =>
                {
                        Layouts.Remove(SelectedLayout);
                        return Task.CompletedTask;
                });
        }

        private void HandleRowDoubleClick()
        {
            if (SelectedLayout != null)
            {
                if (IsLoad)
                {
                    if (MessageFacadeService.Confirm($"Загрузить настройку \"{SelectedLayout.Name}\"?"))
                    {
                        OkCommand.Execute(null);
                    }
                }
                else
                {
                    SaveCommand.Execute(null);
                }
            }
        }
    }
}