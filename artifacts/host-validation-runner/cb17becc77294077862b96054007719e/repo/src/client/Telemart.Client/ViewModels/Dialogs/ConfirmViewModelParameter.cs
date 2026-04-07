namespace Telemart.Client.ViewModels.Dialogs
{
    public sealed class ConfirmViewModelParameter
    {
        public ConfirmViewModelParameter(string text, string title)
        {
            Text = text;
            Title = title;
        }

        public string Text { get; }

        public string Title { get; }
    }
}