using System.Windows;
using System.Windows.Documents;

namespace Telemart.Client.ThirdParty.FlowDocuments
{
    public sealed class BlockTemplateContent : Section
    {
        private static readonly DependencyProperty TemplateProperty = DependencyProperty.Register(
            nameof(Template),
            typeof(DataTemplate),
            typeof(BlockTemplateContent),
            new PropertyMetadata(OnTemplateChanged));

        public BlockTemplateContent()
        {
            Helpers.FixupDataContext(this);

            Loaded += BlockTemplateContentLoaded;
        }

        public DataTemplate Template
        {
            get => (DataTemplate)GetValue(TemplateProperty);
            set => SetValue(TemplateProperty, value);
        }

        private static void OnTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BlockTemplateContent blockTemplateContent = (BlockTemplateContent)d;

            blockTemplateContent.OnTemplateChanged((DataTemplate)e.NewValue);
        }

        private void BlockTemplateContentLoaded(object sender, RoutedEventArgs e)
        {
            GenerateContent(Template);
        }

        private void GenerateContent(DataTemplate template)
        {
            Blocks.Clear();

            if (template != null)
            {
                FrameworkContentElement element = Helpers.LoadDataTemplate(template);
                Blocks.Add((Block)element);
            }
        }

        private void OnTemplateChanged(DataTemplate dataTemplate)
        {
            GenerateContent(dataTemplate);
        }
    }
}
