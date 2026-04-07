using System.ComponentModel.DataAnnotations;

namespace Telemart.Client.Data.Options
{
    public class CronicleOptions
    {
        [Url]
        public string BaseAddress { get; set; }
    }
}