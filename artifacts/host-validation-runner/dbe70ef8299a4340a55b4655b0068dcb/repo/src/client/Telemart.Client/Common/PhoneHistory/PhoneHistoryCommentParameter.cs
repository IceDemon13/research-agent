namespace Telemart.Client.Common.PhoneHistory
{
    public sealed class PhoneHistoryCommentParameter
    {
        public PhoneHistoryCommentParameter(string name, int? documentNumber, int? typeId)
        {
            Name = name;
            DocumentNumber = documentNumber;
            TypeId = typeId;
        }

        public string Name { get; }

        public int? DocumentNumber { get; }

        public int? TypeId { get; }
    }
}