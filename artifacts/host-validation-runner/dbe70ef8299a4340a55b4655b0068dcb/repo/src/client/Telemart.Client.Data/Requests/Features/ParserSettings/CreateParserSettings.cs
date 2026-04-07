using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ParserSettings;

namespace Telemart.Client.Data.Requests.Features.ParserSettings
{
    public sealed class CreateParserSettings : CreateEntityResultRequestBase<ParserSettingsDto, ParserSettingsDto>
    {
        public CreateParserSettings(ParserSettingsDto dto)
            : base(dto, ApiResources.ParserSettings)
        {
        }
    }
}
