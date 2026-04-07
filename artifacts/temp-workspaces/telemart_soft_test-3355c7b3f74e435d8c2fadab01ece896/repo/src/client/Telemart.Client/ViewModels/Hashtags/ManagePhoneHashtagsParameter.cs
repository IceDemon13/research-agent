namespace Telemart.Client.ViewModels.Hashtags
{
    public class ManagePhoneHashtagsParameter
    {
        public ManagePhoneHashtagsParameter(string phone)
        {
            Phone = phone;
        }

        public string Phone { get; set; }
    }
}
