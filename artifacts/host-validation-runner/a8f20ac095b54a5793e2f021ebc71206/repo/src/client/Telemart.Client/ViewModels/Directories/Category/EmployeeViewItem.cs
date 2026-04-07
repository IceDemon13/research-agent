namespace Telemart.Client.ViewModels.Directories.Category
{
    public sealed class EmployeeViewItem
    {
        public EmployeeViewItem(int? id, string name)
        {
            Id = id;
            Name = name;
        }

        public int? Id { get; }

        public string Name { get; }
    }
}