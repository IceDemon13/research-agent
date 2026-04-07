using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using ArpLookup;

namespace Telemart.Client.Extensions
{
    public static class ArpExtensions
    {
        public static string GetMacByIp(this IPAddress ip)
        {
            PhysicalAddress result = Arp.Lookup(ip);

            string macRaw = result?.ToString();

            if (string.IsNullOrEmpty(macRaw))
            {
                return null;
            }

            return string.Join(
                '-',
                macRaw
                    .Select((x, i) => (x, i))
                    .GroupBy(x => x.i / 2)
                    .Select(x => new string(x.Select(y => y.x).ToArray())));
        }
    }
}