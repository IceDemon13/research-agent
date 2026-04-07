using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Core.Extensions;

namespace Telemart.Client.Helpers
{
    public sealed class BulkAddTextProcessor : IBulkAddTextProcessor
    {
        public IEnumerable<(string Pattern, int Quantity)> HandleText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                yield break;
            }

            foreach (string line in GetLines(text))
            {
                bool parsed = true;
                string pattern = line.Trim('\uFEFF'); // ZERO WIDTH NO-BREAK SPACE
                int quantity = 1;

                byte w = 0;

                for (int i = line.Length - 1; i >= 0; i--)
                {
                    switch (line[i])
                    {
                        case ' ':
                            w++;
                            break;
                        case '\t':
                            w = 4;
                            break;
                        default:
                            w = 0;
                            break;
                    }

                    if (w >= 3)
                    {
                        pattern = line.Substring(0, i).Trim();
                        parsed = StringExtensions.TryParseInt32(line.Substring(i, line.Length - i).Trim(), out quantity);
                        break;
                    }
                }

                if (parsed)
                {
                    yield return (pattern, Math.Max(1, quantity));
                }
            }
        }

        private static IEnumerable<string> GetLines(string text)
        {
            return text
                .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim());
        }
    }
}
