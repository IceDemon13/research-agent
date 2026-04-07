using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Comment
{
    public class CommentSimpleDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("parent_id")]
        public int? ParentId { get; set; }

        [JsonProperty("entity_id")]
        public int EntityId { get; set; }

        [JsonProperty("document_id")]
        public int? DocumentId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("parent_category_id")]
        public int? ParentCategoryId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("product_name_ukr")]
        public string ProductNameUkr { get; set; }

        [JsonProperty("product_name_en")]
        public string ProductNameEn { get; set; }

        [JsonProperty("product_link")]
        public string ProductLink { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("validation_required")]
        public bool ValidationRequired { get; init; }

        [JsonProperty("change_date")]
        public DateTime? ChangeDate { get; set; }

        [JsonProperty("pro")]
        public string Pro { get; set; }

        [JsonProperty("contra")]
        public string Contra { get; set; }

        [JsonProperty("like")]
        public int Like { get; set; }

        [JsonProperty("dislike")]
        public int Dislike { get; set; }

        [JsonProperty("bought_product")]
        public bool BoughtProduct { get; set; }

        [JsonProperty("avatar_id")]
        public int? AvatarId { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("stars_avg")]
        public decimal? StarsAvg { get; init; }

        [JsonProperty("stars_division_count")]
        public decimal? StarsDivisionCount { get; init; }

        [JsonProperty("product_ratio")]
        public int ProductRatio { get; init; }
    }
}