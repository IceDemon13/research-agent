using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using Telemart.Client.Data.WebClient.Security;

namespace Telemart.Client.Common.Messages
{
    public sealed class UserLoggedInMessage
    {
        public UserLoggedInMessage(
             int employeeId,
             string employeeName,
             IReadOnlyCollection<string> roles,
             IReadOnlyCollection<BusinessOperation> operations,
             bool isReload = false,
             ReadOnlyCollection<Cookie> cookies = null)
        {
            EmployeeId = employeeId;
            EmployeeName = employeeName;
            Roles = roles;
            Operations = operations;
            IsReload = isReload;
            Cookies = cookies;
        }

        public int EmployeeId { get; }

        public string EmployeeName { get; }

        public IReadOnlyCollection<string> Roles { get; }

        public IReadOnlyCollection<BusinessOperation> Operations { get; }

        public bool IsReload { get; }

        public ReadOnlyCollection<Cookie> Cookies { get; }
    }
}
