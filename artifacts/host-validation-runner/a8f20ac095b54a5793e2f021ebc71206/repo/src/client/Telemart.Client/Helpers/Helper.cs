using Telemart.Client.Dictionaries;

namespace Telemart.Client.Helpers
{
    public static class Helper
    {
        public static T GetMultiLanguageValue<T>(T ruValue, T ukrValue, int languageId, T defaultValue = default)
        {
            switch (languageId)
            {
                case Language.RussianId:
                    return ruValue;
                case Language.UkrainianId:
                    return ukrValue;
                default:
                    return defaultValue;
            }
        }
    }
}
