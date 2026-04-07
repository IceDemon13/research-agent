using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store.Call
{
    public sealed class CreateCallParameter
    {
        public CreateCallParameter(
            CallDocumentType documentType,
            int? orderId,
            int? serviceRquestId,
            int? subdivisionId,
            int? contractorId,
            string fio,
            string phone,
            string phone2)
        {
            DocumentType = documentType;
            OrderId = orderId;
            ServiceRquestId = serviceRquestId;
            SubdivisionId = subdivisionId;
            ContractorId = contractorId;
            Fio = fio;
            Phone = phone;
            Phone2 = phone2;

            Priority = Priority.Normal;
        }

        public static CreateCallParameter Empty { get; } = new CreateCallParameter(
            CallDocumentType.None,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        public CallDocumentType DocumentType { get; }

        public int? OrderId { get; }

        public int? ServiceRquestId { get; }

        public int? SubdivisionId { get; }

        public int? ContractorId { get; }

        public string Fio { get; }

        public string Phone { get; }

        public string Phone2 { get; }

        public Priority Priority { get; private set; }

        public CreateCallParameter WithPriority(Priority priority)
        {
            Priority = priority;
            return this;
        }
    }
}
