using System;
using System.Threading.Tasks;
using DevExpress.Xpf.Data;

namespace Telemart.Client.Helpers
{
    public static class PagingHelper
    {
        public static void FetchEmptyItems(object sender, FetchPageAsyncEventArgs e)
        {
            e.Result = Task.FromResult(new FetchRowsResult(Array.Empty<object>(), false));
        }
    }
}
