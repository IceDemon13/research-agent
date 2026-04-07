namespace Telemart.Client.FiscalRegistrar.Requests
{
    public sealed class PrintChequeProductItem
    {
        public PrintChequeProductItem(int id, string name, decimal price, int quantity)
        {
            Id = id;
            Name = name;
            Price = price;
            Quantity = quantity;
        }

        public int Id { get; }

        public string Name { get; }

        public decimal Price { get; }

        public int Quantity { get; }
    }
}