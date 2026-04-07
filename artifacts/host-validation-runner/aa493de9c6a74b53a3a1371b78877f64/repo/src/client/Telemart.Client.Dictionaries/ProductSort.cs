using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Telemart.Client.Dictionaries
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProductSort
    {
        [EnumMember(Value = "")]
        None,
        [EnumMember(Value = "price")]
        PriceAsc,
        [EnumMember(Value = "-price")]
        PriceDesc,
        [EnumMember(Value = "name")]
        NameAsc,
        [EnumMember(Value = "-name")]
        NameDesc,
        [EnumMember(Value = "date")]
        CreatedOnAsc,
        [EnumMember(Value = "-date")]
        CreatedOnDesc,
        [EnumMember(Value = "ids")]
        Ids,
        [EnumMember(Value = "id")]
        IdAsc,
        [EnumMember(Value = "-id")]
        IdDesc,
        [EnumMember(Value = "full_ru")]
        NameFullAsc,
        [EnumMember(Value = "-full_ru")]
        NameFullDesc,
        [EnumMember(Value = "quantity_free")]
        WarehouseQuantityFreeAsc,
        [EnumMember(Value = "-quantity_free")]
        WarehouseQuantityFreeDesc,
        [EnumMember(Value = "price1")]
        Price1Asc,
        [EnumMember(Value = "-price1")]
        Price1Desc
    }
}