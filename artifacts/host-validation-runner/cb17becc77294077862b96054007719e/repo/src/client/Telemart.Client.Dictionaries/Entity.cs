namespace Telemart.Client.Dictionaries
{
    public sealed class Entity : DictionaryItemBase
    {
        public const int ServiceRequestId = 1;
        public const int CallId = 2;
        public const int TradeInId = 3;
        public const int InvoiceId = 4;
        public const int ProductId = 5;
        public const int OrderId = 6;
        public const int ContractorId = 7;
        public const int ParserSettingsId = 8;
        public const int MovementId = 9;
        public const int AssemblyServiceId = 10;
        public const int AdditionalServiceProductId = 11;
        public const int ReturnInvoiceId = 12;
        public const int CashboxId = 13;
        public const int WarehouseId = 14;
        public const int RefundId = 15;
        public const int SupplierBillId = 16;
        public const int DiscussionId = 19;
        public const int ExternalPaymentId = 22;
        public const int ServiceMovementId = 23;
        public const int ServiceInvoiceId = 24;

        public Entity(int id, string name, string displayName, string[] discussionViewModels, bool logistic)
            : base(id, name)
        {
            DisplayName = displayName;
            DiscussionViewModels = discussionViewModels;
            Logistic = logistic;
        }

        public string DisplayName { get; }

        public string[] DiscussionViewModels { get; }

        public bool Logistic { get; }
    }
}