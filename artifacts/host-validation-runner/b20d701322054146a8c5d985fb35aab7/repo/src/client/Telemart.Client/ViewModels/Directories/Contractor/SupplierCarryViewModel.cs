using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor.Carry;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Contractor
{
    public sealed class SupplierCarryViewModel : TelemartDialogViewModelBase
    {
        private SupplierCarryViewItem model;
        private ContractorViewModel contractorViewModel;

        public SupplierCarryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            Title = "Поставка";
        }

        public SupplierCarryViewModel()
        {
        }

        #region Dependency properties

        public ObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            set { SetProperty(() => CarryTypes, value); }
        }

        public SupplierCarryViewItem Model
        {
            get { return GetProperty(() => Model); }
            set { SetProperty(() => Model, value); }
        }

        public ObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public List<CityDto> Cities
        {
            get { return GetProperty(() => Cities); }
            set { SetProperty(() => Cities, value); }
        }

        public List<SupplierWarehouseViewItem> SupplierWarehouses
        {
            get { return GetProperty(() => SupplierWarehouses); }
            set { SetProperty(() => SupplierWarehouses, value); }
        }

        #endregion

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            CarryTypes = Dictionaries.GetItems<CarryType>().ToObservableCollection();
            await Task.WhenAll(RefreshWarehousesAsync(), RefreshCitiesAsync());
            Model = model;
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                SupplierCarrySaveDto saveDto = Mapper.Map<SupplierCarrySaveDto>(Model);

                Task<Result<SupplierCarryDto>> saveTask = Model.Id > 0
                    ? WebClient.ExecuteApiRequestAsync(new UpdateContractorCarry(contractorViewModel.Model.Id, saveDto))
                    : WebClient.ExecuteApiRequestAsync(new CreateContractorCarry(contractorViewModel.Model.Id, saveDto));

                Result<SupplierCarryDto> result = await saveTask;
                Mapper.Map(result.Data, Model);
                MessageFacadeService.ShowNotificationInfo("Доставка успешно сохранена");
                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении доставки");
                ShowValidationResultView("Ошибки при сохранении доставки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to save contractor carry");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении доставки");
                Logger.LogError(exception, "Error while saving contractor carry");
            }
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (IsInDesignMode)
            {
                return;
            }

            model = (SupplierCarryViewItem)parameter;
        }

        protected override void OnParentViewModelChanged(object parentViewModel)
        {
            if (IsInDesignMode)
            {
                return;
            }

            contractorViewModel = (ContractorViewModel)parentViewModel;
            SupplierWarehouses = contractorViewModel.SupplierWarehouses.ToList();
        }

        private async Task RefreshCitiesAsync()
        {
            Cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
            Warehouses = new ObservableCollection<WarehouseDto>(warehouses.Where(x => x.TypeId == WarehouseKind.Main.Id || x.TypeId == WarehouseKind.Pickup.Id || x.TypeId == WarehouseKind.ShowCase.Id || x.TypeId == WarehouseKind.Assembly.Id).OrderByDescending(x => x.Position));
        }
    }
}