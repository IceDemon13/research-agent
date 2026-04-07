using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.WorkSchedule;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.WorkSchedule;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Common.ErrorHandling;
using DayOfWeek = Telemart.Client.Dictionaries.DayOfWeek;

namespace Telemart.Client.ViewModels.WorkSchedule
{
    public sealed class WorkSchedulesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IMapper _mapper;
        private int _workSchedulesCount;
        private int _holidaysCount;

        public WorkSchedulesViewModel(
            IWebClient webClient,
            IErrorHandler errorHandler,
            IDictionaries dictionaries,
            IMapper mapper,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
            _mapper = mapper;
            RefreshWorkSchedulesCommand = new AsyncCommand<bool>(RefreshWorkSchedulesAsync);
            AddWorkScheduleCommand = new DelegateCommand(AddWorkSchedule);
            DeleteWorkScheduleCommand = new DelegateCommand<WorkScheduleViewItem>(DeleteWorkSchedule, x => x != null);
            SaveWorkSchedulesCommand = new AsyncCommand(SaveWorkSchedulesAsync, CanSaveWorkSchedule);
            HandleWorkScheduleShowingEditorCommand = new DelegateCommand<ShowingEditorEventArgs>(HandleWorkScheduleShowingEditor);

            RefreshHolidaysCommand = new AsyncCommand<bool>(RefreshHolidaysAsync);
            SaveHolidaysCommand = new AsyncCommand(SaveHolidaysAsync, CanSaveHolidays);
            AddHolidayCommand = new DelegateCommand(AddHoliday);
            DeleteHolidayCommand = new DelegateCommand<HolidayViewItem>(DeleteHoliday, x => x != null);
            HandleHolidaysShowingEditorCommand = new DelegateCommand<ShowingEditorEventArgs>(HandleHolidaysShowingEditor);
            CreateNameCommand = new DelegateCommand<WorkScheduleViewItem>(CreateName);

            AllNames = new ObservableCollection<string>();
        }

        #region WorkSchedules

        public IAsyncCommand RefreshWorkSchedulesCommand { get; }

        public IAsyncCommand SaveWorkSchedulesCommand { get; }

        public IDelegateCommand AddWorkScheduleCommand { get; }

        public IDelegateCommand DeleteWorkScheduleCommand { get; }

        public IDelegateCommand HandleWorkScheduleShowingEditorCommand { get; }

        public IDelegateCommand CreateNameCommand { get; }

        #endregion

        #region Holidays

        public IAsyncCommand RefreshHolidaysCommand { get; }

        public IAsyncCommand SaveHolidaysCommand { get; }

        public IDelegateCommand AddHolidayCommand { get; }

        public IDelegateCommand DeleteHolidayCommand { get; }

        public IDelegateCommand HandleHolidaysShowingEditorCommand { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<WorkScheduleType> WorkScheduleTypes
        {
            get { return GetProperty(() => WorkScheduleTypes); }
            set { SetProperty(() => WorkScheduleTypes, value); }
        }

        public ObservableCollection<WorkScheduleViewItem> WorkSchedules
        {
            get { return GetProperty(() => WorkSchedules); }
            private set { SetProperty(() => WorkSchedules, value); }
        }

        public ObservableCollection<string> AllNames
        {
            get { return GetProperty(() => AllNames); }
            private set { SetProperty(() => AllNames, value); }
        }

        public WorkScheduleViewItem SelectedWorkScheduleViewItem
        {
            get { return GetProperty(() => SelectedWorkScheduleViewItem); }
            set { SetProperty(() => SelectedWorkScheduleViewItem, value); }
        }

        public ReadOnlyObservableCollection<DayOfWeek> DaysOfWeek
        {
            get { return GetProperty(() => DaysOfWeek); }
            private set { SetProperty(() => DaysOfWeek, value); }
        }

        public ObservableCollection<HolidayViewItem> Holidays
        {
            get { return GetProperty(() => Holidays); }
            private set { SetProperty(() => Holidays, value); }
        }

        public HolidayViewItem SelectedHolday
        {
            get { return GetProperty(() => SelectedHolday); }
            set { SetProperty(() => SelectedHolday, value); }
        }

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        protected override async Task HandleLoadedAsync()
        {
            WorkScheduleTypes = Dictionaries.GetItems<WorkScheduleType>().ToReadOnlyObservableCollection();
            DaysOfWeek = Dictionaries.GetItems<DayOfWeek>().ToReadOnlyObservableCollection();

            await Task.WhenAll(RefreshWorkSchedulesAsync(), RefreshHolidaysAsync());

            await base.HandleLoadedAsync();
        }

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            return false;
        }

        private static SaveWorkScheduleDto MapToWorkScheduleDto(WorkScheduleViewItem item)
        {
            return new SaveWorkScheduleDto
            {
                Id = item.Id,
                TypeId = item.TypeId,
                Start = item.Start,
                End = item.End,
                Days = item.Days,
                Name = item.Name
            };
        }

        private static SaveHolidayDto MapToSaveHolidayDto(HolidayViewItem item)
        {
            return new SaveHolidayDto
            {
                Id = item.Id,
                TypeId = item.TypeId,
                Start = item.Start,
                End = item.End,
                Date = item.Date
            };
        }

        private static BusinessOperation GetOperationByTypeId(int typeId) => typeId switch
        {
            WorkScheduleType.CallSenterTypeId => BusinessOperation.AllowWorkSchedulesTypeCallSenter,
            WorkScheduleType.CallSenterServiceTypeId => BusinessOperation.AllowWorkSchedulesTypeCallSenterService,
            WorkScheduleType.AdditionalServiceTypeId => BusinessOperation.AllowWorkSchedulesTypeAdditionalService,
            WorkScheduleType.TypeOutsideTypeId => BusinessOperation.AllowWorkSchedulesTypeOutside,
            WorkScheduleType.TypeForReportTypeId => BusinessOperation.AllowWorkSchedulesTypeForReport,
            WorkScheduleType.ShopTypeId => BusinessOperation.AllowWorkSchedulesTypeShop,
            WorkScheduleType.Supplier => BusinessOperation.AllowWorkSchedulesTypeSupplier,
            WorkScheduleType.Sms => BusinessOperation.AllowWorkSchedulesTypeSms,
            WorkScheduleType.Production => BusinessOperation.AllowWorkSchedulesTypeProduction,
            _ => BusinessOperation.None
        };

        private async Task RefreshWorkSchedulesAsync(bool ignoreCanSave = false)
        {
            if (!ignoreCanSave && WorkSchedules is not null && CanSaveWorkSchedule() && !MessageFacadeService.Confirm("Обновить без сохранения изменений?"))
            {
                return;
            }

            List<WorkScheduleDto> workSchedules = await WebClient.ExecuteApiRequestAsync(new QueryWorkSchedules());

            _workSchedulesCount = workSchedules.Count;

            AllNames.Clear();
            AllNames.AddRange(workSchedules
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => x.Name)
                .Distinct()
                .OrderBy(x => x));

            WorkSchedules = workSchedules.Select(x => _mapper.Map<WorkScheduleViewItem>(x)).ToObservableCollection();

            WorkSchedules.ForEach(
                x =>
                {
                    WorkScheduleType type = WorkScheduleTypes.FirstOrDefault(y => y.Id == x.TypeId);

                    x.ParentTypeName = type?.ParentId.HasValue == true ? WorkScheduleTypes.FirstOrDefault(y => y.Id == type?.ParentId)?.Name : null;

                    x.TypeName = type?.Name;

                });
        }

        private async Task RefreshHolidaysAsync(bool ignoreCanSave = false)
        {
            if (!ignoreCanSave && Holidays is not null && CanSaveHolidays() && !MessageFacadeService.Confirm("Обновить без сохранения изменений?"))
            {
                return;
            }

            List<HolidayDto> holidays = await WebClient.ExecuteApiRequestAsync(new QueryHolidays());

            _holidaysCount = holidays.Count;

            Holidays = holidays.OrderBy(x => x.Date).Select(x => _mapper.Map<HolidayViewItem>(x)).ToObservableCollection();

            Holidays.ForEach(
                x =>
                {
                    WorkScheduleType type = WorkScheduleTypes.FirstOrDefault(y => y.Id == x.TypeId);

                    x.ParentTypeName = type?.ParentId.HasValue == true ? WorkScheduleTypes.FirstOrDefault(y => y.Id == type?.ParentId)?.Name : null;

                    x.TypeName = type?.Name;
                });
        }

        private async Task SaveWorkSchedulesAsync()
        {
            WorkScheduleViewItem[] notValidSchedules = WorkSchedules
                .Where(x => string.IsNullOrEmpty(x.Days) || !x.Start.HasValue || x.Start.Value < default(TimeSpan) || !x.End.HasValue || x.End.Value < default(TimeSpan))
                .ToArray();

            if (notValidSchedules.Any())
            {
                MessageFacadeService.ShowMessageBoxWarning("Hеккоректно заполненные данные");
                return;
            }

            WorkSchedulesDto workSchedules = new WorkSchedulesDto
            {
                WorkSchedules = WorkSchedules.Select(MapToWorkScheduleDto).ToArray()
            };

            Result result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new SaveWorkSchedules(workSchedules)),
                "сохранении графиков",
                "Графики сохранены",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                await RefreshWorkSchedulesAsync(true);
            }
        }

        private bool CanSaveWorkSchedule()
        {
            return WorkSchedules?.Any(x => x.IsChanged) == true || _workSchedulesCount != WorkSchedules?.Count;
        }

        private void AddWorkSchedule()
        {
            SelectedWorkScheduleViewItem = null;

            WorkScheduleType[] allowWorkScheduleTypes = WorkScheduleTypes.Where(x => WebClient.IsOperationAllowed(GetOperationByTypeId(x.Id)) || GetOperationByTypeId(x.Id) == BusinessOperation.None).ToArray();

            if (allowWorkScheduleTypes.Any())
            {
                WorkScheduleType selectedType = allowWorkScheduleTypes.First();

                if (allowWorkScheduleTypes.Length > 1)
                {
                    WorkScheduleChangeTypeViewModel model = DialogDocumentManagerService.ShowView<WorkScheduleChangeTypeViewModel>(
                        WorkScheduleTypes.Where(x => WebClient.IsOperationAllowed(GetOperationByTypeId(x.Id))
                                                     || (x.ParentId.HasValue == false && GetOperationByTypeId(x.Id) == BusinessOperation.None)
                                                     || (x.ParentId.HasValue && (WebClient.IsOperationAllowed(GetOperationByTypeId(x.ParentId.Value)) || GetOperationByTypeId(x.ParentId.Value) == BusinessOperation.None)))
                            .ToArray(),
                        this);

                    if (!model.IsOk || model.WorkScheduleType == null)
                    {
                        return;
                    }

                    selectedType = model.WorkScheduleType;
                }

                WorkSchedules.Insert(0, new WorkScheduleViewItem
                {
                    TypeId = selectedType.Id,
                    TypeName = selectedType.Name,
                    ParentTypeName = allowWorkScheduleTypes.FirstOrDefault(x => x.Id == selectedType.ParentId)?.Name
                });
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("У вас нет доступа добавлнения записи");
            }
        }

        private void DeleteWorkSchedule(WorkScheduleViewItem workSchedule)
        {
            WorkScheduleType workScheduleType = WorkScheduleTypes.FirstOrDefault(x => x.Id == workSchedule.TypeId);

            if (WebClient.IsOperationAllowed(GetOperationByTypeId(workScheduleType!.Id))
                || (workScheduleType.ParentId.HasValue == false && GetOperationByTypeId(workScheduleType.Id) == BusinessOperation.None)
                || (workScheduleType.ParentId.HasValue && (WebClient.IsOperationAllowed(GetOperationByTypeId(workScheduleType.ParentId.Value)) || GetOperationByTypeId(workScheduleType.ParentId.Value) == BusinessOperation.None)))
            {
                if (!MessageFacadeService.Confirm("Вы уверены что хотите удалить запись?"))
                {
                    return;
                }

                WorkSchedules.Remove(workSchedule);

                return;
            }

            MessageFacadeService.ShowNotificationWarning("У вас нет доступа удаление записи");
        }

        private async Task SaveHolidaysAsync()
        {
            HolidayViewItem[] notValid = Holidays.Where(x => x.Date == default || (x.End.HasValue && x.Start.HasValue && x.Start > x.End)).ToArray();

            if (notValid.Any())
            {
                MessageFacadeService.ShowMessageBoxWarning("Hеккоректно заполненные данные");
                return;
            }

            SaveHolidaysDto saveHolidays = new SaveHolidaysDto(Holidays.Select(MapToSaveHolidayDto).ToArray());

            Result result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new SaveHolidays(saveHolidays)),
                "сохранении праздников",
                "Праздники сохранены",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                await RefreshHolidaysAsync(true);
            }
        }

        private bool CanSaveHolidays()
        {
            return Holidays?.Any(x => x.IsChanged) == true || _holidaysCount != Holidays?.Count;
        }

        private void AddHoliday()
        {
            SelectedHolday = null;

            WorkScheduleType[] allowWorkScheduleTypes = WorkScheduleTypes.Where(x => WebClient.IsOperationAllowed(GetOperationByTypeId(x.Id)) || GetOperationByTypeId(x.Id) == BusinessOperation.None).ToArray();

            if (allowWorkScheduleTypes.Any())
            {
                WorkScheduleType type = allowWorkScheduleTypes.First();

                if (allowWorkScheduleTypes.Length > 1)
                {
                    WorkScheduleChangeTypeViewModel model = DialogDocumentManagerService.ShowView<WorkScheduleChangeTypeViewModel>(
                        WorkScheduleTypes.Where(x => WebClient.IsOperationAllowed(GetOperationByTypeId(x.Id))
                                                     || (x.ParentId.HasValue == false && GetOperationByTypeId(x.Id) == BusinessOperation.None)
                                                     || (x.ParentId.HasValue && (WebClient.IsOperationAllowed(GetOperationByTypeId(x.ParentId.Value)) || GetOperationByTypeId(x.ParentId.Value) == BusinessOperation.None)))
                            .ToArray(),
                        this);

                    if (!model.IsOk || model.WorkScheduleType == null)
                    {
                        return;
                    }

                    type = model.WorkScheduleType;
                }

                Holidays.Insert(0, new HolidayViewItem
                {
                    TypeId = type.Id,
                    Date = DateTime.Now,
                    TypeName = type.Name,
                    ParentTypeName = allowWorkScheduleTypes.FirstOrDefault(x => x.Id == type.ParentId)?.Name
                });
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("У вас нет доступа на добавление");
            }
        }

        private void DeleteHoliday(HolidayViewItem holiday)
        {
            WorkScheduleType workScheduleType = WorkScheduleTypes.FirstOrDefault(x => x.Id == holiday.TypeId);

            if (WebClient.IsOperationAllowed(GetOperationByTypeId(workScheduleType!.Id))
                || (workScheduleType.ParentId.HasValue == false && GetOperationByTypeId(workScheduleType.Id) == BusinessOperation.None)
                || (workScheduleType.ParentId.HasValue && (WebClient.IsOperationAllowed(GetOperationByTypeId(workScheduleType.ParentId.Value)) || GetOperationByTypeId(workScheduleType.ParentId.Value) == BusinessOperation.None)))
            {
                if (!MessageFacadeService.Confirm("Вы уверены что хотите удалить запись?"))
                {
                    return;
                }

                Holidays.Remove(holiday);
            }

            MessageFacadeService.ShowNotificationWarning("У вас нет доступа на удаление записи");
        }

        private void HandleWorkScheduleShowingEditor(ShowingEditorEventArgs args)
        {
            WorkScheduleViewItem item = (WorkScheduleViewItem)args.Row;

            WorkScheduleType workScheduleType = WorkScheduleTypes.FirstOrDefault(x => x.Id == item.TypeId);

            BusinessOperation allowChangeRow = GetOperationByTypeId(item.TypeId);

            if (WebClient.IsOperationAllowed(allowChangeRow)
                || (workScheduleType!.ParentId.HasValue == false && allowChangeRow == BusinessOperation.None)
                || (workScheduleType!.ParentId.HasValue && (WebClient.IsOperationAllowed(GetOperationByTypeId(workScheduleType.ParentId.Value)) || GetOperationByTypeId(workScheduleType.ParentId.Value) == BusinessOperation.None)))
            {
                return;
            }

            args.Cancel = true;

            MessageFacadeService.ShowNotificationWarning("У вас нет доступа на редактирование строки");
        }

        private void HandleHolidaysShowingEditor(ShowingEditorEventArgs args)
        {
            HolidayViewItem item = (HolidayViewItem)args.Row;

            WorkScheduleType workScheduleType = WorkScheduleTypes.FirstOrDefault(x => x.Id == item.TypeId);

            BusinessOperation allowChangeRow = GetOperationByTypeId(item.TypeId);

            if (WebClient.IsOperationAllowed(allowChangeRow)
                || (workScheduleType!.ParentId.HasValue == false && allowChangeRow == BusinessOperation.None)
                || (workScheduleType!.ParentId.HasValue && WebClient.IsOperationAllowed(GetOperationByTypeId(workScheduleType.ParentId.Value))))
            {
                return;
            }

            args.Cancel = true;

            MessageFacadeService.ShowNotificationWarning("У вас нет доступа на редактирование строки");
        }

        private void CreateName(WorkScheduleViewItem item)
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                "Укажите название",
                "Создание названия графика",
                null,
                "Не валидное значение. ");

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (!fromUserViewModel.IsOk)
            {
                return;
            }

            if (fromUserViewModel.Content.Length > 90)
            {
                MessageFacadeService.ShowNotificationError("Максимально допустимая длина 90 символов");
                return;
            }

            string nameNew = fromUserViewModel.Content.Trim();

            if (AllNames.Any(x => x == nameNew) != true)
            {
                AllNames.Add(nameNew);
                AllNames = AllNames.OrderBy(x => x).ToObservableCollection();
            }

            item.Name = nameNew;
        }
    }
}