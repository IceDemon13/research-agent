using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.WorkSchedule
{
    public sealed class WorkScheduleChangeTypeViewModel : TelemartDialogViewModelBase
    {
        public WorkScheduleChangeTypeViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ReadOnlyObservableCollection<WorkScheduleType> WorkScheduleTypes
        {
            get { return GetProperty(() => WorkScheduleTypes); }
            set { SetProperty(() => WorkScheduleTypes, value); }
        }

        public WorkScheduleType WorkScheduleType
        {
            get { return GetProperty(() => WorkScheduleType); }
            set { SetProperty(() => WorkScheduleType, value); }
        }

        protected override Task HandleLoadedAsync()
        {
            WorkScheduleType[] types = (WorkScheduleType[])Parameter;

            WorkScheduleTypes = types.ToReadOnlyObservableCollection();

            Title = "Выберите тип";

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            if (WorkScheduleType == null)
            {
                MessageFacadeService.ShowNotificationWarning("Не выбран тип");

                return Task.CompletedTask;
            }

            CloseOk();

            return Task.CompletedTask;
        }
    }
}