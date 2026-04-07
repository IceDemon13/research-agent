using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record UpdateCommentValidationDto
    {
        public UpdateCommentValidationDto(bool newValue)
        {
            NewValue = newValue;
        }

        [JsonProperty("new_value")]
        public bool NewValue { get; }
    }
}