using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class SelectItemViewModel : TelemartDialogViewModelBase
    {
        public SelectItemViewModel(IWebClient webClient, IDictionaries dictionaries, IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ComboBoxItem? SelectedItem
        {
            get { return GetProperty(() => SelectedItem); }
            set { SetProperty(() => SelectedItem, value); }
        }

        public string ItemName
        {
            get { return GetProperty(() => ItemName); }
            set { SetProperty(() => ItemName, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Items
        {
            get { return GetProperty(() => Items); }
            set { SetProperty(() => Items, value); }
        }

        public static void BuildMetadata(MetadataBuilder<SelectItemViewModel> builder)
        {
            builder.Property(x => x.SelectedItem)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            SelectItemParameter parameter = (SelectItemParameter)Parameter;
            Items = parameter.Items.ToReadOnlyObservableCollection();
            SelectedItem = parameter.SelectedItem;
            Title = parameter.Title;
            ItemName = parameter.ItemName;

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();

            return Task.CompletedTask;
        }
    }
}