using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.UsageReason;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class UsageReasonViewModel : TelemartDialogViewModelBase
    {
        public UsageReasonViewModel(IWebClient webClient, IDictionaries dictionaries, IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public string Reason
        {
            get { return GetProperty(() => Reason); }
            set { SetProperty(() => Reason, value); }
        }

        public static void BuildMetadata(MetadataBuilder<UsageReasonViewModel> builder)
        {
            builder.Property(x => x.Reason).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            await base.HandleLoadedAsync();
            Title = "Укажите причину использования";
        }

        protected override Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return Task.CompletedTask;
            }

            UsageReasonParameter param = (UsageReasonParameter)Parameter;

            UsageReasonCreateDto createDto = new UsageReasonCreateDto
            {
                EntityId = param.EntityId,
                FeatureName = param.FeatureName,
                Reason = Reason
            };

            Task<UsageReasonDto> task = WebClient.ExecuteApiRequestAsync(new CreateUsageReason(createDto));
            task.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    Logger.LogError(t.Exception.InnerException, "Error while submitting usage reason");
                }
            });

            IsOk = true;
            Close();

            return Task.CompletedTask;
        }
    }
}