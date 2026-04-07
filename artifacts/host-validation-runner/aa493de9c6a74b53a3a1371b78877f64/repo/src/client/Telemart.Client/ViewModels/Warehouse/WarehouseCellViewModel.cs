using System;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.WarehouseCell;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse.Cell;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Warehouse
{
    public class WarehouseCellViewModel : TelemartEditorViewModelBase<WarehouseCellDto, WarehouseCellParameter, WarehouseCellViewItem>
    {
        public WarehouseCellViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
        }

        public WarehouseCellViewModel()
        {
        }

        protected override string CreatedActionMessage => "создана";

        protected override string EntityName => "Ячейка";

        protected override string UpdatedActionMessage => "сохранена";

        protected override Task<Result<WarehouseCellDto>> CreateEntityAsync()
        {
            return WebClient.ExecuteApiRequestAsync(new CreateWarehouseCell(EditorParameter.WarehouseId, Model.Name, Model.Active));
        }

        protected override Task<Result<WarehouseCellDto>> UpdateEntityAsync()
        {
            return WebClient.ExecuteApiRequestAsync(new UpdateWarehouseCell(EditorParameter.Id, EditorParameter.WarehouseId, Model.Name, Model.Active));
        }

        protected override Task<WarehouseCellDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryWarehouseCell(EditorParameter.WarehouseId, id));
        }

        protected override Task<LockResponse<WarehouseCellDto>> LockEntityAsync(int id)
        {
            throw new NotImplementedException();
        }

        protected override Task<LockResponse<WarehouseCellDto>> UnlockEntityAsync(int id)
        {
            throw new NotImplementedException();
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание ячейки";
        }

        protected override void SetEditTitle()
        {
            Title = "Редактирование ячейки";
        }
    }
}