using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.PromoCode;

namespace Telemart.Client.Data.Requests.Features.PromoCode.Actions
{
    public class SetPromoCodeShowInSite : CallEntityActionWithBodyRequestResultBase<PromoCodeFullDto, SetPromoCodeShowInSite.PromoCodeShowInSiteDto>
    {
        public SetPromoCodeShowInSite(int id, bool showInSite)
            : base(id, new PromoCodeShowInSiteDto(id, showInSite), ApiResources.PromoCodes, "set_show_in_site")
        {
        }

        public class PromoCodeShowInSiteDto
        {
            public PromoCodeShowInSiteDto(int promoCodeId, bool showInSite)
            {
                PromoCodeId = promoCodeId;
                ShowInSite = showInSite;
            }

            [JsonProperty("promo_code_id")]
            public int PromoCodeId { get; set; }

            [JsonProperty("show_in_site")]
            public bool ShowInSite { get; set; }
        }
    }
}
