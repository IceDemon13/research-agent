namespace Telemart.Client.Common.Messages
{
    public class RefundUpdateRequisitesMessage
    {
        public RefundUpdateRequisitesMessage(
            string firstName,
            string lastName,
            string middleName,
            string inn,
            string iban,
            string cardNumber,
            int documentId)
        {
            FirstName = firstName;
            LastName = lastName;
            MiddleName = middleName;
            Inn = inn;
            Iban = iban;
            CardNumber = cardNumber;
            DocumentId = documentId;
        }

        public int DocumentId { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public string MiddleName { get; }

        public string Inn { get; }

        public string Iban { get; }

        public string CardNumber { get; }
    }
}