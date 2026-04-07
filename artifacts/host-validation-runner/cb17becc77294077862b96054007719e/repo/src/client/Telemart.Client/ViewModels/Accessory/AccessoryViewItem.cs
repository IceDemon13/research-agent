using System;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Accessory
{
    public class AccessoryViewItem : TelemartEditorViewItemBase
    {
        public AccessoryViewItem()
        {
            Features = new ObservableCollection<AccessoryFeatureViewItem>();
            Categories = new ObservableCollection<AccessoryCategoryViewItem>();
        }

        public int? CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
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

        public ObservableCollection<AccessoryCategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            set { SetProperty(() => Categories, value); }
        }

        public ObservableCollection<AccessoryFeatureViewItem> Features
        {
            get { return GetProperty(() => Features); }
            set { SetProperty(() => Features, value); }
        }

        public string DisplayCategories
        {
            get
            {
                return string.Join(
                    ", ",
                    Categories?.Select(x => x.CategoryName) ?? Enumerable.Empty<string>());
            }

            set
            {
                SetProperty(() => DisplayCategories, value);
            }
        }

        public static void BuildMetadata(MetadataBuilder<AccessoryViewItem> builder)
        {
            builder.Property(x => x.CategoryId)
                .Required(() => Resources.RequiredErrorMessage);
        }

        public override object Clone()
        {
            AccessoryViewItem accessory = (AccessoryViewItem)base.Clone();

            accessory.Categories = Categories?.Select(x =>
            {
                AccessoryCategoryViewItem category = ReflectionObjectCloner.Clone(x);

                category.Features = category.Features.Select(z =>
                {
                    AccessoryCategoryFeatureViewItem categoryFeature = ReflectionObjectCloner.Clone(z);

                    return categoryFeature;
                }).ToObservableCollection();

                return category;
            }).ToObservableCollection();

            accessory.Features = Features.Select(x =>
            {
                AccessoryFeatureViewItem feature = ReflectionObjectCloner.Clone(x);

                return feature;
            }).ToObservableCollection();

            return accessory;
        }
    }
}
