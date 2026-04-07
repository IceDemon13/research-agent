using System;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.Contractor.ParserSettings
{
    public class ParserSettingsViewItem : TelemartEditorViewItemBase
    {
        public ParserSettingsViewItem()
        {
            Availabilities = new ObservableCollection<ParserSettingsAvailabilityViewItem>();
            Categories = new ObservableCollection<ParserSettingsCategoryViewItem>();
        }

        public int ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public string Login
        {
            get { return GetProperty(() => Login); }
            set { SetProperty(() => Login, value); }
        }

        public string Password
        {
            get { return GetProperty(() => Password); }
            set { SetProperty(() => Password, value); }
        }

        public bool AutoRecognize
        {
            get { return GetProperty(() => AutoRecognize); }
            set { SetProperty(() => AutoRecognize, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value); }
        }

        public int PriceRrpLifetime
        {
            get { return GetProperty(() => PriceRrpLifetime); }
            set { SetProperty(() => PriceRrpLifetime, value); }
        }

        public int PriceRetailLifetime
        {
            get { return GetProperty(() => PriceRetailLifetime); }
            set { SetProperty(() => PriceRetailLifetime, value); }
        }

        public int PriceWholesaleLifetime
        {
            get { return GetProperty(() => PriceWholesaleLifetime); }
            set { SetProperty(() => PriceWholesaleLifetime, value); }
        }

        public bool ParseAllBrands
        {
            get { return GetProperty(() => ParseAllBrands); }
            set { SetProperty(() => ParseAllBrands, value); }
        }

        public bool ParseInactiveCategories
        {
            get { return GetProperty(() => ParseInactiveCategories); }
            set { SetProperty(() => ParseInactiveCategories, value); }
        }

        public DateTime ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int ModifiedBy
        {
            get { return GetProperty(() => ModifiedBy); }
            set { SetProperty(() => ModifiedBy, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public ObservableCollection<ParserSettingsAvailabilityViewItem> Availabilities
        {
            get { return GetProperty(() => Availabilities); }
            set { SetProperty(() => Availabilities, value); }
        }

        public ObservableCollection<ParserSettingsCategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            set { SetProperty(() => Categories, value); }
        }

        public ParserSettingsFileViewItem ParserSettingsFile
        {
            get { return GetProperty(() => ParserSettingsFile); }
            set { SetProperty(() => ParserSettingsFile, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ParserSettingsViewItem> builder)
        {
            builder.Property(x => x.Name).MatchesRule(x => x?.Length > 0 && x.Length < 50, () => "Длина должна быть в диапазоне [1..49] символов");
            builder.Property(x => x.TypeId).Required(() => Resources.RequiredErrorMessage);
        }

        public override object Clone()
        {
            ParserSettingsViewItem item = ReflectionObjectCloner.Clone(this);

            item.Availabilities = Availabilities.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();
            item.Categories = Categories.Select(x =>
            {
                ParserSettingsCategoryViewItem category = ReflectionObjectCloner.Clone(x);

                category.CategoryIds = x.CategoryIds.ToObservableCollection();
                category.OkWords = x.OkWords.ToObservableCollection();
                category.StopWords = x.StopWords.ToObservableCollection();
                category.Replaces = x.Replaces.Select(y => ReflectionObjectCloner.Clone(y)).ToObservableCollection();

                return category;
            }).ToObservableCollection();

            if (ParserSettingsFile is not null)
            {
                ParserSettingsFileViewItem parserSettingsFile = ReflectionObjectCloner.Clone(ParserSettingsFile);

                parserSettingsFile.FileColumn = ReflectionObjectCloner.Clone(ParserSettingsFile.FileColumn);
                parserSettingsFile.FileColumn.CategoryRule1 = ReflectionObjectCloner.Clone(ParserSettingsFile.FileColumn.CategoryRule1);
                parserSettingsFile.FileColumn.CategoryRule2 = ReflectionObjectCloner.Clone(ParserSettingsFile.FileColumn.CategoryRule2);
                parserSettingsFile.FileColumn.CategoryRule3 = ReflectionObjectCloner.Clone(ParserSettingsFile.FileColumn.CategoryRule3);

                parserSettingsFile.FileColumn.Prices = ParserSettingsFile.FileColumn.Prices.Select(x =>
                {
                    ParserSettingsFilePriceViewItem viewItem = ReflectionObjectCloner.Clone(x);
                    return viewItem;
                }).ToObservableCollection();

                item.ParserSettingsFile = parserSettingsFile;
            }

            return item;
        }
    }
}