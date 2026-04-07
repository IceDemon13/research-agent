using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Helpers
{
    public interface IRroPrintHelper
    {
        Task<Result> SentCheckAsync(string fiscalId, string phone, string email, int cashboxId, ISupportServices parent, bool? showView = null);
    }
}