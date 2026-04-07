namespace Telemart.Client.ViewModels.PromoCode
{
    public class PromoCodeSetShowInSiteParameter
    {
        public PromoCodeSetShowInSiteParameter(int id, bool showInSite)
        {
            Id = id;
            ShowInSite = showInSite;
        }

        public int Id { get; }

        public bool ShowInSite { get; }
    }
}
