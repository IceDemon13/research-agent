namespace Telemart.Client.Common.Messages
{
    public class WikiHelpMessage
    {
        public WikiHelpMessage(string title, string relativeUrl)
        {
            Title = title;
            RelativeUrl = relativeUrl;
        }

        public string Title { get; }

        public string RelativeUrl { get; }
    }
}
