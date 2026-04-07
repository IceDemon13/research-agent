namespace Telemart.Client.ViewModels.Dialogs
{
    public class GetPasswordFromUserParameter
    {
        public GetPasswordFromUserParameter(
            string contentCaption,
            string title,
            string regexPattern = null,
            string errorMessage = null,
            string content = null)
        {
            ContentCaption = contentCaption;
            Title = title;
            RegexPattern = regexPattern;
            ErrorMessage = errorMessage;
            DefaultContent = content;
        }

        public string ContentCaption { get; }

        public string ErrorMessage { get; }

        public string RegexPattern { get; }

        public string Title { get; }

        public string DefaultContent { get; }
    }
}
