namespace Telemart.Client.Dictionaries
{
    public sealed class MovementState : DictionaryItem
    {
        public const int NewId = 1;
        public const int LeftId = 2;
        public const int ReceivedId = 3;
        public const int ArrivedId = 4;
        public const int CancelledId = 5;

        private MovementState(int id, string name, bool active = true)
            : base(id, name, active)
        {
        }

        /// <summary>
        /// Gets the new (1)
        /// </summary>
        /// <value>
        /// The new.
        /// </value>
        public static MovementState New { get; } = new MovementState(NewId, "Новое");

        /// <summary>
        /// Gets the left (2).
        /// </summary>
        /// <value>
        /// The left.
        /// </value>
        public static MovementState Left { get; } = new MovementState(LeftId, "Уехало");

        /// <summary>
        /// Gets the received (3).
        /// </summary>
        /// <value>
        /// The received.
        /// </value>
        public static MovementState Received { get; } = new MovementState(ReceivedId, "Принято");

        /// <summary>
        /// Gets the arrived (4).
        /// </summary>
        /// <value>
        /// The arrived.
        /// </value>
        public static MovementState Arrived { get; } = new MovementState(ArrivedId, "Приехало");

        /// <summary>
        /// Gets the cancelled (5).
        /// </summary>
        /// <value>
        /// The cancelled.
        /// </value>
        public static MovementState Cancelled { get; } = new MovementState(CancelledId, "Отменено");
    }
}