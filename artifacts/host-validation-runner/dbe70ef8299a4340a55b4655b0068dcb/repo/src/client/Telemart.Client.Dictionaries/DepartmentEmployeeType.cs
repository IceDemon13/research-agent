namespace Telemart.Client.Dictionaries
{
    public sealed class DepartmentEmployeeType : DictionaryItem
    {
        public const int ManagerId = 1;
        public const int WorkerId = 2;

        public DepartmentEmployeeType( int id, string name)
            : base(id, name, true)
        {
        }

        public static DepartmentEmployeeType Manager { get; } = new DepartmentEmployeeType(ManagerId, "Руководитель");

        public static DepartmentEmployeeType Worker { get; } = new DepartmentEmployeeType(WorkerId, "Сотрудник");
    }
}