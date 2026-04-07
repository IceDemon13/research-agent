using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Customer;
using Telemart.Client.Data.Requests.Features.Customer.Actions;
using Telemart.Client.Data.Requests.Features.CustomerBonus;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Customer;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.Constants;

namespace Telemart.Client.ViewModels.Customer
{
    public sealed class CustomerTransferBonusesViewModel : TelemartDialogViewModelBase
    {
        public CustomerTransferBonusesViewModel(
           IWebClient webClient,
           IDictionaries dictionaries,
           IMessageFacadeService messageFacadeService)
           : base(webClient, dictionaries, messageFacadeService)
        {
            Quantity = 1;
        }

        public ReadOnlyObservableCollection<CustomerFindType> FindTypes
        {
            get { return GetProperty(() => FindTypes); }
            private set { SetProperty(() => FindTypes, value); }
        }

        public CustomerFindType SelectedFindType
        {
            get { return GetProperty(() => SelectedFindType); }
            set { SetProperty(() => SelectedFindType, value, SelectedFindTypeChanged); }
        }

        public ReadOnlyObservableCollection<BonusType> BonusTypes
        {
            get { return GetProperty(() => BonusTypes); }
            private set { SetProperty(() => BonusTypes, value); }
        }

        public BonusType SelectedBonusType
        {
            get { return GetProperty(() => SelectedBonusType); }
            set { SetProperty(() => SelectedBonusType, value); }
        }

        public string Value
        {
            get { return GetProperty(() => Value); }
            set { SetProperty(() => Value, value); }
        }

        public int SenderCustomerId
        {
            get { return GetProperty(() => SenderCustomerId); }
            set { SetProperty(() => SenderCustomerId, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public DateTime? ExpireDate
        {
            get { return GetProperty(() => ExpireDate); }
            set { SetProperty(() => ExpireDate, value); }
        }

        private IDispatcherService DispatcherService => GetService<IDispatcherService>();

        public static void BuildMetadata(MetadataBuilder<CustomerTransferBonusesViewModel> builder)
        {
            builder.Property(x => x.Value).MatchesInstanceRule(
                (x, y) => (y.SelectedFindType == CustomerFindType.Phone && !string.IsNullOrWhiteSpace(x))
                || (y.SelectedFindType == CustomerFindType.Email && !string.IsNullOrWhiteSpace(x) && Regex.IsMatch(x, RegexConstants.EmailRegex)),
                () => "Введите корректные данные");

            builder.Property(x => x.SelectedBonusType)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Quantity)
                .MatchesInstanceRule((x, y) => x < 100_000, () => "Максимальное значение 99999")
                .MatchesInstanceRule((x, y) => x > 0, () => "Минимальное значение 1");
        }

        protected override async Task HandleLoadedAsync()
        {
            CustomerTransferBonusesParameter parameter = (CustomerTransferBonusesParameter)Parameter;
            SenderCustomerId = parameter.SenderCustomerId;
            SelectedBonusType = parameter.SelectedBonusType;

            ExpireDateDto dto = await WebClient.ExecuteApiRequestAsync(new QueryExpireDate(SelectedBonusType.Id, DateTime.Now));

            ExpireDate = dto.ExpireDate;

            Title = "Перевод бонусов";
            BonusTypes = parameter.BonusTypes;

            FindTypes = Dictionaries.GetItems<CustomerFindType>().ToReadOnlyObservableCollection();
            SelectedFindType = FindTypes.First();

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            const string ErrorRu = "Ошибка при переводе бонусов";
            const string ErrorEn = "Failed to delete bonuses";

            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            CustomerFilteringItem filteringItem = SelectedFindType == CustomerFindType.Email
             ? new CustomerFilteringItem(phone: null, email: Value)
             : new CustomerFilteringItem(phone: Value, email: null);

            PagedResult<CustomerDto> customers = await WebClient.ExecuteApiRequestAsync(new QueryCustomers(filteringItem));

            CustomerDto customerRecepient = customers.Data.FirstOrDefault(x => x.Phone1 == Value || x.Email == Value);

            if (customerRecepient is null)
            {
                MessageFacadeService.ShowNotificationWarning("Клиент, который соответствует заданным критериям поиска, не найден");
                return;
            }

            DelayedConfirmViewModel viewModel = DialogDocumentManagerService.ShowView<DelayedConfirmViewModel>($"Перевод {Quantity} {SelectedBonusType.Name} клиенту {customerRecepient.Fio}", this);

            if (!viewModel.IsOk)
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new CustomerTransferBonuses(SenderCustomerId, customerRecepient.Id, Quantity, SelectedBonusType.Id, ExpireDate));

                MessageFacadeService.ShowNotificationInfo("Бонусы успешно переведены");

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(ErrorRu);
                ShowValidationResultView(ErrorRu, exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, ErrorEn);
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, ErrorEn);
                MessageFacadeService.ShowNotificationError(ErrorRu);
            }
        }

        private void SelectedFindTypeChanged()
        {
            Value = null;
            RaisePropertyChanged(nameof(Value));
        }
    }
}