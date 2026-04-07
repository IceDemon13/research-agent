using Telemart.Client.Data.Options;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Helpers
{
    public static class DocumentsBotHelper
    {
        public static string GetUrl(int documentId, int entityId, IDictionaries dictionaries, TelegramBotOptions options)
        {
            Entity entity = dictionaries.GetItemById<Entity>(entityId);

            return $"https://t.me/{options.DocumentsBotName}?start={entity.Name}-{documentId}";
        }
    }
}