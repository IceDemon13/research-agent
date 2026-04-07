namespace Telemart.Client.ViewModels.RobotProperties
{
    public sealed class RobotCategoryParameter
    {
        public RobotCategoryParameter(int categoryId, string fullName)
        {
            CategoryId = categoryId;
            FullName = fullName;
        }

        public int CategoryId { get; }

        public string FullName { get; }
    }
}