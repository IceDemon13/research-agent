using System.Windows;
using System.Windows.Documents;

namespace Telemart.Client.ThirdParty.FlowDocuments
{
    public sealed class BindableRun : Run
    {
        public static readonly DependencyProperty BoundTextProperty = DependencyProperty.Register(
            nameof(BoundText),
            typeof(string),
            typeof(BindableRun),
            new PropertyMetadata(OnBoundTextChanged));

        public BindableRun()
        {
            Helpers.FixupDataContext(this);
        }

        public string BoundText
        {
            get => (string)GetValue(BoundTextProperty);
            set => SetValue(BoundTextProperty, value);
        }

        private static void OnBoundTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Run run = (Run)d;

            run.Text = (string)e.NewValue;
        }
    }
}
