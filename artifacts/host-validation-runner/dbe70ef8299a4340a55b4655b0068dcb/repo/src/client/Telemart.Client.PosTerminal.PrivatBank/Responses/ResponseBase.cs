using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Telemart.Client.PosTerminal.PrivatBank.Responses
{
    public class Response<TParam> where TParam : ResponseItemBase
    {
        private readonly string errorMessage;

        public Response()
        {
        }

        public Response(string errorMessage)
        {
            Error = true;
            this.errorMessage = errorMessage;
        }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("step")]
        public int Step { get; set; }

        [JsonProperty("params")]
        public TParam Parameter { get; set; }

        [JsonProperty("error")]
        public bool Error { get; set; }

        [JsonProperty("errorDescription")]
        public string ErrorDescription { get; set; }

        public bool IsValid()
        {
            return !Error && GetErrorMessages().Any() == false;
        }

        public IEnumerable<string> GetErrorMessages()
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                yield return errorMessage;
            }

            if (!string.IsNullOrWhiteSpace(ErrorDescription))
            {
                yield return ErrorDescription;
            }

            if (!string.IsNullOrWhiteSpace(Parameter?.ResponseCode))
            {
                string errorMsg = GetErrorMesageByCode(Parameter.ResponseCode);

                if (!string.IsNullOrWhiteSpace(errorMsg))
                {
                    yield return errorMsg;
                }
            }
        }

        private string GetErrorMesageByCode(string code)
        {
            switch (code)
            {
                case "1000": return "Неизвестная ошибка";
                case "1001": return "Транзакция отменена пользователем";
                case "1002": return "Отклонено EMV";
                case "1003": return "Журнал транзакций заполнен. Нужно закрыть смену";
                case "1004": return "Нет связи с хостом";
                case "1005": return "Нет бумаги в принтере";
                case "1006": return "Ошибка криптографических ключей";
                case "1007": return "Устройство чтения карт не подключено";
                case "1008": return "Транзакция уже завершена";
            }

            return string.Empty;
        }
    }
}