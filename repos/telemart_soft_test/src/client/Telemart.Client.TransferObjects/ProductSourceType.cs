using System.Runtime.Serialization;

namespace Telemart.Client.TransferObjects
{
    public enum ProductSourceType
    {
        [EnumMember(Value = "warehouse")]
        Warehouse = 1,

        [EnumMember(Value = "transit")]
        Transit = 2,

        [EnumMember(Value = "invoice")]
        Invoice = 3
    }
}