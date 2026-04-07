using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.Contractor.ParserSettings
{
    public sealed class ParserSettingsCategoryViewModel : TelemartDialogViewModelBase
    {
        public ParserSettingsCategoryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            CreateReplaceCommand = new DelegateCommand(() => Model.Replaces.Add(new ParserSettingsCategoryReplaceViewItem()));
            DeleteReplaceCommand = new DelegateCommand<ParserSettingsCategoryReplaceViewItem>(x => Model.Replaces.Remove(x), x => x != null);
        }

        public ParserSettingsCategoryViewModel()
        {
        }

        public IDelegateCommand CreateReplaceCommand { get; }

        public IDelegateCommand DeleteReplaceCommand { get; }

        public ParserSettingsCategoryViewItem Model
        {
            get { return GetProperty(() => Model); }
            set { SetProperty(() => Model, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Categories
        {
            get { return GetProperty(() => Categories); }
            set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> ComparisonTypes
        {
            get { return GetProperty(() => ComparisonTypes); }
            set { SetProperty(() => ComparisonTypes, value); }
        }

        protected override async Task HandleLoadedAsync()
        {
            Model = (ParserSettingsCategoryViewItem)Parameter;

            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            Categories = categories
                .Where(x => x.IsParent && x.Active == 1)
                .OrderBy(x => x.Left)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            ComparisonTypes = new[]
            {
                ParserCategoryComparisonType.Ordinal,
                ParserCategoryComparisonType.Like,
                ParserCategoryComparisonType.Regex
            }
            .Select(x => new ComboBoxItem(x.Id, x.Description))
            .ToReadOnlyObservableCollection();

            Title = Model.Id <= 0
                ? "Создание категории"
                : $"Редактирование категории ({Model.Id})";

            await base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(Model) || Model.Replaces.Any(x => IDataErrorInfoHelper.HasErrors(x)))
            {
                return Task.CompletedTask;
            }

            IsOk = true;
            Close();

            return Task.CompletedTask;
        }
    }
}