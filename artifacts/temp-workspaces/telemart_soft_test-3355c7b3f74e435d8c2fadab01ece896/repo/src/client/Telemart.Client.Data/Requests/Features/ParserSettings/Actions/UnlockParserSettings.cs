using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ParserSettings;

namespace Telemart.Client.Data.Requests.Features.ParserSettings.Actions
{
    public sealed class UnlockParserSettings : UnlockRequestBase<ParserSettingsDto>
    {
        public UnlockParserSettings(int id, bool force = false)
            : base(force, ApiResources.ParserSettings, id)
        {
        }
    }
}
