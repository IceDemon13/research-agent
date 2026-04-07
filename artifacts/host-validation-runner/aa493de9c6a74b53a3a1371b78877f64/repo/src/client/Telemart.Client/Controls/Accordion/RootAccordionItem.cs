using System.Collections.Generic;
using DevExpress.Mvvm.Native;

namespace Telemart.Client.Controls.Accordion
{
    public class RootAccordionItem
    {
        public RootAccordionItem(IAccordionItem item)
        {
            Children = new[] { item };
            Item = item;
        }

        public IReadOnlyCollection<IAccordionItem> Children { get; }

        public IAccordionItem Item { get; }

        public void Cancel()
        {
            Children.ForEach(x => x.Cancel());
        }
    }
}
