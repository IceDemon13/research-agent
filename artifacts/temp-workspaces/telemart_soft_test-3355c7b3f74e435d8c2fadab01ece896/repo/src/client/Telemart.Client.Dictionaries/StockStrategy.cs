using System.Runtime.Serialization;

namespace Telemart.Client.Dictionaries
{
    public enum StockStrategy
    {
        [EnumMember(Value = "none")]
        None = 0,
        
        [EnumMember(Value = "1c")]
        AccountingSystem = 1,

        [EnumMember(Value = "db")]
        Database = 2,

        [EnumMember(Value = "all")]
        Full = 3
    }
}