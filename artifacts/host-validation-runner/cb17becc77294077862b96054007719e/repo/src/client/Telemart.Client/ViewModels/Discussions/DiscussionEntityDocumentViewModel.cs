using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Discussions
{
    public sealed class DiscussionEntityDocumentViewModel : TelemartDialogViewModelBase
    {
        public DiscussionEntityDocumentViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public int? SelectedEntityId
        {
            get { return GetProperty(() => SelectedEntityId); }
            set { SetProperty(() => SelectedEntityId, value); }
        }

        public int? DocumentId
        {
            get { return GetProperty(() => DocumentId); }
            set { SetProperty(() => DocumentId, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Entities
        {
            get { return GetProperty(() => Entities); }
            private set { SetProperty(() => Entities, value); }
        }


        public static void BuildMetadata(MetadataBuilder<DiscussionEntityDocumentViewModel> builder)
        {
            builder.Property(x => x.SelectedEntityId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DocumentId).MatchesRule(x => x > 0, () => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            Entities = Dictionaries
                .GetItems<Entity>()
                .Select(x => new ComboBoxItem(x.Id, x.DisplayName))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            Title = "Прикрепление документа в обсуждение";

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }
    }
}