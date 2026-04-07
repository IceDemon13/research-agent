using System.ComponentModel.DataAnnotations;

namespace Telemart.Client.Data.Options
{
    public sealed class CatalogServiceOptions : ServiceOptionsBase
    {
        [Range(1, 11)]
        public int? ElasticPrecisionTuning { get; set; }
    }
}