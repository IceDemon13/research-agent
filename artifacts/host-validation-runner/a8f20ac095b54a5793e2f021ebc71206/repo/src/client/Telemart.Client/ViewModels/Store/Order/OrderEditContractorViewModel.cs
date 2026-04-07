using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.Contractor;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderEditContractorViewModel : TelemartDialogViewModelBase
    {
        private OrderEditContractorParameter data;

        public OrderEditContractorViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            Messenger = messenger;
        }

        public OrderEditContractorViewModel()
        {
        }

        #region INPC

        public ContractorViewItem CurrentContractor
        {
            get { return GetProperty(() => CurrentContractor); }
            set { SetProperty(() => CurrentContractor, value); }
        }

        public ContractorViewItem SelectedContractor
        {
            get { return GetProperty(() => SelectedContractor); }
            set { SetProperty(() => SelectedContractor, value); }
        }

        public ObservableCollection<ContractorViewItem> AllContractors
        {
            get { return GetProperty(() => AllContractors); }
            private set { SetProperty(() => AllContractors, value); }
        }

        public ObservableCollection<ContractorViewItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        #endregion

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<OrderEditContractorViewModel> builder)
        {
            builder.Property(x => x.SelectedContractor).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            data = (OrderEditContractorParameter)Parameter;

            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            AllContractors = contractors.Select(x => Mapper.Map<ContractorViewItem>(x)).ToObservableCollection();

            CurrentContractor = AllContractors.FirstOrDefault(x => x.Id == data.ClientId);

            Contractors = AllContractors
                .Where(x => !x.IsFolder
                    && x.Active
                    && x.CurrencyPermissions?.Any(z => z.CurrencyId == Currency.UahId && z.Sale) == true
                    && (CurrentContractor == null
                        || (x.Id != CurrentContractor.Id && x.Subdivision.Id == CurrentContractor.Subdivision.Id)))
                .OrderBy(x => x.Subdivision.Id)
                .ThenBy(x => x.Name)
                .ToObservableCollection();

            Title = "Изменение контрагента";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this, 1))
            {
                return;
            }

            try
            {
                Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(new UpdateOrderContractor(data.OrderId, SelectedContractor.Id));

                MessageFacadeService.ShowNotificationInfo($"Контрагент для заказа №{result.Data.Id} успешно сохранен");
                Messenger.Send(new OrderMessage(result.Data, MessageType.Changed));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки при изменении контрагента заказа", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при изменении контрагента заказа", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while saving orders client");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
            }
        }
    }
}