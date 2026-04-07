using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.Bonuses;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class BonusConfirmationViewModel : TelemartDialogViewModelBase
    {
        public BonusConfirmationViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            DisctibuteBonusesCommand = new DelegateCommand(DisctibuteBonuses);
        }

        public BonusConfirmationViewModel()
        {
        }

        public IDelegateCommand DisctibuteBonusesCommand { get; }

        #region INPC

        public ObservableCollection<BonusConfirmationViewItem> Items
        {
            get { return GetProperty(() => Items); }
            private set { SetProperty(() => Items, value); }
        }

        public int CustomerBonuses
        {
            get { return GetProperty(() => CustomerBonuses); }
            private set { SetProperty(() => CustomerBonuses, value, () => RaisePropertiesChanged(nameof(Remain), nameof(UseBonuses))); }
        }

        public int UseBonuses
        {
            get { return GetProperty(() => UseBonuses); }
            set { SetProperty(() => UseBonuses, value); }
        }

        public int OriginalUseBonuses
        {
            get { return GetProperty(() => OriginalUseBonuses); }
            set { SetProperty(() => OriginalUseBonuses, value); }
        }

        public bool BonusesQuantityValid
        {
            get { return GetProperty(() => BonusesQuantityValid); }
            set { SetProperty(() => BonusesQuantityValid, value); }
        }

        public int Limit
        {
            get { return GetProperty(() => Limit); }
            private set { SetProperty(() => Limit, value, () => RaisePropertyChanged(nameof(UseBonuses))); }
        }

        public int Remain => CustomerBonuses - UseBonuses;

        #endregion

        #region DialogSettings

        public override int Height => 455;

        public override int MaxHeight => 600;

        public override int MaxWidth => 1000;

        public override int MinHeight => 200;

        public override int MinWidth => 600;

        public override int Width => 820;

        #endregion

        public static void BuildMetadata(MetadataBuilder<BonusConfirmationViewModel> builder)
        {
            builder.Property(x => x.UseBonuses)
                   .MatchesRule((x) => x > 0, () => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            BonusConfirmationParameter parameter = (BonusConfirmationParameter)Parameter;

            Items = parameter.Items.ToObservableCollection();

            UseBonuses = parameter.Items.Sum(x => x.Quantity);
            OriginalUseBonuses = UseBonuses;
            CustomerBonuses = parameter.CustomerBonuses + UseBonuses;
            Limit = parameter.Items.Sum(x => x.MaxQuantity);

            Title = "Распределение бонусов";

            return Task.CompletedTask;
        }

        protected override Task HandleOkAsync()
        {
            if (UseBonuses != OriginalUseBonuses && !BonusesQuantityValid)
            {
                MessageFacadeService.ShowNotificationError("Начисляемые бонусы изменены, но не распределены");
                return Task.CompletedTask;
            }

            IsOk = true;
            Close();
            return Task.CompletedTask;
        }

        private void DisctibuteBonuses()
        {
            BonusesQuantityValid = false;

            if (UseBonuses > CustomerBonuses)
            {
                MessageFacadeService.ShowNotificationError("Превышен лимит бонусов клиента");
                return;
            }

            if (UseBonuses > Limit)
            {
                MessageFacadeService.ShowNotificationError("Превышен лимит бонусов по товарам");
                return;
            }

            RaisePropertyChanged(nameof(Remain));

            (bool success, string result) result = BonusesDistribution.Distribute(Items.Cast<IBonusProduct>().ToList(), UseBonuses);

            BonusesQuantityValid = result.success;

            if (!result.success)
            {
                MessageFacadeService.ShowNotificationError(result.result);
            }
        }
    }
}
