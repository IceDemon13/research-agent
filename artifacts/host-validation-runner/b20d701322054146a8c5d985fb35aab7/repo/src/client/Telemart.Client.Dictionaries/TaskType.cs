namespace Telemart.Client.Dictionaries
{
    public class TaskType : DictionaryItem
    {
        private const int ProductResolutionId = 1;
        private const int MoveProductId = 2;
        private const int PrintTagsId = 3;
        public const int ProductPickupResolutionId = 4;
        private const int UnpackOrderId = 5;
        private const int UnpackAndCancelOrderId = 6;
        private const int CancelOrderId = 8;

        private TaskType(int id, string name, Priority priority)
            : base(id, name, true)
        {
            Priority = priority;
        }

        public static TaskType ProductResolution { get; } = new TaskType(ProductResolutionId, "Решение по товару", Priority.Normal);

        public static TaskType MoveProduct { get; } = new TaskType(MoveProductId, "Переместить товар", Priority.High);

        public static TaskType PrintTags { get; } = new TaskType(PrintTagsId, "Напечатать ценники", Priority.Normal);

        public static TaskType ProductPickupResolution { get; } = new TaskType(ProductPickupResolutionId, "Решение по товару на самовывозе",  Priority.High);

        public static TaskType UnpackOrder { get; } = new TaskType(UnpackOrderId, "Распаковать заказ",  Priority.High);

        public static TaskType UnpackAndCancelOrder { get; } = new TaskType(UnpackAndCancelOrderId, "Распаковать и отменить заказ",  Priority.High);

        public static TaskType CancelOrder { get; } = new TaskType(CancelOrderId, "Отмена заказа", Priority.High);

        public Priority Priority { get; }
    }
}