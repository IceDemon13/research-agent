using System.ComponentModel.DataAnnotations;

namespace Telemart.Client.Data.Options
{
    public sealed class PrintRroOptions
    {
        [Required]
        [Range(1, 10000)]
        public int DelaySeconds { get; set; }

        [Required]
        [Range(1, 100)]
        public int Retries { get; set; }
    }
}