using System.Windows;
using System.Windows.Controls;

namespace Telemart.Client.Controls.Accordion
{
    public class AccordionItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate RangeTemplate { get; set; }

        public DataTemplate CheckedListTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            DataTemplate template;

            switch (item)
            {
                case RangeAccordionItem _:
                    template = RangeTemplate;
                    break;
                case CheckedListAccordionItem _:
                    template = CheckedListTemplate;
                    break;
                default:
                    template = null;
                    break;
            }

            return template;
        }
    }
}