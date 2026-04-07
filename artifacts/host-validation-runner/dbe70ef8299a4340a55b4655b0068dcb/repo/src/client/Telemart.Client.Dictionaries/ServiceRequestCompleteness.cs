namespace Telemart.Client.Dictionaries
{
    public sealed class ServiceRequestCompleteness : DictionaryItem
    {
        private ServiceRequestCompleteness(int id, string name)
            : base(id, name, true)
        {
        }

        public static ServiceRequestCompleteness All { get; } = new ServiceRequestCompleteness(1, "Вся");

        public static ServiceRequestCompleteness NotAll { get; } = new ServiceRequestCompleteness(2, "Не вся");

        public static ServiceRequestCompleteness ProductOnly { get; } = new ServiceRequestCompleteness(3, "Только изделие");
    }
}