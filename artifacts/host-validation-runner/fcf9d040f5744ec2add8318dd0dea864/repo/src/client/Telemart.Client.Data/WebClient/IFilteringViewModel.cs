namespace Telemart.Client.Data.WebClient
{
    public interface IFilteringViewModel<TFilteringItem>
    where TFilteringItem : FilteringItemBase
    {
        TFilteringItem GetFilteringItem();

        void SetFilteringItem(TFilteringItem filteringItem);
    }
}