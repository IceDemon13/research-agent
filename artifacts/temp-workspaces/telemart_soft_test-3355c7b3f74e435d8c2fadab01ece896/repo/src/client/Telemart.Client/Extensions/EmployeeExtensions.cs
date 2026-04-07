using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Extensions
{
    public static class EmployeeExtensions
    {
        public static bool HasAnyRole(this EmployeeDto employee, params Role[] roles)
        {
            return HasAnyRoleInternal(employee, roles);
        }

        private static bool HasAnyRoleInternal(EmployeeDto employee, IEnumerable<Role> roles)
        {
            if (employee == null)
            {
                return false;
            }

            HashSet<string> hashSet = new HashSet<string>(roles.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
            return employee.Roles.Any(x => hashSet.Contains(x));
        }
    }
}