using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Telemart.Client.Data.Requests.Features.Catalog.TransferObjects
{
    public class ComplectationsGeneratorDto
    {
        public IReadOnlyCollection<ComplectationGeneratorDto> Complectations { get; set; }

        public JArray DebugData { get; set; }
    }
}