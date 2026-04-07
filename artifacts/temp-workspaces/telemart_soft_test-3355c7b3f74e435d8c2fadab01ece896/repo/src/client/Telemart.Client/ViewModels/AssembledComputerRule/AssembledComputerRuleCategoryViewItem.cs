using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class AssembledComputerRuleCategoryViewItem : TelemartViewItemBase
    {
        public event EventHandler<ProductTypesChangedEventArgs> ProductTypesChanged;

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public string CategoryName
        {
            get { return GetProperty(() => CategoryName); }
            set { SetProperty(() => CategoryName, value); }
        }

        public int? FeatureId
        {
            get { return GetProperty(() => FeatureId); }
            set { SetProperty(() => FeatureId, value); }
        }

        public string FeatureName
        {
            get { return GetProperty(() => FeatureName); }
            set { SetProperty(() => FeatureName, value); }
        }

        public int? FeatureValueId
        {
            get { return GetProperty(() => FeatureValueId); }
            set { SetProperty(() => FeatureValueId, value); }
        }

        public string FeatureValueName
        {
            get { return GetProperty(() => FeatureValueName); }
            set { SetProperty(() => FeatureValueName, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public bool UseAnyProducts
        {
            get { return GetProperty(() => UseAnyProducts); }
            set { SetProperty(() => UseAnyProducts, value); }
        }

        public string CompareMethod
        {
            get { return GetProperty(() => CompareMethod); }
            set { SetProperty(() => CompareMethod, value); }
        }

        public ObservableCollection<ProductType> ProductTypes
        {
            get { return GetProperty(() => ProductTypes); }
            set { SetProperty(() => ProductTypes, value, OnProductTypesChanged); }
        }

        public void OnProductTypesChanged()
        {
            ProductTypesChanged?.Invoke(this, new ProductTypesChangedEventArgs(CategoryId, ProductTypes));
        }
    }

    public sealed class ProductTypesChangedEventArgs : EventArgs
    {
        public ProductTypesChangedEventArgs(int categoryId, IEnumerable<ProductType> productTypes)
        {
            CategoryId = categoryId;
            ProductTypes = productTypes;
        }

        public int CategoryId { get; init; }

        public IEnumerable<ProductType> ProductTypes { get; init;}
    }
}
