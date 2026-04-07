namespace Telemart.Client.Reports.Order.Assembly
{
    public sealed class AssemblyAdditionalServiceProductData
    {
        public AssemblyAdditionalServiceProductData(
            int id,
            string additionalServiceName,
            string productName,
            string status,
            string priorityType,
            string comment)
        {
            Id = id;
            AdditionalServiceName = additionalServiceName;
            ProductName = productName;
            PriorityType = priorityType;
            Status = status;
            Comment = comment;
        }

        public int Id { get; }

        public string AdditionalServiceName { get; }

        public string ProductName { get; }

        public string Status { get; }

        public string PriorityType { get; }

        public string Comment { get; }
    }
}