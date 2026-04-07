using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public class ViolatorsRrpProduct
    {
        public ViolatorsRrpProduct(int id, string name, IReadOnlyCollection<ViolatorsRrpContractorPrice> rrpPrices, IReadOnlyCollection<ViolatorsRrpContractorPrice> competitorPrices)
        {
            Id = id;
            Name = name;
            RrpPrices = rrpPrices;
            CompetitorPrices = competitorPrices;
        }

        public int Id { get; }

        public string Name { get; }

        public IReadOnlyCollection<ViolatorsRrpContractorPrice> RrpPrices { get; }

        public IReadOnlyCollection<ViolatorsRrpContractorPrice> CompetitorPrices { get; }
    }
}
