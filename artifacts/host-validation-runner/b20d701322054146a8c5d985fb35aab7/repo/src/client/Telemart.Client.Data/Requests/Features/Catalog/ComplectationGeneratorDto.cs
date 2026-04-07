using System.Collections.Generic;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public class ComplectationGeneratorDto
    {
        public int Id { get; set; }

        public IReadOnlyCollection<ProductComplectationDto> Complectation { get; set; }
    }
}