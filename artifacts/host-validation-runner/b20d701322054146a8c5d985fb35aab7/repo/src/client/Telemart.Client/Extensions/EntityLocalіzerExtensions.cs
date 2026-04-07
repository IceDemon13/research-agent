using Telemart.Client.Dictionaries;
using Telemart.Common.Localization;

namespace Telemart.Client.Extensions
{
    public static class EntityLocalіzerExtensions
    {
        public static string GetLocalName(this ILocalіzableEntity entity, LocalizableNameType localіzableNameType)
        {
            if (entity == null)
            {
                return string.Empty;
            }

            string defaultLocalName = entity.Name;

            string localName = localіzableNameType switch
            {
                LocalizableNameType.Ukr => entity.NameUkr,
                LocalizableNameType.En => entity.NameEn,
                _ => defaultLocalName
            };

            return string.IsNullOrEmpty(localName)
                ? defaultLocalName
                : localName;
        }

        public static string GetLacalString(
            string sourceRu,
            string sourceUkr,
            string sourceEn,
            LocalizableNameType localіzableNameType)
        {
           string defaultLocalName = sourceRu;

           string localName = localіzableNameType switch
            {
                LocalizableNameType.Ukr => sourceUkr,
                LocalizableNameType.En => sourceEn,
                _ => defaultLocalName
            };

           return string.IsNullOrEmpty(localName)
                ? defaultLocalName
                : localName;
        }
    }
}