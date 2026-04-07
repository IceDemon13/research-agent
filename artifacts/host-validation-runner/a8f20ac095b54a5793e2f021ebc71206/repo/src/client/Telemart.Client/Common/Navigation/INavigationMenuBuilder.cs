using System.Collections.Generic;
using Telemart.Client.Data.WebClient.Security;

namespace Telemart.Client.Common.Navigation
{
    public interface INavigationMenuBuilder
    {
        IEnumerable<NavigationMenuGroup> BuildNavigationMenu(
            IReadOnlyCollection<string> roles,
            IReadOnlyCollection<BusinessOperation> operations);
    }
}