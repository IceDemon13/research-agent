namespace Telemart.Client.ViewModels.Showcase
{
    public sealed class AutoShowcasesHistoryParameter
    {
        public AutoShowcasesHistoryParameter(int productId)
        {
            ProductId = productId;
        }

        public int ProductId { get; }
    }
}