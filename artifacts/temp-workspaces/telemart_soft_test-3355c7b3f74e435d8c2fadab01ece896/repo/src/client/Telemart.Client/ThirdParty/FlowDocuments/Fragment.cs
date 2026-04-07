using System.Windows;
using System.Windows.Markup;

namespace Telemart.Client.ThirdParty.FlowDocuments
{
    [ContentProperty("Content")]
    public sealed class Fragment : FrameworkElement
    {
        private static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            nameof(Content),
            typeof(FrameworkContentElement),
            typeof(Fragment));

        public FrameworkContentElement Content
        {
            get => (FrameworkContentElement)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }
    }
}
