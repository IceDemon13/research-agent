using DevExpress.Mvvm;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.Common.Layouts
{
    public interface IFilterModuleLayoutService<TFilteringItem> : IModuleLayoutService
    where TFilteringItem : FilteringItemBase
    {
        void Init(int moduleId, ISupportServices supportServices, IFilteringViewModel<TFilteringItem> filteringViewModel);
    }
}