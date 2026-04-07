using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.Requests.Features.Warehouse.Delivery;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.Warehouse.Delivery;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;
using DayOfWeek = Telemart.Client.Dictionaries.DayOfWeek;

namespace Telemart.Client.ViewModels.Warehouse
{
    public sealed class WarehouseDeliveryEditViewModel
        : TelemartEditorViewModelBase<DeliveryDto, WarehouseDeliveryEditParameter, WarehouseDeliveryViewItem>
    {
        private readonly IErrorHandler _errorHandler;

        private WarehouseDeliveryEditParameter parameter;

        public WarehouseDeliveryEditViewModel(
            IErrorHandler errorHandler,
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            _errorHandler = errorHandler;

            LoadIntervalsCommand = new AsyncCommand(GetNPCouirerCallIntervalsAsync);
            NPCourierCallIntervalChangedCommand = new DelegateCommand(NPCourierCallIntervalChanged);
        }

        public WarehouseDeliveryEditViewModel()
        {
        }

        #region Commands

        public IAsyncCommand LoadIntervalsCommand { get; }

        public IDelegateCommand NPCourierCallIntervalChangedCommand { get; }

        #endregion

        #region INPC

        public bool IsNovaposhta
        {
            get { return CarryTypes.Where(x => Model?.CarryIds?.Contains(x.Id) == true).Any(x => x.IsNovaposhta()); }
        }

        public ReadOnlyObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public ReadOnlyObservableCollection<Subdivision> Subdivisions
        {
            get { return GetProperty(() => Subdivisions); }
            private set { SetProperty(() => Subdivisions, value); }
        }

        public ReadOnlyObservableCollection<DayOfWeek> DaysOfWeek
        {
            get { return GetProperty(() => DaysOfWeek); }
            private set { SetProperty(() => DaysOfWeek, value); }
        }

        public ReadOnlyObservableCollection<Entity> EntityTypes
        {
            get { return GetProperty(() => EntityTypes); }
            private set { SetProperty(() => EntityTypes, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> NPCourierCallIntervals
        {
            get { return GetProperty(() => NPCourierCallIntervals); }
            private set { SetProperty(() => NPCourierCallIntervals, value); }
        }

        public ComboBoxItem? SelectedNPCourierCallInterval
        {
            get { return GetProperty(() => SelectedNPCourierCallInterval); }
            set { SetProperty(() => SelectedNPCourierCallInterval, value, () => { }); }
        }

        #endregion

        public override void OnDestroy()
        {
            if (Model is not null)
            {
                Model.PropertyChanged -= OnModelPropertyChanged;
            }

            base.OnDestroy();
        }

        protected override string CreatedActionMessage => "создано";

        protected override string EntityName => "Правило";

        protected override string UpdatedActionMessage => "сохранено";

        protected override object CreateEntityMessage(DeliveryDto dto, MessageType messageType)
        {
            return new WarehouseDeliveryMessage(dto, messageType);
        }

        protected override Task<DeliveryDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryDelivery(parameter.WarehouseId, id));
        }

        protected override async Task HandleLoadedAsync()
        {
            parameter = (WarehouseDeliveryEditParameter)Parameter;

            CarryTypes = Dictionaries.GetItems<CarryType>().OrderBy(x => x.Position).ToReadOnlyObservableCollection();

            Subdivisions = Dictionaries.GetItems<Subdivision>().ToReadOnlyObservableCollection();

            DaysOfWeek = Dictionaries.GetItems<DayOfWeek>().ToReadOnlyObservableCollection();

            EntityTypes = Dictionaries.GetItems<Entity>().Where(x => x.Logistic).ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            Model.PropertyChanged += OnModelPropertyChanged;

            RaisePropertyChanged(nameof(IsNovaposhta));

            if (Model.PlanCourierCall || IsNovaposhta)
            {
                await GetNPCouirerCallIntervalsAsync();
            }
        }

        protected override Task<LockResponse<DeliveryDto>> LockEntityAsync(int id)
        {
            throw new NotImplementedException();
        }

        protected override Task<LockResponse<DeliveryDto>> UnlockEntityAsync(int id)
        {
            throw new NotImplementedException();
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание правила";
        }

        protected override void SetEditTitle()
        {
            Title = "Изменение правила";
        }

        protected override Task<Result<DeliveryDto>> UpdateEntityAsync()
        {
            DeliverySaveDto saveDto = Mapper.Map<DeliverySaveDto>(Model);
            UpdateDelivery gatewayRequest = new UpdateDelivery(parameter.WarehouseId, Model.Id, saveDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override Task<Result<DeliveryDto>> CreateEntityAsync()
        {
            DeliverySaveDto saveDto = Mapper.Map<DeliverySaveDto>(Model);
            CreateDelivery gatewayRequest = new CreateDelivery(Model.WarehouseId, saveDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(WarehouseDeliveryViewItem.PlanCourierCall):
                {
                    if (!Model.PlanCourierCall)
                    {
                        Model.EntityTypeIds = new ObservableCollection<int>();
                        Model.PlannedWeight = null;
                    }
                    else
                    {
                        Model.PlannedWeight = 0;
                    }
                    break;
                }

                case nameof(WarehouseDeliveryViewItem.CarryIds):
                {
                    RaisePropertyChanged(nameof(IsNovaposhta));
                    break;
                }
            }
        }

        private async Task GetNPCouirerCallIntervalsAsync()
        {
            DateTime dateTime = DateTime.Today;

            if (!string.IsNullOrEmpty(Model?.DaysOfWeek))
            {
                var dayOfWeek = DayOfWeekHelper.Parse(Model?.DaysOfWeek).FirstOrDefault();

                if (dayOfWeek <= dateTime.DayOfWeek)
                {
                    dateTime = dateTime.AddDays(7);
                }

                dateTime = dateTime.AddDays(dayOfWeek - dateTime.DayOfWeek);
            }

            IReadOnlyCollection<NpCourierCallIntervalDto> intervals = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryNpCourierCallIntervals(Model.WarehouseId, dateTime.Date)),
                "запросе временных интервалов",
                null,
                this,
                true);

            NPCourierCallIntervals = (intervals ?? Enumerable.Empty<NpCourierCallIntervalDto>()).Select((x, i) => new ComboBoxItem(i, $"{x.Start} - {x.End}"))
                .ToReadOnlyObservableCollection();
        }

        private void NPCourierCallIntervalChanged()
        {
            if (SelectedNPCourierCallInterval.HasValue)
            {
                string intervalStr = SelectedNPCourierCallInterval.Value.DisplayValue;
                var times = intervalStr.Split(" - ").Select(TimeOnly.Parse);

                Model.TimeDeliveryFrom = times.First().ToTimeSpan();
                Model.TimeDeliveryTo = times.Last().ToTimeSpan();
            }
        }
    }
}