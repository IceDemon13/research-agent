using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.TransferObjects.Warehouse.Route;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Warehouse
{
    public sealed class WarehouseRouteCreateViewModel : TelemartDialogViewModelBase
    {
        public WarehouseRouteCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public WarehouseRouteCreateViewModel()
        {
        }

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public int? WarehouseFromId
        {
            get { return GetProperty(() => WarehouseFromId); }
            set { SetProperty(() => WarehouseFromId, value); }
        }

        public int? WarehouseToId
        {
            get { return GetProperty(() => WarehouseToId); }
            set { SetProperty(() => WarehouseToId, value); }
        }

        public bool OpenWithWarehouseFrom
        {
            get { return GetProperty(() => OpenWithWarehouseFrom); }
            set { SetProperty(() => OpenWithWarehouseFrom, value); }
        }

        public int Weight
        {
            get { return GetProperty(() => Weight); }
            set { SetProperty(() => Weight, value); }
        }

        #endregion INPC

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<WarehouseRouteCreateViewModel> builder)
        {
            builder.Property(x => x.WarehouseFromId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.WarehouseToId).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            WarehouseRouteCreateParameter parameter = (WarehouseRouteCreateParameter)Parameter;

            OpenWithWarehouseFrom = parameter != null;

            if (parameter != null)
            {
                WarehouseFromId = parameter.WarehouseFromId;
            }

            Weight = 100;

            await RefreshWarehouses();

            Title = "Создание маршрута";

            async Task RefreshWarehouses()
            {
                List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

                Warehouses = warehouses
                    .Where(x => x.Active == 1 && x.TypeId != WarehouseKind.Virtual.Id)
                    .OrderByDescending(x => x.Position)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }
        }

        protected override async Task HandleOkAsync()
        {
            const string GeneralErrorMessage = "Ошибка при создании маршрута";

            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            WarehouseRouteCreateDto createDto = MapToDto(this);

            try
            {
                Result<WarehouseRouteSimpleDto> result =
                    await WebClient.ExecuteApiRequestAsync(new CreateWarehouseRoute(WarehouseFromId.Value, createDto));

                Messenger.Send(new WarehouseRouteMessage(result.Data, MessageType.Added));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(GeneralErrorMessage);
                ShowValidationResultView("Ошибки при создании маршрута", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create warehouse route");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(GeneralErrorMessage);
                Logger.LogError(exception, "Error while creating warehouse route");
            }
        }

        private WarehouseRouteCreateDto MapToDto(WarehouseRouteCreateViewModel source)
        {
            WarehouseRouteCreateDto target = new WarehouseRouteCreateDto
            {
                WarehouseFromId = source.WarehouseFromId.Value,
                WarehouseToId = source.WarehouseToId.Value,
                Weight = Weight
            };

            return target;
        }
    }
}