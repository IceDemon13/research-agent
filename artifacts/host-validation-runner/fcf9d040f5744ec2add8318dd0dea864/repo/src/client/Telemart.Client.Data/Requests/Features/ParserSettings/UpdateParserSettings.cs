using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ParserSettings;

namespace Telemart.Client.Data.Requests.Features.ParserSettings
{
    public sealed class UpdateParserSettings : UpdateEntityResultRequestBase<ParserSettingsDto, ParserSettingsDto>
    {
        public UpdateParserSettings(int id, ParserSettingsDto dto)
            : base(dto, ApiResources.ParserSettings, id)
        {
        }
    }
}
