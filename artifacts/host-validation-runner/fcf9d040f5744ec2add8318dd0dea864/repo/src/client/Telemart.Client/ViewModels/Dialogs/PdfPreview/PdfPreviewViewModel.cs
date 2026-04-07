using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Dialogs.PdfPreview
{
    public class PdfPreviewViewModel : TelemartDialogViewModelBase
    {
        public PdfPreviewViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public override int MinWidth => 600;

        public override int Width => 800;

        public override int MaxWidth => 1920;

        public override int MinHeight => 180;

        public override int Height => 600;

        public override int MaxHeight => 1080;

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            return Task.CompletedTask;
        }

        public object Source
        {
            get { return GetProperty(() => Source); }
            private set { SetProperty(() => Source, value); }
        }

        protected override void OnParameterChanged(object parameter)
        {
            PdfPreviewParameter previewParameter = (PdfPreviewParameter)parameter;

            Source = previewParameter.Source;
        }
    }
}