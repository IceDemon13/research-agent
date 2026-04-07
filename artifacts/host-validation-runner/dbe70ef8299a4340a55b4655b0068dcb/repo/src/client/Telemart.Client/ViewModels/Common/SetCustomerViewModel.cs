using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Customer;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Customer;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Customer;
using Telemart.Common.Constants;

namespace Telemart.Client.ViewModels.Common
{
    public class SetCustomerViewModel : TelemartDialogViewModelBase
    {
        private Func<CustomerDto, Task<bool>> okCommand;

        public SetCustomerViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
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

        public string Value
        {
            get { return GetProperty(() => Value); }
            set { SetProperty(() => Value, value); }
        }

        public static void BuildMetadata(MetadataBuilder<SetCustomerViewModel> builder)
        {
            builder.Property(x => x.Value).MatchesInstanceRule(
                (x, y) => (y.SelectedFindType == CustomerFindType.Phone && !string.IsNullOrWhiteSpace(x))
                || (y.SelectedFindType == CustomerFindType.Email && !string.IsNullOrWhiteSpace(x) && Regex.IsMatch(x, RegexConstants.EmailRegex)),
                () => "Введите корректные данные");
        }

        protected override Task HandleLoadedAsync()
        {
            SetCustomerParameter parameter = (SetCustomerParameter)Parameter;

            Title = parameter.Title;
            okCommand = parameter.OkCommand;

            FindTypes = Dictionaries.GetItems<CustomerFindType>().ToReadOnlyObservableCollection();
            SelectedFindType = FindTypes.First();

            return base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            CustomerFilteringItem filteringItem = SelectedFindType == CustomerFindType.Email
                ? new CustomerFilteringItem(phone: null, email: Value)
                : new CustomerFilteringItem(phone: Value, email: null);

            PagedResult<CustomerDto> customers = await WebClient.ExecuteApiRequestAsync(new QueryCustomers(filteringItem));

            if (customers.Data.Any(x => x.Phone1 == Value || x.Email == Value))
            {
                bool success = await okCommand(customers.Data.First());

                if (!success)
                {
                    return;
                }

                IsOk = true;
                Close();
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Клиент, который соответствует заданным критериям поиска, не найден");
            }
        }

        private void SelectedFindTypeChanged()
        {
            Value = null;
            RaisePropertyChanged(nameof(Value));
        }
    }
}
