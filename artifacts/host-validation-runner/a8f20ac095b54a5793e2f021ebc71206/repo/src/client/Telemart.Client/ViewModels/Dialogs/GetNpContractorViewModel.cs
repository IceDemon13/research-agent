using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Dialogs
{
    public class GetNpContractorViewModel : TelemartDialogViewModelBase
    {
        public GetNpContractorViewModel(
           IWebClient webClient,
           IDictionaries dictionaries,
           IMessageFacadeService messageFacadeService)
           : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public GetNpContractorViewModel()
        {
        }

        public string SelectedNpContractorRef
        {
            get { return GetProperty(() => SelectedNpContractorRef); }
            set { SetProperty(() => SelectedNpContractorRef, value); }
        }

        public ReadOnlyObservableCollection<NpContractorDto> NpContractors
        {
            get { return GetProperty(() => NpContractors); }
            set { SetProperty(() => NpContractors, value); }
        }

        public static void BuildMetadata(MetadataBuilder<GetNpContractorViewModel> builder)
        {
            builder.Property(x => x.SelectedNpContractorRef).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            GetNpContractorParameter parameter = (GetNpContractorParameter)Parameter;

            List<NpContractorDto> npContractors = await WebClient.ExecuteApiRequestAsync(new QueryNpContractors());

            NpContractors = npContractors
                .Where(x => x.Active)
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();

            SelectedNpContractorRef = parameter.NpContractorRef;

            Title = "Выберите контрагента";
        }

        protected override Task HandleOkAsync()
        {
            if (!IDataErrorInfoHelper.HasErrors(this))
            {
                IsOk = true;
                Close();
            }

            return Task.CompletedTask;
        }
    }
}
