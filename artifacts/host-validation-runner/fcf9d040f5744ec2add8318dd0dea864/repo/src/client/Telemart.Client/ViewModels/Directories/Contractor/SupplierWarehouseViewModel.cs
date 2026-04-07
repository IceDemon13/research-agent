using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Contractor.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.Contractor
{
    public sealed class SupplierWarehouseViewModel : TelemartDialogViewModelBase
    {
        public SupplierWarehouseViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            Title = "Склад";
        }

        public SupplierWarehouseViewModel()
        {
        }

        #region Dependency properties

        public List<CityDto> Cities
        {
            get { return GetProperty(() => Cities); }
            set { SetProperty(() => Cities, value); }
        }

        public SupplierWarehouseViewItem Model
        {
            get { return GetProperty(() => Model); }
            set { SetProperty(() => Model, value); }
        }

        #endregion

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            if (Parameter is SupplierWarehouseViewItem viewItem)
            {
                Model = viewItem;
            }
            else
            {
                SupplierWarehouseDto supplierWarehouse = await WebClient.ExecuteApiRequestAsync(new QuerySupplierWarehouse((int)Parameter));

                Model = Mapper.Map<SupplierWarehouseViewItem>(supplierWarehouse);
            }

            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();
            Cities = cities.OrderBy(x => x.Position).ThenBy(x => x.Name).ToList();
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                SupplierWarehouseDto saveDto = Mapper.Map<SupplierWarehouseDto>(Model);

                Task<SupplierWarehouseDto> saveTask = Model.Id > 0
                    ? WebClient.ExecuteApiRequestAsync(new UpdateContractorWarehouse(Model.SupplierId, saveDto))
                    : WebClient.ExecuteApiRequestAsync(new CreateContractorWarehouse(Model.SupplierId, saveDto));

                SupplierWarehouseDto objFromServer = await saveTask;
                Mapper.Map(objFromServer, Model);
                MessageFacadeService.ShowNotificationInfo("Склад успешно сохранен");
                IsOk = true;
                Close();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to save contractor warehouse");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении склада");
            }
        }
    }
}