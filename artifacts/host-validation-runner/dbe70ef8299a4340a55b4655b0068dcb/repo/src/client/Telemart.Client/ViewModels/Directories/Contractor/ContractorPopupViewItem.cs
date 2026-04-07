namespace Telemart.Client.ViewModels.Directories.Contractor
{
    public class ContractorPopupViewItem
    {
        public ContractorPopupViewItem(int id, int? parentId, string name, bool isFolder)
        {
            Id = id;
            ParentId = parentId;
            Name = name;
            IsFolder = isFolder;
        }

        public int Id { get; }

        public bool IsFolder { get; set; }

        public string Name { get; }

        public int? ParentId { get; set; }
    }
}