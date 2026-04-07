using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Asterisk;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Asterisk;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Call
{
    public sealed class ChangeCallStateViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;

        private IReadOnlyCollection<AsteriskPresenceDto> _asteriskPresenceStatuses;
        private IReadOnlyCollection<AsteriskDndDto> _asteriskDndStatuses;

        public ChangeCallStateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
        }

        public ReadOnlyObservableCollection<AsteriskStatusDto> AsteriskStatuses
        {
            get { return GetProperty(() => AsteriskStatuses); }
            set { SetProperty(() => AsteriskStatuses, value); }
        }

        public string CurrentAsteriskStatus
        {
            get { return GetProperty(() => CurrentAsteriskStatus); }
            set { SetProperty(() => CurrentAsteriskStatus, value); }
        }

        public AsteriskStatusDto SelectedAsteriskStatus
        {
            get { return GetProperty(() => SelectedAsteriskStatus); }
            set { SetProperty(() => SelectedAsteriskStatus, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ChangeCallStateViewModel> builder)
        {
            builder.Property(x => x.SelectedAsteriskStatus).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            await LoadAsteriskStatusesAsync();

            AsteriskEmployeeStatusDto asteriskEmployeeStatusDto = await WebClient.ExecuteCallApiRequestAsync(new QueryAsteriskStatusByEmployee(WebClient.AuthenticatedEmployee.Id));

            CurrentAsteriskStatus = asteriskEmployeeStatusDto != null ? MapStatus(asteriskEmployeeStatusDto.Presence, asteriskEmployeeStatusDto.Dnd) : string.Empty;

            Title = "Изменить свой статус";
        }

        private async Task LoadAsteriskStatusesAsync()
        {
            _asteriskPresenceStatuses = await WebClient.ExecuteCallApiRequestAsync(new QueryAsteriskPresenceStatuses());
            _asteriskDndStatuses = await WebClient.ExecuteCallApiRequestAsync(new QueryAsteriskDndStatuses());

            List<AsteriskStatusDto> asteriskStatuses = await WebClient.ExecuteCallApiRequestAsync(new QueryAsteriskStatuses());
            AsteriskStatuses = asteriskStatuses.ToReadOnlyObservableCollection();
        }

        protected override async Task HandleOkAsync()
        {
            Result<object> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteCallApiRequestAsync(new SetAsteriskEmployeeStatus(SelectedAsteriskStatus.Id, WebClient.AuthenticatedEmployee.Id)),
                "изменения статуса",
                "Статус изменен",
                this,
                true,
                showNotification: true);

            if (result.IsSuccess)
            {
                MainWindowViewModel.ChangeUsersStatusAsterisk = true;
                CloseOk();
            }
        }

        private string MapStatus(string presense, string dnd)
        {
            return $"{_asteriskDndStatuses.FirstOrDefault(x => x.Name == dnd)?.DisplayText ?? dnd} ({_asteriskPresenceStatuses.FirstOrDefault(x => x.Name == presense)?.DisplayText ?? presense})";
        }
    }
}