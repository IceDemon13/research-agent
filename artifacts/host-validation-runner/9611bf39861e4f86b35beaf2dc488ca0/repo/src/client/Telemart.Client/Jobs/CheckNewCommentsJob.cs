using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Quartz;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Data.Requests.Features.Comment;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.Comment;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels;
using Telemart.Client.ViewModels.Comment;

namespace Telemart.Client.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class CheckNewCommentsJob : IJob
    {
        private static DateTime? lastCheckTime;

        private readonly IWebClient webClient;
        private readonly IMessenger messenger;

        public CheckNewCommentsJob(
            IWebClient webClient,
            IMessenger messenger,
            ILogger<CheckNewCommentsJob> logger)
        {
            this.webClient = webClient;
            this.messenger = messenger;
            Logger = logger;
        }

        private ILogger<CheckNewCommentsJob> Logger { get; }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                object documentManagerObj = context.MergedJobDataMap["DocumentManagerService"];

                IDocumentManagerService documentManagerService = documentManagerObj as IDocumentManagerService ?? throw new ArgumentNullException(nameof(documentManagerObj));

                if (webClient.AuthenticatedEmployee.HasAnyRole(Role.Admin)
                    || (!webClient.IsOperationAllowed(BusinessOperation.CommentNotify)
                        && documentManagerService.Documents.All(x => (x.Title as ModuleHeader)?.Title != ModuleNameConstants.CommentsModule)))
                {
                    lastCheckTime = null;
                    return;
                }

                CommentFilteringItem filteringItem;

                if (lastCheckTime is null)
                {
                    filteringItem = new()
                    {
                        Take = 1,
                        IncludeChildren = true,
                        CreatedBy = Constants.WebUserEmployee
                    };
                }
                else
                {
                    filteringItem = new()
                    {
                        IncludeChildren = true,
                        CreatedOnFrom = lastCheckTime.Value.AddSeconds(1),
                        CreatedBy = Constants.WebUserEmployee
                    };
                }

                PagedResult<CommentSimpleDto> commentsData = await webClient.ExecuteApiRequestAsync(new QueryComments(filteringItem));

                if (lastCheckTime.HasValue)
                {
                    foreach (CommentSimpleDto comment in commentsData.Data)
                    {
                        Application.Current.Dispatcher.Invoke(() => { messenger.Send(new ShowNewCommentNotificationMessage(comment.Id, "Новый комментарий", comment.Text)); });
                    }
                }

                if (commentsData.Data.Any())
                {
                    lastCheckTime = commentsData.Data.Last().CreatedOn;
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to check new comments");
            }
        }
    }
}