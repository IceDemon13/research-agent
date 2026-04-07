using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ParserSettings;

namespace Telemart.Client.Data.Requests.Features.ParserSettings.Actions
{
    public sealed class LockParserSettings : LockRequestBase<ParserSettingsDto>
    {
        public LockParserSettings(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.ParserSettings, id)
        {
        }
    }
}
