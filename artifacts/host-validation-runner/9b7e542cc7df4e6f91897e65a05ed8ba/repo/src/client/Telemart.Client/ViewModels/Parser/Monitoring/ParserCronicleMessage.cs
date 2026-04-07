namespace Telemart.Client.ViewModels.Parser.Monitoring
{
    public sealed class ParserCronicleMessage
    {
        public ParserCronicleMessage(bool logoutClient)
        {
            LogoutClient = logoutClient;
        }

        public bool LogoutClient { get; }
    }
}