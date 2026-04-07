namespace Telemart.Client.Common
{
    public interface ICheckableTreeItem
    {
        int Id { get; }

        int? ParentId { get; }

        bool? Checked { get; set; }
    }
}
