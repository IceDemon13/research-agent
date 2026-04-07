using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Backlog
{
    public sealed class BacklogTaskJiraViewModel : TelemartDialogViewModelBase
    {
        private BacklogTaskJiraParameter _parameter;

        public BacklogTaskJiraViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public string JiraId
        {
            get { return GetProperty(() => JiraId); }
            set { SetProperty(() => JiraId, value); }
        }

        public int? Estimate
        {
            get { return GetProperty(() => Estimate); }
            set { SetProperty(() => Estimate, value); }
        }

        public static void BuildMetadata(MetadataBuilder<BacklogTaskJiraViewModel> builder)
        {
            builder.Property(x => x.JiraId)
                .MaxLength(10, () => "Длина поля должна быть не больше 10 символов")
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Estimate)
                .MatchesRule(x => x is null || (x > 0 && x < 1000), () => "Значение должно быть в диапазоне 1..999");
        }

        protected override Task HandleLoadedAsync()
        {
            _parameter = (BacklogTaskJiraParameter)Parameter;

            Estimate = _parameter.BacklogTask.Estimate;
            JiraId = _parameter.BacklogTask.JiraId;

            Title = "Создание задачи в Jira";

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }
    }
}