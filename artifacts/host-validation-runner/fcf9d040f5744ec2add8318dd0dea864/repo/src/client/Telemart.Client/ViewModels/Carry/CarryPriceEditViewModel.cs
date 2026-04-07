using System;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Carry;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Carry
{
    public sealed class CarryPriceEditViewModel
        : TelemartEditorViewModelBase<CarryPriceDto, CarryPriceEditParameter, CarryPriceViewItem>
    {
        private CarryPriceEditParameter parameter;

        public CarryPriceEditViewModel(
           IWebClient webClient,
           IDictionaries dictionaries,
           IMessageFacadeService messageFacadeService,
           IMapper mapper,
           IMessenger messenger)
           : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
        }

        public CarryPriceEditViewModel()
        {
        }

        protected override string CreatedActionMessage => "создано";

        protected override string EntityName => "Правило";

        protected override string UpdatedActionMessage => "сохранено";

        protected override Task<Result<CarryPriceDto>> CreateEntityAsync()
        {
            CarryPriceDto saveDto = MapViewItem(Model);
            CreateCarryPrice gatewayRequest = new CreateCarryPrice(Model.Id, saveDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override Task HandleLoadedAsync()
        {
            parameter = (CarryPriceEditParameter)Parameter;
            return base.HandleLoadedAsync();
        }

        protected override object CreateEntityMessage(CarryPriceDto dto, MessageType messageType)
        {
            return new CarryPriceMessage(dto, messageType);
        }

        protected override Task<CarryPriceDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryCarryPrice(parameter.CarryId, id));
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание правила";
        }

        protected override void SetEditTitle()
        {
            Title = "Изменение правила";
        }

        protected override Task<Result<CarryPriceDto>> UpdateEntityAsync()
        {
            CarryPriceDto saveDto = MapViewItem(Model);
            UpdateCarryPrice gatewayRequest = new UpdateCarryPrice(parameter.CarryId, Model.Id, saveDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override Task<LockResponse<CarryPriceDto>> LockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override Task<LockResponse<CarryPriceDto>> UnlockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        private CarryPriceDto MapViewItem(CarryPriceViewItem viewItem)
        {
            CarryPriceDto saveDto = new CarryPriceDto
            {
                Id = viewItem.Id,
                CarryId = parameter.CarryId,
                Cost = viewItem.Cost,
                WeightFrom = viewItem.WeightFrom,
                WeightTo = viewItem.WeightTo,
                Active = viewItem.Active
            };

            return saveDto;
        }
    }
}