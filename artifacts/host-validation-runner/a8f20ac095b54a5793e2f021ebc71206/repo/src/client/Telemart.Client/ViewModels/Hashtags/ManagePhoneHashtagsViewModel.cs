using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Customer;
using Telemart.Client.Data.Requests.Features.Hashtag;
using Telemart.Client.Data.Requests.Features.Hashtag.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Customer;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Customer;

namespace Telemart.Client.ViewModels.Hashtags
{
    public sealed class ManagePhoneHashtagsViewModel : TelemartDialogViewModelBase
    {
        public ManagePhoneHashtagsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;
            SelectedMinusHashtagIds = new ObservableCollection<int>();
            SelectedPlusHashtagIds = new ObservableCollection<int>();
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            private set { SetProperty(() => Phone, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            private set { SetProperty(() => Fio, value); }
        }

        public ObservableCollection<int> SelectedPlusHashtagIds
        {
            get { return GetProperty(() => SelectedPlusHashtagIds); }
            set { SetProperty(() => SelectedPlusHashtagIds, value); }
        }

        public ObservableCollection<int> SelectedMinusHashtagIds
        {
            get { return GetProperty(() => SelectedMinusHashtagIds); }
            set { SetProperty(() => SelectedMinusHashtagIds, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> PlusHashtags
        {
            get { return GetProperty(() => PlusHashtags); }
            private set { SetProperty(() => PlusHashtags, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> MinusHashtags
        {
            get { return GetProperty(() => MinusHashtags); }
            private set { SetProperty(() => MinusHashtags, value); }
        }

        public KeyGesture SelectKey
        {
            get { return GetProperty(() => SelectKey); }
            set { SetProperty(() => SelectKey, value); }
        }

        private IErrorHandler ErrorHandler { get; }

        protected override async Task HandleLoadedAsync()
        {
            ManagePhoneHashtagsParameter parameter = (ManagePhoneHashtagsParameter)Parameter;

            Phone = parameter.Phone;

            List<HashtagDto> hashtags = await WebClient.ExecuteApiRequestAsync(new QueryHashtags());

            PlusHashtags = hashtags
                .Where(x => x.TypeId == HashtagType.PlusId && x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            MinusHashtags = hashtags
                .Where(x => x.TypeId == HashtagType.MinusId && x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
               .ToReadOnlyObservableCollection();

            CustomerFilteringItem filteringItem = new CustomerFilteringItem(phone: parameter.Phone, email: null);

            PagedResult<CustomerDto> customers = await WebClient.ExecuteApiRequestAsync(new QueryCustomers(filteringItem));

            if (customers.Data.Any())
            {
                CustomerDto customerDto = customers.Data.First();

                Fio = customerDto.Fio;
                SelectedMinusHashtagIds = customerDto.MinusHashtagIds?.ToObservableCollection() ?? new ObservableCollection<int>();
                SelectedPlusHashtagIds = customerDto.PlusHashtagIds?.ToObservableCollection() ?? new ObservableCollection<int>();
            }
            else
            {
                List<HashtagDto> phoneHashtags = await WebClient.ExecuteApiRequestAsync(new QueryHashtagsByPhone(parameter.Phone));

                SelectedPlusHashtagIds = phoneHashtags
                    .Where(x => x.TypeId == HashtagType.PlusId)
                    .Select(x => x.Id)
                    .ToObservableCollection();

                SelectedMinusHashtagIds = phoneHashtags
                    .Where(x => x.TypeId == HashtagType.MinusId)
                    .Select(x => x.Id)
                    .ToObservableCollection();

                Fio = "[Незарегистрированный пользователь]";
            }

            SelectKey = new KeyGesture(Key.Enter);

            Title = "Управление хэштегами";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            ReadOnlyCollection<int> hashtagIds = SelectedPlusHashtagIds.Concat(SelectedMinusHashtagIds).ToReadOnlyCollection();

            UpdateHashtagsByPhoneDto saveDto = new UpdateHashtagsByPhoneDto { Phone = Phone, HashtagIds = hashtagIds };

            (await ErrorHandler.HandleErrorsAsync(x => WebClient.ExecuteApiRequestAsync(new UpdateHashtagsByPhone(saveDto)), "обновлении хештегов", "Хэштеги обновлены", this, true))
                .IfNotNull(x =>
                {
                    IsOk = true;
                    Close();
                });
        }
    }
}