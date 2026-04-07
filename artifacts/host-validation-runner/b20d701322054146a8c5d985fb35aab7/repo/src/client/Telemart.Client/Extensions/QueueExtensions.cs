using System.Collections.Generic;

namespace Telemart.Client.Extensions
{
    public static class QueueExtensions
    {
        public static IEnumerable<T> TryDequeueChank<T>(this Queue<T> queue, int size)
        {
            for (int i = 0; i < size; i++)
            {
                if (queue.Count == 0)
                {
                    yield break;
                }

                yield return queue.Dequeue();
            }
        }
    }
}
