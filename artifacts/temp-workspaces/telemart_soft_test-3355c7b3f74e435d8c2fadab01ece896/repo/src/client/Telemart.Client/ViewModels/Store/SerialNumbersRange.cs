using System;
using System.Collections.Generic;
using System.Linq;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class SerialNumbersRange
    {
        public SerialNumbersRange(string sn1, string sn2)
        {
            if (sn1 == null)
            {
                throw new ArgumentNullException(nameof(sn1));
            }

            if (sn2 == null)
            {
                throw new ArgumentNullException(nameof(sn2));
            }

            if (sn1.Length != sn2.Length)
            {
                throw new InvalidOperationException();
            }

            SerialNumber1 = sn1;
            SerialNumberN = sn2;

            Range = GenerateRange().ToArray();
        }

        public string SerialNumber1 { get; }

        public string SerialNumberN { get; }

        public string[] Range { get; }

        private static IEnumerable<char> GetCommonChars(string s1, string s2)
        {
            for (int i = 0; i < s1.Length; i++)
            {
                if (s1[i] == s2[i])
                {
                    yield return s1[i];
                }
                else
                {
                    break;
                }
            }
        }

        private IEnumerable<string> GenerateRange()
        {
            if (string.Equals(SerialNumber1, SerialNumberN, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            string common = new string(GetCommonChars(SerialNumber1, SerialNumberN).ToArray());

            if (common.Length > 0)
            {
                string nonCommonSerialNumber1 = SerialNumber1.Replace(common, string.Empty);
                string nonCommonSerialNumberN = SerialNumberN.Replace(common, string.Empty);

                if (int.TryParse(nonCommonSerialNumber1, out int start) && int.TryParse(nonCommonSerialNumberN, out int end))
                {
                    string format = new string(Enumerable.Range(1, nonCommonSerialNumberN.Length).Select(i => '0').ToArray());

                    for (int i = start; i <= end; i++)
                    {
                        yield return $"{common}{i.ToString(format)}";
                    }
                }
            }
        }
    }
}