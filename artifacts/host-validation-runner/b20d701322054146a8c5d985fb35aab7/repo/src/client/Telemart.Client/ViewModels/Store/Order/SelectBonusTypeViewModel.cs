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

namespace Telemart.Client.ViewModels.Store.Order
{
    public class SelectBonusTypeViewModel : TelemartDialogViewModelBase
    {
        public SelectBonusTypeViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public SelectBonusTypeViewModel()
        {
        }

        #region INPC

        public BonusType SelectedBonus
        {
            get { return GetProperty(() => SelectedBonus); }
            set { SetProperty(() => SelectedBonus, value); }
        }

        public ReadOnlyObservableCollection<BonusType> Bonuses
        {
            get { return GetProperty(() => Bonuses); }
            set { SetProperty(() => Bonuses, value); }
        }

        #endregion

        public static void BuildMetadata(MetadataBuilder<SelectBonusTypeViewModel> builder)
        {
            builder.Property(x => x.SelectedBonus).Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            SelectBonusTypeParameter p = (SelectBonusTypeParameter)Parameter;

            Bonuses = p.Bonuses.ToReadOnlyObservableCollection();

            Title = "Выберите тип бонусов";

            return Task.CompletedTask;
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
