using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Helpers
{
    public interface ICallHelper
    {
        Task CreateCallByOrderAsync(OrderDto order, int callTypeId, Priority priority, string titleGetContent,  string contentDefault, ISupportServices parent);
    }
}