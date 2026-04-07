using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.ProductCompatibility
{
    public class ProductCompatibilityViewItem : TelemartEditorViewItemBase
    {
        public int TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value, OnTypeIdChanged); }
        }

        public int? NotificationImageId
        {
            get { return GetProperty(() => NotificationImageId); }
            set { SetProperty(() => NotificationImageId, value); }
        }

        public int? SlaveCategoryId
        {
            get { return GetProperty(() => SlaveCategoryId); }
            set { SetProperty(() => SlaveCategoryId, value); }
        }

        public HierarchicalItem? SlaveFeature
        {
            get { return GetProperty(() => SlaveFeature); }
            set { SetProperty(() => SlaveFeature, value); }
        }

        public string CompareMethod
        {
            get { return GetProperty(() => CompareMethod); }
            set { SetProperty(() => CompareMethod, value); }
        }

        public string ValidationMessageTemplate
        {
            get { return GetProperty(() => ValidationMessageTemplate); }
            set { SetProperty(() => ValidationMessageTemplate, value); }
        }

        public string ValidationMessageTemplateUkr
        {
            get { return GetProperty(() => ValidationMessageTemplateUkr); }
            set { SetProperty(() => ValidationMessageTemplateUkr, value); }
        }

        public string ValidationMessageTemplateEn
        {
            get { return GetProperty(() => ValidationMessageTemplateEn); }
            set { SetProperty(() => ValidationMessageTemplateEn, value); }
        }

        public int? MasterCategoryId
        {
            get { return GetProperty(() => MasterCategoryId); }
            set { SetProperty(() => MasterCategoryId, value); }
        }

        public HierarchicalItem? MasterFeature
        {
            get { return GetProperty(() => MasterFeature); }
            set { SetProperty(() => MasterFeature, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public bool AllowEditNotificationImageId => TypeId != ProductCompatibilityType.Accessory.Id;

        public static void BuildMetadata(MetadataBuilder<ProductCompatibilityViewItem> builder)
        {
            builder.Property(x => x.NotificationImageId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SlaveCategoryId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SlaveFeature).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.CompareMethod).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ValidationMessageTemplate)
                .MatchesInstanceRule((x, y) => y.TypeId != ProductCompatibilityType.PC.Id || !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.ValidationMessageTemplateUkr)
              .MatchesInstanceRule((x, y) => y.TypeId != ProductCompatibilityType.PC.Id || !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.ValidationMessageTemplateEn)
                .MatchesInstanceRule((x, y) => y.TypeId != ProductCompatibilityType.PC.Id || !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.MasterCategoryId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.MasterFeature).Required(() => Resources.RequiredErrorMessage);
        }

        private void OnTypeIdChanged()
        {
            if (TypeId == ProductCompatibilityType.Accessory.Id)
            {
                NotificationImageId = NotificationImage.InformationId;
            }

            RaisePropertiesChanged(nameof(ValidationMessageTemplate), nameof(AllowEditNotificationImageId));
        }
    }
}