using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Dialogs
{
    public sealed class GetCategoryFromUserViewModel : TelemartDialogViewModelBase
    {
        public GetCategoryFromUserViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public GetCategoryFromUserViewModel()
        {
        }

        public ReadOnlyObservableCollection<CategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public int? CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public static void BuildMetadata(MetadataBuilder<GetFeatureFromUserViewModel> builder)
        {
            builder.Property(x => x.CategoryId)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            GetCategoryFromUserParameter parameter = (GetCategoryFromUserParameter)Parameter;

            Title = parameter.Title;

            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            Categories = categories
                .Where(x => x.Active > 0)
                .OrderBy(x => x.Position)
                .ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();
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
