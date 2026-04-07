using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Directories.Category;
using CommentType = Telemart.Client.Dictionaries.CommentType;

namespace Telemart.Client.ViewModels.Comment
{
    public class CommentFilterViewModel : BindableBase, IDataErrorInfo
    {
        private List<EmployeeDto> employeesList;

        private List<CategoryDto> categoryList;

        public CommentFilterViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMapper mapper)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            Mapper = mapper;

            Statuses.AddRange(Dictionaries.GetItems<CommentState>());
            Types.AddRange(Dictionaries.GetItems<CommentType>());
        }

        public CommentFilterViewModel()
        {
        }

        #region Collections

        public ObservableRangeCollection<ComboBoxItem> Employees { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<CategoryViewItem> Categories { get; } = new ObservableRangeCollection<CategoryViewItem>();

        public ObservableRangeCollection<CommentState> Statuses { get; } = new ObservableRangeCollection<CommentState>();

        public ObservableRangeCollection<CommentType> Types { get; } = new ObservableRangeCollection<CommentType>();

        #endregion

        public DateTime? DateFrom
        {
            get { return GetProperty(() => DateFrom); }
            set { SetProperty(() => DateFrom, value); }
        }

        public DateTime? DateTo
        {
            get { return GetProperty(() => DateTo); }
            set { SetProperty(() => DateTo, value); }
        }

        public List<object> SelectedPoductManager
        {
            get { return GetProperty(() => SelectedPoductManager); }
            set { SetProperty(() => SelectedPoductManager, value); }
        }

        public int? SelectedCategoryId
        {
            get { return GetProperty(() => SelectedCategoryId); }
            set { SetProperty(() => SelectedCategoryId, value); }
        }

        public int? ProductRatioFrom
        {
            get { return GetProperty(() => ProductRatioFrom); }
            set { SetProperty(() => ProductRatioFrom, value); }
        }

        public int? ProductRatioTo
        {
            get { return GetProperty(() => ProductRatioTo); }
            set { SetProperty(() => ProductRatioTo, value); }
        }

        public decimal? StarsAvgFrom
        {
            get { return GetProperty(() => StarsAvgFrom); }
            set { SetProperty(() => StarsAvgFrom, value); }
        }

        public decimal? StarsAvgTo
        {
            get { return GetProperty(() => StarsAvgTo); }
            set { SetProperty(() => StarsAvgTo, value); }
        }

        public string Product
        {
            get { return GetProperty(() => Product); }
            set { SetProperty(() => Product, value); }
        }

        public ObservableCollection<CommentType> SelectedTypes
        {
            get { return GetProperty(() => SelectedTypes); }
            set { SetProperty(() => SelectedTypes, value); }
        }

        public ObservableCollection<CommentState> SelectedStatuses
        {
            get { return GetProperty(() => SelectedStatuses); }
            set { SetProperty(() => SelectedStatuses, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        private IDictionaries Dictionaries { get; }

        private IWebClient WebClient { get; }

        private IMapper Mapper { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<CommentFilterViewModel> builder)
        {
            builder.Property(x => x.StarsAvgFrom)
                .MatchesRule(x => x is null || (x is >= 0 and <= 5), () => "Значение должно быть в диапазоне 0..5")
                .MatchesInstanceRule((x, y) => x is null || y.StarsAvgTo is null || x <= y.StarsAvgTo, () => "Значение должно быть меньше или равно значению ДО");
            builder.Property(x => x.StarsAvgTo)
                .MatchesRule(x => x is null || (x is > 0 and <= 5), () => "Значение должно быть в диапазоне 0..5");
            builder.Property(x => x.ProductRatioFrom)
                .MatchesRule(x => x is null || (x is >= 0 and <= 100), () => "Значение должно быть в диапазоне 0..100")
                .MatchesInstanceRule((x, y) => x is null || y.StarsAvgTo is null || x <= y.ProductRatioTo, () => "Значение должно быть меньше или равно значению ДО");
            builder.Property(x => x.ProductRatioTo)
                .MatchesRule(x => x is null || (x is >= 0 and <= 100), () => "Значение должно быть в диапазоне 0..100");
        }

        public CommentFilteringItem GetCommentFilteringItem()
        {
            CommentFilteringItem item = new CommentFilteringItem
            {
                Product = Product,
                DateFrom = DateFrom,
                DateTo = DateTo,
                ProductManagerIds = SelectedPoductManager?.Cast<ComboBoxItem>().Select(p => p.Id).ToList(),
                CategoryId = SelectedCategoryId,
                Types = SelectedTypes?.Select(t => t.Id).ToList(),
                Statuses = SelectedStatuses?.Select(s => s.Id).ToList(),
                ProductRatioFrom = ProductRatioFrom,
                ProductRatioTo = ProductRatioTo,
                StarsAvgFrom = StarsAvgFrom,
                StarsAvgTo = StarsAvgTo
            };

            return item;
        }

        public Task RefreshAsync()
        {
            return Task.WhenAll(RefreshEmployeesAsync(), RefreshCategoriesAsync());
        }

        public void ResetFilterValues()
        {
            DateFrom = DateTime.Now.AddDays(-3);
            DateTo = DateTime.Now;
            SelectedStatuses = new ObservableCollection<CommentState>();
            SelectedTypes = new ObservableCollection<CommentType>();
            SelectedPoductManager = new List<object>();
            SelectedCategoryId = null;
            Product = null;
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true)
                .GetPagedResultDataAsync();

            if (ReferenceEquals(employees, employeesList))
            {
                return;
            }

            Employees.Clear();

            employeesList = employees;

            List<ComboBoxItem> employeeItems = employeesList
                .Where(x => x.Active && x.Roles.Contains(Role.Product.Name))
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToList();

            Employees.AddRange(employeeItems);
        }

        private async Task RefreshCategoriesAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true)
                .GetPagedResultDataAsync();

            if (ReferenceEquals(categories, categoryList))
            {
                return;
            }

            Categories.Clear();

            categoryList = categories;

            List<CategoryViewItem> categoryItems = categoryList.Where(x => x.Active > 0)
                .OrderBy(x => x.Left)
                .Select(x => Mapper.Map<CategoryViewItem>(x))
                .ToList();

            Categories.AddRange(categoryItems);
        }
    }
}