using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telemart.Client.TransferObjects.Paging;

namespace Telemart.Client.Extensions
{
    public static class TaskExtensions
    {
        public static async Task<List<T>> GetPagedResultDataAsync<T>(this Task<PagedResult<T>> taskWithResult)
            where T : new()
        {
            PagedResult<T> pagedResult = await taskWithResult;

            return pagedResult.Data;
        }

        public static async Task CatchAll(this Task task, Action<Exception> handler = null)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                handler?.Invoke(ex);
            }
        }
    }
}