using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class ChooseAbcClassViewModel : TelemartDialogViewModelBase
    {
        public ChooseAbcClassViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ChooseAbcClassViewModel()
        {
        }

        #region INPC

        public ReadOnlyObservableCollection<AbcType> AbcTypes
        {
            get { return GetProperty(() => AbcTypes); }
            set { SetProperty(() => AbcTypes, value); }
        }

        public AbcType ResultType
        {
            get { return GetProperty(() => ResultType); }
            set { SetProperty(() => ResultType, value); }
        }

        #endregion

        public static void BuildMetadata(MetadataBuilder<ChooseAbcClassViewModel> builder)
        {
            builder.Property(x => x.ResultType).Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            if (Parameter != null)
            {
                ResultType = (AbcType)Parameter;
            }

            AbcTypes = Dictionaries.GetItems<AbcType>().ToReadOnlyObservableCollection();

            Title = "Выберите класс";

            return base.HandleLoadedAsync();
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
