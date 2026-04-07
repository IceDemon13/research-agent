using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Movement;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public sealed class EditMovementLogisticsViewModel : TelemartDialogViewModelBase
    {
        private int movementId;

        public EditMovementLogisticsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public EditMovementLogisticsViewModel()
        {
        }

        #region INPC

        public CarryType SelectedCarryType
        {
            get { return GetProperty(() => SelectedCarryType); }
            set { SetProperty(() => SelectedCarryType, value, () => { RaisePropertiesChanged(nameof(TrackNumber), nameof(NeedTrackNumber)); }); }
        }

        public string TrackNumber
        {
            get { return GetProperty(() => TrackNumber); }
            set { SetProperty(() => TrackNumber, value); }
        }

        public ReadOnlyObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public bool NeedTrackNumber => SelectedCarryType != null && !string.IsNullOrWhiteSpace(SelectedCarryType.TtnRegex);

        #endregion

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<EditMovementLogisticsViewModel> builder)
        {
            builder.Property(x => x.SelectedCarryType).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.TrackNumber)
                .MatchesInstanceRule(
                    (x, y) => string.IsNullOrWhiteSpace(y.SelectedCarryType?.TtnRegex) || (x != null && Regex.IsMatch(x, y.SelectedCarryType.TtnRegex)),
                    () => "Введите корректно номер ТТН");
        }

        protected override async Task HandleLoadedAsync()
        {
            movementId = (int)Parameter;

            MovementDto movement = await WebClient.ExecuteApiRequestAsync(new QueryMovement(movementId));

            CarryTypes = Dictionaries.GetItems<CarryType>()
                .Where(x => x.UseInMovements || x.Id == movement.CarryId)
                .OrderBy(x => x.Position)
                .ToReadOnlyObservableCollection();

            SelectedCarryType = CarryTypes.FirstOrDefault(x => x.Id == movement.CarryId);
            TrackNumber = movement.TrackNumber;

            Title = $"Перемещение №{movementId}";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                UpdateMovementLogistics request = new UpdateMovementLogistics(
                    movementId,
                    SelectedCarryType.Id,
                    NeedTrackNumber ? TrackNumber : null);

                Result<MovementDto> result = await WebClient.ExecuteApiRequestAsync(request);

                MessageFacadeService.ShowNotificationInfo("Логистика успешно обновлена");

                Messenger.Send(new MovementMessage(result.Data, MessageType.Changed));

                IsOk = true;
                Close();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to update movement logistics. MovementId: {MovementId}", movementId);
                MessageFacadeService.ShowNotificationError("Ошибка при обновлении логистики перемещения");
            }
        }
    }
}