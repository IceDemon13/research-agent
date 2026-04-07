using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class RefundRequisitesDto
    {
        public RefundRequisitesDto(
            string firstName,
            string lastName,
            string middleName,
            string iban,
            string cardNumber,
            string inn)
        {
            FirstName = firstName;
            LastName = lastName;
            MiddleName = middleName;
            Iban = iban;
            Inn = inn;
            CardNumber = cardNumber;
        }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("middle_name")]
        public string MiddleName { get; set; }

        [JsonProperty("iban")]
        public string Iban { get; set; }

        [JsonProperty("card_number")]
        public string CardNumber { get; set; }

        [JsonProperty("inn")]
        public string Inn { get; set; }
    }
}