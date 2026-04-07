namespace Telemart.Client.Controls.Accordion
{
    public interface IAccordionItem
    {
        string Title { get; }

        string Name { get; }

        bool IsExpanded { get; }

        void Cancel();
    }
}
