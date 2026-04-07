using System;
using System.Threading.Tasks;
using Telemart.Client.TransferObjects.Customer;

namespace Telemart.Client.ViewModels.Common
{
    public class SetCustomerParameter
    {
        public SetCustomerParameter(string title, Func<CustomerDto, Task<bool>> okCommand)
        {
            OkCommand = okCommand;
            Title = title;
        }

        public Func<CustomerDto, Task<bool>> OkCommand { get; }

        public string Title { get; }
    }
}
