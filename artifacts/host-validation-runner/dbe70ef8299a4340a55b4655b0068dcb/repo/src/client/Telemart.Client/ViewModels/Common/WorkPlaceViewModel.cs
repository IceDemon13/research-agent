using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Equipment;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.WorkPlace;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common
{
    public class WorkPlaceViewModel : TelemartDialogViewModelBase
    {
        private WorkPlaceDto workPlace;

        private WorkPlaceDeviceType originalWorkPlaceDeviceType;
        private WorkPlaceType originalWorkPlaceType;

        public WorkPlaceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMessenger messenger,
            IEquipmentSettingsStore equipmentSettingsStore)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;
            Messenger = messenger;
            EquipmentSettingsStore = equipmentSettingsStore;
        }

        public WorkPlaceType SelectedWorkPlaceType
        {
            get { return GetProperty(() => SelectedWorkPlaceType); }
            set { SetProperty(() => SelectedWorkPlaceType, value); }
        }

        public WorkPlaceDeviceType SelectedWorkPlaceDeviceType
        {
            get { return GetProperty(() => SelectedWorkPlaceDeviceType); }
            set { SetProperty(() => SelectedWorkPlaceDeviceType, value); }
        }

        public ReadOnlyObservableCollection<WorkPlaceType> WorkPlaceTypes
        {
            get { return GetProperty(() => WorkPlaceTypes); }
            private set { SetProperty(() => WorkPlaceTypes, value); }
        }

        public ReadOnlyObservableCollection<WorkPlaceDeviceType> WorkPlaceDeviceTypes
        {
            get { return GetProperty(() => WorkPlaceDeviceTypes); }
            private set { SetProperty(() => WorkPlaceDeviceTypes, value); }
        }

        public bool CancelCommandVisible
        {
            get { return GetProperty(() => CancelCommandVisible); }
            private set { SetProperty(() => CancelCommandVisible, value); }
        }

        private IErrorHandler ErrorHandler { get; }

        private IMessenger Messenger { get; }

        private IEquipmentSettingsStore EquipmentSettingsStore { get; }

        public static void BuildMetadata(MetadataBuilder<WorkPlaceViewModel> builder)
        {
            builder.Property(x => x.SelectedWorkPlaceType)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedWorkPlaceDeviceType)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            workPlace = (WorkPlaceDto)Parameter;

            if (workPlace != null)
            {
                SelectedWorkPlaceType = Dictionaries.GetItemById<WorkPlaceType>(workPlace.TypeId);
                SelectedWorkPlaceDeviceType = Dictionaries.GetItemById<WorkPlaceDeviceType>(workPlace.DeviceTypeId);

                originalWorkPlaceType = SelectedWorkPlaceType;
                originalWorkPlaceDeviceType = SelectedWorkPlaceDeviceType;
            }

            if (WebClient.WorkPlaceId.HasValue)
            {
                CancelCommandVisible = true;
            }

            WorkPlaceTypes = Dictionaries.GetItems<WorkPlaceType>().ToReadOnlyObservableCollection();
            WorkPlaceDeviceTypes = Dictionaries.GetItems<WorkPlaceDeviceType>().ToReadOnlyObservableCollection();

            base.HandleLoadedAsync();

            Title = "Выбор рабочего места";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            if (originalWorkPlaceType?.Id == SelectedWorkPlaceType.Id && originalWorkPlaceDeviceType?.Id == SelectedWorkPlaceDeviceType.Id && WebClient.WorkPlaceId.HasValue)
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");

                IsOk = true;
                Close();

                return;
            }

            EquipmentSettingsInfo equipmentSettings = await EquipmentSettingsStore.LoadAsync();

            Guid uniqueDeviceGuid = equipmentSettings.UniqueDeviceGuid.Value;

            WorkPlaceCreateDto createDto = new WorkPlaceCreateDto(
                SelectedWorkPlaceType.Id,
                SelectedWorkPlaceDeviceType.Id,
                uniqueDeviceGuid.ToString());

            (await ErrorHandler.HandleErrorsAsync(x => WebClient.ExecuteApiRequestAsync(new CreateWorkPlace(createDto)), "сохранении рабочего места", "Рабочее место сохранено", this, true))
                .IfNotNull(x =>
                {
                    WebClient.SetWorkPlaceId(x.Data.Id);

                    Messenger.Send(new UpdateWorkPlaceMessage(x.Data));

                    IsOk = true;
                    Close();
                });
        }
    }
}