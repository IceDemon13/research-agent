namespace Telemart.Client.Dictionaries
{
    public class WorkScheduleType : DictionaryItem
    {
        public const int CallSenterTypeId = 1;
        public const int CallSenterServiceTypeId = 2;
        public const int AdditionalServiceTypeId = 3;
        public const int TypeOutsideTypeId = 4;
        public const int TypeForReportTypeId = 5;
        public const int ShopTypeId = 6;
        public const int Supplier = 7;
        public const int Sms = 8;
        public const int Production = 19;

        public WorkScheduleType(int id, string name, int? parentId)
            : base(id, name, true)
        {
            ParentId = parentId;
        }

        public int? ParentId { get; }
    }
}