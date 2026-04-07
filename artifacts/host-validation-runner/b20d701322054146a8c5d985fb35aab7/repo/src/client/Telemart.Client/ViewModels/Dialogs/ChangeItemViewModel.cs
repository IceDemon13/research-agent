using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Dialogs
{
    public sealed class ChangeItemViewModel : TelemartDialogViewModelBase
    {
        public ChangeItemViewModel(
           IWebClient webClient,
           IDictionaries dictionaries,
           IMessageFacadeService messageFacadeService)
           : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ChangeItemViewModel()
        {
        }

        protected override Task HandleLoadedAsync()
        {
            ChangeItemParameter parameter = (ChangeItemParameter)Parameter;
            Title = parameter.Title;
            OldItem = parameter.OldItem;
            Items = parameter.Items;

            return Task.CompletedTask;
        }

        public ComboBoxItem? NewItem
        {
            get { return GetProperty(() => NewItem); }
            set { SetProperty(() => NewItem, value); }
        }

        public string OldItem
        {
            get { return GetProperty(() => OldItem); }
            set { SetProperty(() => OldItem, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Items
        {
            get { return GetProperty(() => Items); }
            set { SetProperty(() => Items, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ChangeItemViewModel> builder)
        {
            builder.Property(x => x.NewItem).Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleOkAsync()
        {
            if (!IDataErrorInfoHelper.HasErrors(this))
            {
                if (!string.IsNullOrEmpty(OldItem) && OldItem == NewItem?.DisplayValue)
                {
                    MessageFacadeService.ShowNotificationWarning("Значение не изменилось");
                }
                else
                {
                    IsOk = true;
                }

                Close();
            }

            return Task.CompletedTask;
        }
    }
}
