namespace Telemart.Client.Common.MvvmEnhancements
{
    internal sealed class RowDoubleClickInfo
    {
        public RowDoubleClickInfo(string fieldName, object data)
        {
            FieldName = fieldName;
            Data = data;
        }

        public object Data { get; }

        public string FieldName { get; }
    }
}