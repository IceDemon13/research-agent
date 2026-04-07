using DevExpress.Mvvm;

namespace Telemart.Client.Controls.Accordion
{
    public class RangeAccordionItem : BindableBase, IAccordionItem
    {
        public RangeAccordionItem(double minimum, double maximum, double step, string title, bool isExpanded, string name = null)
        {
            Minimum = minimum;
            Maximum = maximum;
            Step = step;
            Title = title;
            IsExpanded = isExpanded;
            Name = name;
        }

        public decimal? SelectionMinimum
        {
            get { return GetProperty(() => SelectionMinimum); }
            set { SetProperty(() => SelectionMinimum, value); }
        }

        public decimal? SelectionMaximum
        {
            get { return GetProperty(() => SelectionMaximum); }
            set { SetProperty(() => SelectionMaximum, value); }
        }

        public double Minimum { get; }

        public double Maximum { get; }

        public double Step { get; }

        public string Title { get; }

        public string Name { get; }

        public bool IsExpanded { get; }

        public void Cancel()
        {
            SelectionMinimum = null;
            SelectionMaximum = null;
        }
    }
}