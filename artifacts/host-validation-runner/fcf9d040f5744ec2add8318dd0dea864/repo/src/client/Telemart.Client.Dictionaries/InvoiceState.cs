namespace Telemart.Client.Dictionaries
{
    public sealed class InvoiceState : DictionaryItem
    {
        private const int OpenId = 1;
        private const int ClosedId = 2;
        private const int ArrivedId = 3;
        private const int ReceivedId = 4;
        private const int CancelledId = 5;

        private InvoiceState(int id, string name)
            : base(id, name, true)
        {
        }

        /// <summary>
        /// Gets the open invoice state (1).
        /// </summary>
        /// <value>
        /// The open.
        /// </value>
        public static InvoiceState Open { get; } = new InvoiceState(OpenId, "Открыта");

        /// <summary>
        /// Gets the closed invoice state (2).
        /// </summary>
        /// <value>
        /// The closed.
        /// </value>
        public static InvoiceState Closed { get; } = new InvoiceState(ClosedId, "Закрыта");

        /// <summary>
        /// Gets the arrived invoice state (3).
        /// </summary>
        /// <value>
        /// The arrived.
        /// </value>
        public static InvoiceState Arrived { get; } = new InvoiceState(ArrivedId, "Приехала");

        /// <summary>
        /// Gets the received invoice state (4).
        /// </summary>
        /// <value>
        /// The received.
        /// </value>
        public static InvoiceState Received { get; } = new InvoiceState(ReceivedId, "Принята");

        /// <summary>
        /// Gets the cancelled invoice state (5).
        /// </summary>
        /// <value>
        /// The cancelled.
        /// </value>
        public static InvoiceState Cancelled { get; } = new InvoiceState(CancelledId, "Отменена");
    }
}
