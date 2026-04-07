using System;
using System.Collections.Generic;

namespace Telemart.Client.Data.WebClient
{
    public interface IFilteringItem
    {
        IEnumerable<(string Name, object Value)> BuildParameters();
    }
}