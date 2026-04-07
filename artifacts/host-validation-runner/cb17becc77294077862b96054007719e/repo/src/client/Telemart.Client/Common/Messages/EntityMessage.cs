namespace Telemart.Client.Common.Messages
{
    public class EntityMessage<T>
    {
        public EntityMessage(T entity, MessageType messageType)
        {
            Entity = entity;
            MessageType = messageType;
        }

        public T Entity { get; }

        public MessageType MessageType { get; }
    }
}