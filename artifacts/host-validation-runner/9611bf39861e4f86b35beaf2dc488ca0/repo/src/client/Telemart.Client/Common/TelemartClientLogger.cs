using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Update;
using Telemart.Client.Data.Requests.Features.Telegram;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common
{
    public class TelemartClientLogger : ITelemartClientLogger
    {
        private readonly IMessageFacadeService _messageFacadeService;
        private readonly IUpdateManager _updateManager;
        private readonly IWebClient _webClient;
        private readonly ILogger<TelemartClientLogger> _logger;

        public TelemartClientLogger(
            IMessageFacadeService messageFacadeService,
            IUpdateManager updateManager,
            IWebClient webClient,
            ILogger<TelemartClientLogger> logger)
        {
            _messageFacadeService = messageFacadeService;
            _updateManager = updateManager;
            _webClient = webClient;
            _logger = logger;
        }

        public async Task SendLogsAsync(string problem, bool automaticSend = false, string link = null)
        {
            await SendLogsAsync(problem, string.Empty, automaticSend, link);
        }

        public async Task SendLogsAsync(string problem, string fileSuffix, bool automaticSend = false, string link = null)
        {
            const string errorRu = "Ошибка при отправке логов";

            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                FileSystemInfo[] fileSystemInfo =
                    new DirectoryInfo($"{appDataPath}\\telemart.client\\logs").GetFileSystemInfos();

                string fileNamePrefix = string.Concat("log", string.IsNullOrWhiteSpace(fileSuffix) ? string.Empty : $"_{fileSuffix}", "-");

                FileSystemInfo fileInfo = fileSystemInfo
                    .Where(x => x.Name.Contains(fileNamePrefix))
                    .OrderByDescending(x => x.Name == $"{fileNamePrefix}{DateTime.Today:yyyyMMdd}")
                    .ThenByDescending(x => x.LastWriteTime)
                    .FirstOrDefault();

                if (fileInfo is null)
                {
                    _messageFacadeService.ShowNotificationWarning("Файл с логами не найден");
                    return;
                }

                await using FileStream fileStream = new FileStream(
                    fileInfo.FullName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

                byte[] array = new byte[fileStream.Length];

                await fileStream.ReadAsync(array, 0, array.Length);

                StringBuilder caption = new();

                caption.AppendLine(_webClient.AuthenticatedEmployee.Name);

                if (!string.IsNullOrEmpty(_webClient.AuthenticatedEmployee.Phone1))
                {
                    caption.AppendLine(_webClient.AuthenticatedEmployee.Phone1);
                }

                caption.AppendLine($"Telemart.Client {_updateManager.GetCurrentVersion()}");
                caption.AppendLine($"Проблема: {problem}");

                if (!string.IsNullOrWhiteSpace(link))
                {
                    caption.AppendLine(link);
                }

                if (automaticSend)
                {
                    caption.AppendLine("[Отправлено автоматически]");
                }

                TelegramBotSendDocumentDto dto = new TelegramBotSendDocumentDto(
                    array,
                    Path.GetFileName(fileInfo.FullName),
                    caption.ToString(),
                    false);

                await _webClient.ExecuteTelegramApiRequestAsync(new SupportBotSendDocument(dto));

                _messageFacadeService.ShowNotificationInfo("Логи успешно отправлены");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while sending logs");
                _messageFacadeService.ShowNotificationError(errorRu);
            }
        }
    }
}