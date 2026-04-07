namespace Telemart.Client.FiscalRegistrar.Requests
{
    public class PaymentItem
    {
        public PaymentItem(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }
    }
}
