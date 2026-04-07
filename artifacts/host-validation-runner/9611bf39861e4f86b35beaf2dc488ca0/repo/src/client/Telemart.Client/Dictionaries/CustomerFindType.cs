namespace Telemart.Client.Dictionaries
{
    public class CustomerFindType : DictionaryItem
    {
        private const int PhoneId = 1;
        private const int EmailId = 2;

        public CustomerFindType(int id, string name)
            : base(id, name, true)
        {
        }

        public static CustomerFindType Phone { get; } = new CustomerFindType(PhoneId, "Номер телефона");

        public static CustomerFindType Email { get; } = new CustomerFindType(EmailId, "Email");
    }
}
