using System;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class AddBonusesViewModel : TelemartDialogViewModelBase
    {
        public AddBonusesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public int CustomerBonusesQuantity
        {
            get { return GetProperty(() => CustomerBonusesQuantity); }
            set { SetProperty(() => CustomerBonusesQuantity, value); }
        }

        public int OrderLeftToPay
        {
            get { return GetProperty(() => OrderLeftToPay); }
            set { SetProperty(() => OrderLeftToPay, value); }
        }

        public string BonusTypeName
        {
            get { return GetProperty(() => BonusTypeName); }
            set { SetProperty(() => BonusTypeName, value); }
        }

        public int SelectedQuantity
        {
            get { return GetProperty(() => SelectedQuantity); }
            set { SetProperty(() => SelectedQuantity, value); }
        }

        public static void BuildMetadata(MetadataBuilder<AddBonusesViewModel> builder)
        {
            builder.Property(x => x.SelectedQuantity)
                .MatchesInstanceRule((x, y) => x > 0, () => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            AddBonusesParameter parameter = (AddBonusesParameter)Parameter;

            BonusTypeName = parameter.BonusTypeName;
            OrderLeftToPay = parameter.OrderLeftToPay;
            CustomerBonusesQuantity = parameter.CustomerBonusesQuantity;
            SelectedQuantity = Math.Min(CustomerBonusesQuantity, OrderLeftToPay);

            Title = $"Внесение бонусов по заказу №{parameter.OrderId}";

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            if (SelectedQuantity > CustomerBonusesQuantity)
            {
                MessageFacadeService.ShowNotificationError("У клиенте нет столько бонусов");
                return Task.CompletedTask;
            }

            if (SelectedQuantity > OrderLeftToPay)
            {
                MessageFacadeService.ShowNotificationError("Выбранная сумма больше,\n чем осталось внести в заказ");
                return Task.CompletedTask;
            }

            CloseOk();

            return Task.CompletedTask;
        }
    }
}