namespace Telemart.Client.ViewModels.Common
{
    public sealed class DelayedConfirmParameter
    {
        public DelayedConfirmParameter(string text, int totalSecondsToWait)
        {
            Text = text;
            TotalSecondsToWait = totalSecondsToWait;
        }

        public string Text { get; }

        public int TotalSecondsToWait { get; }
    }
}