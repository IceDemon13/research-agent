using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Complaint;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.Requests.Features.TradeIn;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Complaint;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;

namespace Telemart.Client.ViewModels.Complaint
{
    public class ComplaintsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public ComplaintsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            AddFromOrderCommand = new AsyncCommand(AddFromOrderAsync);
            AddFromServiceRequestCommand = new AsyncCommand(AddFromServiceRequestAsync);
            AddFromTradeInCommand = new AsyncCommand(AddFromTradeInAsync);
            AddCommand = new DelegateCommand(Add);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            EditCommand = new DelegateCommand(Edit, () => SelectedComplaint != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);

            Filter = new ComplaintsFilterViewModel(webClient, dictionaries);

            Complaints = new ObservableRangeCollection<ComplaintViewItem>();

            Messenger.Register<ComplaintMessage>(this, OnComplaintMessage);
        }

        public ComplaintsViewModel()
        {
        }

        #region Commands

        public IAsyncCommand AddFromOrderCommand { get; }

        public IAsyncCommand AddFromServiceRequestCommand { get; }

        public IAsyncCommand AddFromTradeInCommand { get; }

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        #endregion

        #region INPC

        public ComplaintsFilterViewModel Filter { get; }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Types
        {
            get { return GetProperty(() => Types); }
            private set { SetProperty(() => Types, value); }
        }

        public ReadOnlyObservableCollection<Priority> Priorities
        {
            get { return GetProperty(() => Priorities); }
            private set { SetProperty(() => Priorities, value); }
        }

        public ReadOnlyObservableCollection<ComplaintSourceDto> Sources
        {
            get { return GetProperty(() => Sources); }
            private set { SetProperty(() => Sources, value); }
        }

        public ReadOnlyObservableCollection<ComplaintState> States
        {
            get { return GetProperty(() => States); }
            private set { SetProperty(() => States, value); }
        }

        public ObservableRangeCollection<ComplaintViewItem> Complaints
        {
            get { return GetProperty(() => Complaints); }
            set { SetProperty(() => Complaints, value); }
        }

        public ComplaintViewItem SelectedComplaint
        {
            get { return GetProperty(() => SelectedComplaint); }
            set { SetProperty(() => SelectedComplaint, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService NonModalDialogDocumentManagerService => GetService<IDocumentManagerService>("NonModalDialogDocumentManagerService");

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.L:
                        IsSearchPanelClosed = !IsSearchPanelClosed;
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (msg.HotkeyMessageType)
                {
                    case HotkeyMessageType.Refresh:
                        RefreshCommand.Execute(null);
                        handled = true;
                        break;

                    case HotkeyMessageType.Edit:
                        EditCommand.Execute(null);
                        handled = true;
                        break;

                    case HotkeyMessageType.ShowColumnChooser:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            IsSearchPanelClosed = false;

            await Filter.RefreshAsync();
            CancelFilteringCommand.Execute(null);
        }

        private async Task AddFromOrderAsync()
        {
            string documentNumber = GetDocumentNumber("Введите номер заказа", "Номер заказа");

            if (string.IsNullOrEmpty(documentNumber))
            {
                return;
            }

            if (int.TryParse(documentNumber, out int orderId))
            {
                try
                {
                    OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderId));

                    ComplaintCreateParameter parameter = new ComplaintCreateParameter(
                        order.Id,
                        null,
                        null,
                        order.ClientId,
                        order.Products.Select(x => new ComboBoxItem(x.Product.Id, x.Product.Name)).ToList(),
                        order.Fio,
                        order.Phone,
                        order.Phone2,
                        order.Email);

                    NonModalDialogDocumentManagerService.ShowView<ComplaintCreateViewModel>(parameter, this);
                }
                catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    MessageFacadeService.ShowNotificationWarning($"Заказ №{orderId} не найден");
                }
                catch (Exception exception)
                {
                    MessageFacadeService.ShowNotificationWarning(Resources.ErrorDuringDataLoading);
                    Logger.LogError(exception, "Error while creating complaint from order");
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Неверный номер заказа");
            }
        }

        private async Task AddFromServiceRequestAsync()
        {
            string documentNumber = GetDocumentNumber("Введите номер заявки", "Номер сервисной заявки");

            if (string.IsNullOrEmpty(documentNumber))
            {
                return;
            }

            if (int.TryParse(documentNumber, out int serviceRequestId))
            {
                try
                {
                    ServiceRequestDto serviceRequest = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(serviceRequestId));

                    ComplaintCreateParameter parameter = new ComplaintCreateParameter(
                        serviceRequest.OrderId,
                        serviceRequest.Id,
                        null,
                        serviceRequest.ContractorId,
                        new[] { new ComboBoxItem(serviceRequest.ProductId, serviceRequest.ProductName) },
                        serviceRequest.Fio,
                        serviceRequest.Phone,
                        serviceRequest.Phone2,
                        serviceRequest.Email);

                    NonModalDialogDocumentManagerService.ShowView<ComplaintCreateViewModel>(parameter, this);
                }
                catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    MessageFacadeService.ShowNotificationWarning($"Заявка №{serviceRequestId} не найдена");
                }
                catch (Exception exception)
                {
                    MessageFacadeService.ShowNotificationWarning(Resources.ErrorDuringDataLoading);
                    Logger.LogError(exception, "Error while creating complaint from service request");
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Неверный номер заявки");
            }
        }

        private async Task AddFromTradeInAsync()
        {
            string documentNumber = GetDocumentNumber("Введите номер заявки", "Номер сервисной заявки");

            if (string.IsNullOrEmpty(documentNumber))
            {
                return;
            }

            if (int.TryParse(documentNumber, out int serviceRequestId))
            {
                try
                {
                    TradeInDto tradeIn = await WebClient.ExecuteApiRequestAsync(new QueryTradeIn(serviceRequestId));

                    ComplaintCreateParameter parameter = new ComplaintCreateParameter(
                        null,
                        null,
                        tradeIn.Id,
                        null,
                        new[] { new ComboBoxItem(tradeIn.ProductId ?? 0, tradeIn.ProductName) },
                        $"{tradeIn.LastName} {tradeIn.FirstName} {tradeIn.MiddleName}",
                        tradeIn.Phone,
                        null,
                        tradeIn.Email);

                    NonModalDialogDocumentManagerService.ShowView<ComplaintCreateViewModel>(parameter, this);
                }
                catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    MessageFacadeService.ShowNotificationWarning($"Заявка №{serviceRequestId} не найдена");
                }
                catch (Exception exception)
                {
                    MessageFacadeService.ShowNotificationWarning(Resources.ErrorDuringDataLoading);
                    Logger.LogError(exception, "Error while creating complaint from service request");
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Неверный номер заявки");
            }
        }

        private void Add()
        {
            NonModalDialogDocumentManagerService.ShowView<ComplaintCreateViewModel>(ComplaintCreateParameter.Empty, this);
        }

        private string GetDocumentNumber(string caption, string title)
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                caption,
                title,
                @"^\d+$",
                "Номер документа должен быть числом");

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            return fromUserViewModel.IsOk ? fromUserViewModel.Content : null;
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void Edit()
        {
            Messenger.Send(new ComplaintViewMessage(SelectedComplaint.Id));
        }

        private async Task RefreshAsync()
        {
            try
            {
                Priorities = Dictionaries.GetItems<Priority>().ToReadOnlyObservableCollection();
                States = Dictionaries.GetItems<ComplaintState>().ToReadOnlyObservableCollection();

                await Task.WhenAll(RefreshEmployees(), RefreshContractors(), RefreshTypes(), RefreshComplaints(), RefreshSources());

                await Filter.RefreshAsync();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }

            async Task RefreshEmployees()
            {
                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

                Employees = employees
                    .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshContractors()
            {
                List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

                Contractors = contractors
                    .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshTypes()
            {
                List<ComplaintTypeDto> types = await WebClient.ExecuteApiRequestAsync(new QueryComplaintTypes(), true);

                Types = types
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshSources()
            {
                List<ComplaintSourceDto> sourcesDtos = await WebClient.ExecuteApiRequestAsync(new QueryComplaintSources());

                Sources = sourcesDtos
                    .OrderBy(x => x.Name)
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshComplaints()
            {
                List<ComplaintDto> complaints = await WebClient.ExecuteApiRequestAsync(new QueryComplaints(Filter.GetFilteringItem())).GetPagedResultDataAsync();

                Complaints.Clear();

                Complaints.AddRange(complaints.Select(x => Mapper.Map<ComplaintViewItem>(x)));
            }
        }

        private void OnComplaintMessage(ComplaintMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    Complaints.Insert(0, Mapper.Map<ComplaintViewItem>(message.Entity));
                    break;
                case MessageType.Changed:
                    Complaints.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }
        }
    }
}