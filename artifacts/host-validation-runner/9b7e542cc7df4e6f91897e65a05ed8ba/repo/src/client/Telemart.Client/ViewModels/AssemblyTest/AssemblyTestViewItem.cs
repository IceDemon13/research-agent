using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssemblyTest
{
    public class AssemblyTestViewItem : TelemartEditorViewItemBase
    {
        public AssemblyTestViewItem()
        {
            MainFeatureIds = new ObservableCollection<int>();
            SlaveCategories = new ObservableCollection<AssemblyTestSlaveCategoryViewItem>();
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string NameUa
        {
            get { return GetProperty(() => NameUa); }
            set { SetProperty(() => NameUa, value); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public int GroupId
        {
            get { return GetProperty(() => GroupId); }
            set { SetProperty(() => GroupId, value); }
        }

        public int MainCategoryId
        {
            get { return GetProperty(() => MainCategoryId); }
            set { SetProperty(() => MainCategoryId, value); }
        }

        public ObservableCollection<int> MainFeatureIds
        {
            get { return GetProperty(() => MainFeatureIds); }
            set { SetProperty(() => MainFeatureIds, value); }
        }

        public string GroupName
        {
            get { return GetProperty(() => GroupName); }
            set { SetProperty(() => GroupName, value); }
        }

        public string Suffix
        {
            get { return GetProperty(() => Suffix); }
            set { SetProperty(() => Suffix, value); }
        }

        public string Regex
        {
            get { return GetProperty(() => Regex); }
            set { SetProperty(() => Regex, value); }
        }

        public bool AvailOnWeb
        {
            get { return GetProperty(() => AvailOnWeb); }
            set { SetProperty(() => AvailOnWeb, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public bool Required
        {
            get { return GetProperty(() => Required); }
            set { SetProperty(() => Required, value); }
        }

        public ObservableCollection<AssemblyTestSlaveCategoryViewItem> SlaveCategories
        {
            get { return GetProperty(() => SlaveCategories); }
            set { SetProperty(() => SlaveCategories, value); }
        }

        public static void BuildMetadata(MetadataBuilder<AssemblyTestViewItem> builder)
        {
            builder.Property(x => x.MainCategoryId)
               .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.MainFeatureIds)
              .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameUa)
              .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameEn)
                .Required(() => Resources.RequiredErrorMessage);
        }

        public override object Clone()
        {
            AssemblyTestViewItem item = (AssemblyTestViewItem)base.Clone();

            item.MainFeatureIds = MainFeatureIds.ToObservableCollection();
            item.SlaveCategories = SlaveCategories.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();

            return item;
        }
    }
}