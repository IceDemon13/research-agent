namespace Telemart.Client.Dictionaries
{
    public class ServiceRequestDiscussionState : DictionaryItem
    {
        private const int NoMessageId = 1;
        private const int ManagerMessageId = 2;
        private const int ServiceMessageId = 3;

        private ServiceRequestDiscussionState(int id, string name)
            : base(id, name, true)
        {
        }

        public static ServiceRequestDiscussionState NoMessage { get; } = new ServiceRequestDiscussionState(NoMessageId, "Нет");

        public static ServiceRequestDiscussionState ManagerMessage { get; } = new ServiceRequestDiscussionState(ManagerMessageId, "От менеджера");

        public static ServiceRequestDiscussionState ServiceMessage { get; } = new ServiceRequestDiscussionState(ServiceMessageId, "От сервиса");
    }
}