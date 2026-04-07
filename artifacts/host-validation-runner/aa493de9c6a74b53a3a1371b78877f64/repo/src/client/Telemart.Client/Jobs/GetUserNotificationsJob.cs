using System;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Quartz;
using Telemart.Client.Common.Messages;
using Telemart.Client.Data.Requests.Features.Task;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Task;
using Telemart.Client.ViewModels.Tasks;

namespace Telemart.Client.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class GetUserNotificationsJob : IJob
    {
        public GetUserNotificationsJob(IWebClient webClient, IMessenger messenger, IDictionaries dictionaries, ILogger<GetUserNotificationsJob> logger)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            Logger = logger;
        }

        private ILogger<GetUserNotificationsJob> Logger { get; }

        private IMessenger Messenger { get; }

        private IWebClient WebClient { get; }

        private IDictionaries Dictionaries { get; }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                TaskFilteringItem filteringItem = new TaskFilteringItem()
                {
                    States = new[] { TaskState.New.Id, TaskState.InProgress.Id }
                };

                PagedResult<TaskDto> tasks = await WebClient.ExecuteApiRequestAsync(new QueryTasks(filteringItem));

                int[] highTaskTypeIds = Dictionaries.GetItems<TaskType>()
                    .Where(x => x.Priority.Weight > Priority.Normal.Weight)
                    .Select(x => x.Id)
                    .ToArray();

                Messenger.Send(new UpdateUserNotificationsMessage(tasks.Data.Count, tasks.Data.Any(x => highTaskTypeIds.Contains(x.TypeId))));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get user notifications");
            }
        }
    }
}