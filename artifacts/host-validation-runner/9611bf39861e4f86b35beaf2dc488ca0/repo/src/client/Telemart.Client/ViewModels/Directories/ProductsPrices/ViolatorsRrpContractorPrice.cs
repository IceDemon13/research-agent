namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public class ViolatorsRrpContractorPrice
    {
        public ViolatorsRrpContractorPrice(int id, decimal price)
        {
            Id = id;
            Price = price;
        }

        public int Id { get; }

        public decimal Price { get; }
    }
}
