using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Backlog;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Backlog;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Backlog
{
    public sealed class CreateBacklogTaskViewModel : TelemartDialogViewModelBase
    {
        private const int BitrixTaskCreatedState = 1;
        private const int BitrixTaskCreateOnBitrixState = 2;

        public CreateBacklogTaskViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public CreateBacklogTaskViewModel()
        {
        }

        #region INPC

        public ReadOnlyObservableCollection<Priority> Priorities
        {
            get { return GetProperty(() => Priorities); }
            private set { SetProperty(() => Priorities, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> BitrixTaskStates
        {
            get { return GetProperty(() => BitrixTaskStates); }
            private set { SetProperty(() => BitrixTaskStates, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<BacklogCategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ComboBoxItem? SelectedBitrixTaskState
        {
            get { return GetProperty(() => SelectedBitrixTaskState); }
            set { SetProperty(() => SelectedBitrixTaskState, value, RaiseBitrixTypeChanged); }
        }

        public bool? IsBitrixTaskCreated => SelectedBitrixTaskState.HasValue ? SelectedBitrixTaskState.Value.Id == BitrixTaskCreatedState : (bool?)null;

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string Formulation
        {
            get { return GetProperty(() => Formulation); }
            set { SetProperty(() => Formulation, value); }
        }

        public string Solution
        {
            get { return GetProperty(() => Solution); }
            set { SetProperty(() => Solution, value); }
        }

        public string Justification
        {
            get { return GetProperty(() => Justification); }
            set { SetProperty(() => Justification, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public int? BitrixId
        {
            get { return GetProperty(() => BitrixId); }
            set { SetProperty(() => BitrixId, value); }
        }

        public int? AuthorId
        {
            get { return GetProperty(() => AuthorId); }
            set { SetProperty(() => AuthorId, value); }
        }

        public IReadOnlyCollection<int> SelectedCategoryIds
        {
            get { return GetProperty(() => SelectedCategoryIds); }
            set { SetProperty(() => SelectedCategoryIds, value); }
        }

        #endregion

        public override int Width => 800;

        public override int Height => 600;

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<CreateBacklogTaskViewModel> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(250, () => "Значение поля должно быть короче 250 символов");
            builder.Property(x => x.BitrixId)
                .MatchesInstanceRule((x, y) => y.IsBitrixTaskCreated == false || x.HasValue, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.Formulation)
                .MatchesInstanceRule((x, y) => y.IsBitrixTaskCreated == true || !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage)
                .MinLength(20, () => "Значение поля должно быть длиннее 20 символов");
            builder.Property(x => x.AuthorId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedBitrixTaskState)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            Priorities = Dictionaries.GetItems<Priority>()
                .OrderByDescending(p => p.Id)
                .ToReadOnlyObservableCollection();

            AuthorId = WebClient.AuthenticatedEmployee.Id;

            BitrixTaskStates = new List<ComboBoxItem> { new ComboBoxItem(1, "Есть в битриксе"), new ComboBoxItem(2, "Создать в битрикс") }.ToReadOnlyObservableCollection();
            SelectedBitrixTaskState = BitrixTaskStates.FirstOrDefault(x => x.Id == BitrixTaskCreateOnBitrixState);

            await Task.WhenAll(RefreshCategories(), RefreshEmployees(), base.HandleLoadedAsync());

            Title = "Создание задачи";

            async Task RefreshEmployees()
            {
                List<EmployeeDto> employeeList = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

                Employees = employeeList
                    .Where(x => x.Active)
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshCategories()
            {
                List<BacklogCategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryBacklogCategories(), true);

                Categories = categories
                    .Where(x => x.Active)
                    .OrderBy(x => x.Name)
                    .Select(x => Mapper.Map<BacklogCategoryViewItem>(x))
                    .ToReadOnlyObservableCollection();
            }
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            if (Categories.Any(x => x.ParentId == null && x.Checked == false))
            {
                MessageFacadeService.ShowNotificationWarning("Отметьте хотя бы одну категорию в каждой ветке");
                return;
            }

            SelectedCategoryIds = Categories.Where(x => x.Checked != false).Select(x => x.Id).ToArray();

            try
            {
                Result<BacklogTaskDto> result = await WebClient.ExecuteApiRequestAsync(new CreateBacklogTask(Mapper.Map<BacklogTaskCreateDto>(this)));

                Messenger.Send(new BacklogTaskMessage(result.Data, MessageType.Added));

                IsOk = true;
                Close();

                MessageFacadeService.ShowNotificationInfo($"Задача №{result.Data.Id} успешно создана");

                Messenger.Send(new BacklogTaskViewMessage(result.Data.Id));
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании задачи");
                ShowValidationResultView("Ошибки при создании задачи", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create backlog task");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании задачи");
                Logger.LogError(exception, "Error while creating backlog task");
            }
        }

        private void RaiseBitrixTypeChanged()
        {
            RaisePropertiesChanged(nameof(IsBitrixTaskCreated), nameof(BitrixId), nameof(Formulation));
        }
    }
}