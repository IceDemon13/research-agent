namespace Telemart.Client.Common
{
    public interface IProductItem : IUniqueItem
    {
        int CategoryId { get; }

        string ColorPrimary { get; }

        string ColorSecondary { get; }
    }
}
