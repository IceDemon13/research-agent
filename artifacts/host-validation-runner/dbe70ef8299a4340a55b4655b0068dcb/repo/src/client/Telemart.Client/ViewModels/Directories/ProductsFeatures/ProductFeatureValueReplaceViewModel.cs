using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Content.ProductFeatureGroups.FeatureValues;

namespace Telemart.Client.ViewModels.Directories.ProductsFeatures
{
    public sealed class ProductFeatureValueReplaceViewModel : TelemartDialogViewModelBase
    {
        private ProductFeaturesViewModel productFeaturesViewModel;
        private string featureFieldName;

        public ProductFeatureValueReplaceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Title = "Замена";
        }

        public ReadOnlyObservableCollection<FeatureValueViewItem> FeatureValues
        {
            get { return GetProperty(() => FeatureValues); }
            private set { SetProperty(() => FeatureValues, value); }
        }

        public string FeatureName
        {
            get { return GetProperty(() => FeatureName); }
            private set { SetProperty(() => FeatureName, value); }
        }

        public int? FromValueId
        {
            get { return GetProperty(() => FromValueId); }
            set { SetProperty(() => FromValueId, value); }
        }

        public int? ToValueId
        {
            get { return GetProperty(() => ToValueId); }
            set { SetProperty(() => ToValueId, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ProductFeatureValueReplaceViewModel> builder)
        {
            builder.Property(x => x.FromValueId)
                .Required(() => Resources.RequiredErrorMessage);
        }

        public void Refresh(string featureName, string fieldName, int? featureValueId, IReadOnlyCollection<FeatureValueViewItem> featureValues)
        {
            FeatureName = featureName;
            featureFieldName = fieldName;
            FromValueId = featureValueId;
            FeatureValues = featureValues.ToReadOnlyObservableCollection();
        }

        protected override Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this) || !MessageFacadeService.Confirm("Вы уверены?"))
            {
                return Task.CompletedTask;
            }

            if (FromValueId == ToValueId)
            {
                MessageFacadeService.ShowNotificationWarning("Нечего заменять");
            }
            else
            {
                productFeaturesViewModel.ChangeValues(featureFieldName, FromValueId.Value, ToValueId);
            }

            return Task.CompletedTask;
        }

        protected override void OnParentViewModelChanged(object parentViewModel)
        {
            productFeaturesViewModel = (ProductFeaturesViewModel)parentViewModel;
        }
    }
}
