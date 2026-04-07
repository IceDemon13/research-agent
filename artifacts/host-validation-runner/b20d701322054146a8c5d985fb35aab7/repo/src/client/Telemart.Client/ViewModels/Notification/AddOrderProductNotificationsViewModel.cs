using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Notification;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.ViewModels.Notification
{
    public sealed class AddOrderProductNotificationsViewModel : TelemartDialogViewModelBase, INotificationConfigViewModel
    {
        private readonly IMapper _mapper;

        public AddOrderProductNotificationsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _mapper = mapper;
        }

        public NotificationSubscribeConfigDto Config
        {
            get { return GetProperty(() => Config); }
            set { SetProperty(() => Config, value); }
        }

        public string ProductIdsString
        {
            get { return GetProperty(() => ProductIdsString); }
            set { SetProperty(() => ProductIdsString, value); }
        }

        public ObservableCollection<CategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ObservableCollection<CategoryViewItem> SelectedCategories
        {
            get { return GetProperty(() => SelectedCategories); }
            set { SetProperty(() => SelectedCategories, value); }
        }

        public static void BuildMetadata(MetadataBuilder<AddOrderProductNotificationsViewModel> builder)
        {
            builder.Property(x => x.ProductIdsString)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        protected override async Task HandleLoadedAsync()
        {
            PagedResult<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

            Categories = categories.Data
                .Select(x => _mapper.Map<CategoryViewItem>(x))
                .ToObservableCollection();

            Config = (NotificationSubscribeConfigDto)Parameter;

            if (Config?.ProductIds is not null)
            {
                ProductIdsString = string.Join(",", Config.ProductIds);
            }

            if (Config?.CategoryIds is not null)
            {
                SelectedCategories = Categories
                    .Where(x => Config.CategoryIds.Contains(x.Id))
                    .ToObservableCollection();
            }
            else
            {
                SelectedCategories = new ObservableCollection<CategoryViewItem>();
            }

            Title = "Настройка уведомления о добавлении товаров в заказ";
        }

        protected override Task HandleOkAsync()
        {
            Config ??= new NotificationSubscribeConfigDto();

            Config.ProductIds = ProductIdsString?
                .Split(',')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(int.Parse)
                .ToArray();

            Config.CategoryIds = SelectedCategories?
                .Select(x => x.Id)
                .ToArray();

            CloseOk();
            return Task.CompletedTask;
        }
    }
}