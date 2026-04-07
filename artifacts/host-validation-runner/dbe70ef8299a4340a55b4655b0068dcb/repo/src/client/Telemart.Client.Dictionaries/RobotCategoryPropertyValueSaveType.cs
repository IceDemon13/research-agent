namespace Telemart.Client.Dictionaries
{
    public sealed class RobotCategoryPropertyValueSaveType : DictionaryItem
    {
        public RobotCategoryPropertyValueSaveType(int id, string name, bool active = true)
            : base(id, name, active)
        {
        }

        public static RobotCategoryPropertyValueSaveType OnlyInCategory { get; } = new RobotCategoryPropertyValueSaveType(1, "Только эта категория");

        public static RobotCategoryPropertyValueSaveType InDescendantsWithSameValue { get; } = new RobotCategoryPropertyValueSaveType(2, "Эта категория и все дети с таким же значением");

        public static RobotCategoryPropertyValueSaveType InAllDescendants { get; } = new RobotCategoryPropertyValueSaveType(3, "Эта категория и все дети");

        public static RobotCategoryPropertyValueSaveType EmployeesCategories { get; } = new RobotCategoryPropertyValueSaveType(4, "Категории, где я ответственный");

        public static RobotCategoryPropertyValueSaveType AllCategories { get; } = new RobotCategoryPropertyValueSaveType(5, "Все категории");
    }
}