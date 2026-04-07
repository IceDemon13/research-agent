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
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.HotlineCompetitor;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.HotlineCompetitor;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class HotlineCompetitorViewModel : TelemartDialogViewModelBase
    {
        private int id;

        public HotlineCompetitorViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            private set { SetProperty(() => Name, value); }
        }

        public int? AbcId
        {
            get { return GetProperty(() => AbcId); }
            set { SetProperty(() => AbcId, value); }
        }

        public int? ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            set { SetProperty(() => Contractors, value); }
        }

        public ReadOnlyObservableCollection<AbcType> AbcTypes
        {
            get { return GetProperty(() => AbcTypes); }
            set { SetProperty(() => AbcTypes, value); }
        }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<HotlineCompetitorViewModel> builder)
        {
            builder.Property(x => x.AbcId)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            HotlineCompetitorViewItem item = (HotlineCompetitorViewItem)Parameter;

            id = item.Id;
            Name = item.Name;
            AbcId = item.AbcId;
            ContractorId = item.ContractorId;

            AbcTypes = Dictionaries.GetItems<AbcType>().ToReadOnlyObservableCollection();

            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            Contractors = contractors
                .Where(x => (x.IsCompetitor && !x.IsFolder) || x.Id == id)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            Title = $"Конкурент Hotline №{id}";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                HotlineCompetitorSaveDto saveDto = new HotlineCompetitorSaveDto()
                {
                    Id = id,
                    AbcId = AbcId.Value,
                    ContractorId = ContractorId
                };

                Result<HotlineCompetitorDto> result = await WebClient.ExecuteApiRequestAsync(new UpdateHotlineCompetitor(id, saveDto));

                Messenger.Send(new HotlineCompetitorMessage(result.Data, MessageType.Changed));

                IsOk = true;
                Close();

                MessageFacadeService.ShowNotificationInfo($"Конкурент hotline №{result.Data.Id} успешно изменен");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении конкурента hotline");
                ShowValidationResultView("Ошибки при изменении конкурента hotline", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to update hotline competitor");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении конкурента hotline");
                Logger.LogError(exception, "Error while updating hotline competitor");
            }
        }
    }
}