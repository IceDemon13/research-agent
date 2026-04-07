namespace Telemart.Client.ViewModels.Dialogs
{
    public sealed class GetTextFromUserParameter
    {
        public GetTextFromUserParameter(
            string contentCaption,
            string title,
            string regexPattern = null,
            string errorMessage = null,
            string content = null,
            bool isMultiline = false,
            string contentMask = null,
            bool required = true)
        {
            ContentCaption = contentCaption;
            Title = title;
            RegexPattern = regexPattern;
            ErrorMessage = errorMessage;
            DefaultContent = content;
            IsMultiline = isMultiline;
            ContentMask = contentMask;
            Required = required;
        }

        public string ContentCaption { get; }

        public string ErrorMessage { get; }

        public string RegexPattern { get; }

        public string Title { get; }

        public string DefaultContent { get; }

        public string ContentMask { get; }

        public bool IsMultiline { get; }

        public bool Required { get; }
    }
}