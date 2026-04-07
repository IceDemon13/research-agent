using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;

namespace Telemart.Client.ViewModels.Accessory
{
    public sealed class AccessoryCategoryFeatureViewModel : TelemartDialogViewModelBase
    {
        private AccessoryCategoryFeatureParameter parameter;

        public AccessoryCategoryFeatureViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            AddFeatureCommand = new DelegateCommand(AddFeature);
            RemoveFeatureCommand = new DelegateCommand(RemoveFeature, () => SelectedFeature is not null);
        }

        public IDelegateCommand AddFeatureCommand { get; }

        public IDelegateCommand RemoveFeatureCommand { get; }

        public AccessoryCategoryFeatureViewItem SelectedFeature
        {
            get { return GetProperty(() => SelectedFeature); }
            set { SetProperty(() => SelectedFeature, value); }
        }

        public string CategoryName
        {
            get { return GetProperty(() => CategoryName); }
            set { SetProperty(() => CategoryName, value); }
        }

        public ObservableCollection<AccessoryCategoryFeatureViewItem> Features
        {
            get { return GetProperty(() => Features); }
            set { SetProperty(() => Features, value); }
        }

        protected override Task HandleLoadedAsync()
        {
            parameter = (AccessoryCategoryFeatureParameter)Parameter;
            Features = parameter.Features;
            CategoryName = parameter.CategoryName;

            Title = "Условия по характеристикам";

            return base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();
            return Task.CompletedTask;
        }

        private void AddFeature()
        {
            GetCategoryFeatureValueViewModel viewModel = DialogDocumentManagerService.ShowView<GetCategoryFeatureValueViewModel>(new GetCategoryFeatureValueParameter(true, false, parameter.CategoryId, true), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            Features.Add(new AccessoryCategoryFeatureViewItem(
                0,
                parameter.AccessoryCategoryId,
                viewModel.SelectedFeature.Value.Id,
                viewModel.SelectedFeature.Value.DisplayValue,
                viewModel.SelectedFeatureValue.Value.Id,
                viewModel.SelectedFeatureValue.Value.DisplayValue));
        }

        private void RemoveFeature()
        {
            Features.Remove(SelectedFeature);
        }
    }
}