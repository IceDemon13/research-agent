using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public sealed class QueryComplectationGeneratorOptions : QueryRequestBase<ComplectationGeneratorOptionsDto>
    {
        public QueryComplectationGeneratorOptions()
            : base("assembly_complectations", "complectation_generator_options")
        {
        }
    }
}