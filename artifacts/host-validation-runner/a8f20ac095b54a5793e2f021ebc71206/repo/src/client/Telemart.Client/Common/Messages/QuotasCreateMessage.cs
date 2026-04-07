using Telemart.Client.TransferObjects.Quotas;

namespace Telemart.Client.Common.Messages
{
    public sealed class QuotasCreateMessage
    {
        public QuotasCreateMessage(QuotaDto[] quotas, MessageType messageType)
        {
            Quotas = quotas;
            MessageType = messageType;
        }

        public QuotaDto[] Quotas { get; }

        public MessageType MessageType { get; }
    }
}