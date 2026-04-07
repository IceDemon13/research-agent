using System.Collections.Generic;

namespace Telemart.Client.Helpers
{
    public interface IBulkAddTextProcessor
    {
        IEnumerable<(string Pattern, int Quantity)> HandleText(string text);
    }
}