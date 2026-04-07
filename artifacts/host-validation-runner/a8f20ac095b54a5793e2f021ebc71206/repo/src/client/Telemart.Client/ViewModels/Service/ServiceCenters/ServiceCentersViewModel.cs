using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.ServiceCenter;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceCenters
{
    public sealed class ServiceCentersViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private IReadOnlyDictionary<int, ComboBoxItem> employeesDictionary;

        public ServiceCentersViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            AddServiceCenterCommand = new DelegateCommand(AddServiceCenter);
            EditServiceCenterCommand = new DelegateCommand<int?>(EditServiceCenter, x => x.HasValue);
            RefreshCommand = new AsyncCommand(RefreshAsync);

            Messenger.Register<ServiceCenterMessage>(this, OnServiceCenterMessage);
        }

        public ServiceCentersViewModel()
        {
        }

        #region Commands

        public IDelegateCommand AddServiceCenterCommand { get; }

        public IDelegateCommand EditServiceCenterCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        #endregion

        #region INPC

        public ObservableCollection<ServiceCenterViewItem> ServiceCenters
        {
            get { return GetProperty(() => ServiceCenters); }
            set { SetProperty(() => ServiceCenters, value); }
        }

        public ServiceCenterViewItem CurrentServiceCenter
        {
            get { return GetProperty(() => CurrentServiceCenter); }
            set { SetProperty(() => CurrentServiceCenter, value); }
        }

        public ReadOnlyObservableCollection<CityDto> Cities
        {
            get { return GetProperty(() => Cities); }
            set { SetProperty(() => Cities, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        #endregion

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            switch (msg.HotkeyMessageType)
            {
                case HotkeyMessageType.Refresh:
                    RefreshCommand.Execute(null);
                    handled = true;
                    break;
                case HotkeyMessageType.Add:
                    AddServiceCenterCommand.Execute(null);
                    handled = true;
                    break;
                case HotkeyMessageType.Edit:
                    EditServiceCenterCommand.Execute(CurrentServiceCenter?.Id);
                    handled = true;
                    break;
                case HotkeyMessageType.ShowColumnChooser:
                    IsColumnChooserVisible = !IsColumnChooserVisible;
                    handled = true;
                    break;
            }

            return handled;
        }

        protected override Task HandleLoadedAsync()
        {
            RefreshCommand.Execute(null);
            return Task.CompletedTask;
        }

        private void AddServiceCenter()
        {
            Messenger.Send(new ServiceCenterViewMessage(0));
        }

        private void EditServiceCenter(int? serviceCenterId)
        {
            if (serviceCenterId.HasValue)
            {
                Messenger.Send(new ServiceCenterViewMessage(serviceCenterId.Value));
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                await Task.WhenAll(RefreshCitiesAsync(), RefreshEmployeesAsync());

                await RefreshServiceCentersAsync();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while loading service centers");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshServiceCentersAsync()
        {
            PagedResult<ServiceCenterDto> serviceCenters = await WebClient.ExecuteApiRequestAsync(new QueryServiceCenters());
            SetServiceCenters(serviceCenters.Data);
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();
            Cities = cities.OrderBy(x => x.Position).ThenBy(x => x.Name).ToReadOnlyObservableCollection();
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
            employeesDictionary = employees.ToDictionary(x => x.Id, y => new ComboBoxItem(y.Id, y.Name, y.Active));
        }

        private void OnServiceCenterMessage(ServiceCenterMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    {
                        ServiceCenterViewItem serviceCenterViewItem = Map(message.Entity, new ServiceCenterViewItem());

                        ServiceCenters.Insert(0, serviceCenterViewItem);
                        SetServiceCenters(ServiceCenters);
                        CurrentServiceCenter = serviceCenterViewItem;
                        break;
                    }

                case MessageType.Changed:
                    {
                        ServiceCenters.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Map(message.Entity, viewItem));
                        SetServiceCenters(ServiceCenters);
                        break;
                    }

                default:
                    {
                        Debug.WriteLine($"Unknown serviceCenter message type {message.MessageType}");
                        break;
                    }
            }
        }

        private void SetServiceCenters(IReadOnlyCollection<ServiceCenterDto> serviceCenterDtos)
        {
            SetServiceCenters(serviceCenterDtos.Select(x => Map(x, new ServiceCenterViewItem())));
        }

        private void SetServiceCenters(IEnumerable<ServiceCenterViewItem> viewItems)
        {
            ServiceCenters = viewItems.OrderBy(x => x.Type.Name).ThenBy(x => x.Name).ToObservableCollection();
        }

        private ServiceCenterViewItem Map(ServiceCenterDto dto, ServiceCenterViewItem viewItem)
        {
            ServiceCenterViewItem mappedItem = Mapper.Map(dto, viewItem);

            mappedItem.Employee = employeesDictionary.GetValueOrDefault(dto.EmployeeId);

            return mappedItem;
        }
    }
}