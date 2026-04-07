using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.LegalEntity;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Tools.ChangeLegalEntity
{
    public sealed class ChangeLegalEntityViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;

        public ChangeLegalEntityViewModel(
            IWebClient webClient,
            IErrorHandler errorHandler,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
        }

        public int? DocumentId
        {
            get { return GetProperty(() => DocumentId); }
            set { SetProperty(() => DocumentId, value); }
        }

        public int? SelectedEntityId
        {
            get { return GetProperty(() => SelectedEntityId); }
            set { SetProperty(() => SelectedEntityId, value); }
        }

        public int? SelectedLegalEntityId
        {
            get { return GetProperty(() => SelectedLegalEntityId); }
            set { SetProperty(() => SelectedLegalEntityId, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Entities
        {
            get { return GetProperty(() => Entities); }
            private set { SetProperty(() => Entities, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> LegalEntities
        {
            get { return GetProperty(() => LegalEntities); }
            private set { SetProperty(() => LegalEntities, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ChangeLegalEntityViewModel> builder)
        {
            builder.Property(x => x.SelectedEntityId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DocumentId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedLegalEntityId).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            Entities = Dictionaries
                .GetItems<Entity>()
                .Where(x => x.Id is Entity.OrderId or Entity.RefundId)
                .Select(x => new ComboBoxItem(x.Id, x.DisplayName))
                .ToReadOnlyObservableCollection();

            List<LegalEntityDto> legalEntities = await WebClient.ExecuteApiRequestAsync(new QueryLegalEntities(), true);

            LegalEntities = legalEntities
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            SelectedEntityId = Entities.First(x => x.Id == Entity.OrderId).Id;

            Title = "Изменения юр. лица";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            ChangeLegalEntityDto dto = new ChangeLegalEntityDto()
            {
                EntityId = SelectedEntityId!.Value,
                LegalEntityId = SelectedLegalEntityId!.Value,
                DocumentId = DocumentId!.Value
            };

            Result<object> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new Data.Requests.Features.Tools.ChangeLegalEntity(dto)),
                "изменении юр. лица",
                "Юр. лицо изменено",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                CloseOk();
            }
        }
    }
}