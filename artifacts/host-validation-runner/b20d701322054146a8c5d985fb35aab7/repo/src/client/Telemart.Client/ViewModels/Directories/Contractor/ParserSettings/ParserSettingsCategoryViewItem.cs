using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Directories.Contractor.ParserSettings
{
    public class ParserSettingsCategoryViewItem : BindableBase, IDataErrorInfo
    {
        public ParserSettingsCategoryViewItem(int parserId)
        {
            ParserId = parserId;
            Replaces = new ObservableCollection<ParserSettingsCategoryReplaceViewItem>();
            StopWords = new ObservableCollection<string>();
            OkWords = new ObservableCollection<string>();
        }

        public ParserSettingsCategoryViewItem()
            : this(0)
        {
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ParserId
        {
            get { return GetProperty(() => ParserId); }
            set { SetProperty(() => ParserId, value); }
        }

        public string Url
        {
            get { return GetProperty(() => Url); }
            set { SetProperty(() => Url, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            init { SetProperty(() => Description, value); }
        }

        public bool BrandIgnore
        {
            get { return GetProperty(() => BrandIgnore); }
            set { SetProperty(() => BrandIgnore, value); }
        }

        public ObservableCollection<int> CategoryIds
        {
            get { return GetProperty(() => CategoryIds); }
            set { SetProperty(() => CategoryIds, value); }
        }

        public ObservableCollection<string> OkWords
        {
            get { return GetProperty(() => OkWords); }
            set { SetProperty(() => OkWords, value); }
        }

        public ObservableCollection<string> StopWords
        {
            get { return GetProperty(() => StopWords); }
            set { SetProperty(() => StopWords, value); }
        }

        public ObservableCollection<ParserSettingsCategoryReplaceViewItem> Replaces
        {
            get { return GetProperty(() => Replaces); }
            set { SetProperty(() => Replaces, value); }
        }

        public ObservableCollection<CategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            set { SetProperty(() => Categories, value, () => RaisePropertyChanged(nameof(CategoriesFormatted))); }
        }

        public int UrlComparisonType
        {
            get { return GetProperty(() => UrlComparisonType); }
            set { SetProperty(() => UrlComparisonType, value); }
        }

        public string CategoriesFormatted => string.Join(", ", Categories?.Select(x => x.Name) ?? Array.Empty<string>());

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<ParserSettingsCategoryViewItem> builder)
        {
            builder.Property(x => x.Url)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.CategoryIds)
                .MatchesRule(x => x?.Any() == true, () => Resources.RequiredErrorMessage);
        }
    }
}