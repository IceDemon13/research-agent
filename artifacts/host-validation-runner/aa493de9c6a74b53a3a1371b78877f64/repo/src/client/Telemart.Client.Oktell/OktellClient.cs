using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Xml;

namespace Telemart.Client.Oktell
{
    public class OktellClient : ICallServiceClient
    {
        private const string BaseUrl = "http://localhost:4059/";

        private readonly HttpClient client;

        public OktellClient()
        {
            client = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
            };
        }

        public async Task<OktellResult> CallAsync(string number)
        {
            OktellResult result;

            try
            {
                HttpResponseMessage response = await client.GetAsync($"callto?number={number}");

                result = response.IsSuccessStatusCode
                    ? new OktellResult()
                    : new OktellResult("Ошибка при старте звонка", true);
            }
            catch (HttpRequestException e) when ((e.InnerException as SocketException)?.ErrorCode == 10061)
            {
                result = new OktellStateResult("Oktell клиент не запущен", true);
            }
            catch (Exception e)
            {
                result = new OktellResult($"Ошибка при старте звонка: {e.Message}", true);
            }

            return result;
        }

        public bool CanCall() => true;

        public async Task<OktellStateResult> GetStateAsync()
        {
            OktellStateResult result;

            try
            {
                HttpResponseMessage response = await client.GetAsync("getcurrentcallinfo");

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    result = GetOktellState(content);
                }
                else
                {
                    result = new OktellStateResult("Ошибка при получении статуса пользователя", true);
                }
            }
            catch (HttpRequestException e) when ((e.InnerException as SocketException)?.ErrorCode == 10061)
            {
                result = new OktellStateResult("Oktell клиент не запущен", true);
            }
            catch (Exception e)
            {
                result = new OktellStateResult($"Ошибка при получении статуса пользователя: {e.Message}", true);
            }

            return result;
        }

        public async Task<OktellResult> EndCallAsync()
        {
            OktellResult result;

            try
            {
                HttpResponseMessage response = await client.GetAsync("disconnectcall");

                result = response.IsSuccessStatusCode
                    ? new OktellResult()
                    : new OktellResult("Ошибка при завершении звонка", true);
            }
            catch (HttpRequestException e) when ((e.InnerException as SocketException)?.ErrorCode == 10061)
            {
                result = new OktellStateResult("Oktell клиент не запущен", true);
            }
            catch (Exception e)
            {
                result = new OktellResult($"Ошибка при завершении звонка: {e.Message}", true);
            }

            return result;
        }

        public bool CanEndCall() => true;

        public async Task<OktellResult> CancelCallAsync()
        {
            OktellResult result;

            try
            {
                HttpResponseMessage response = await client.GetAsync("declinecall");

                result = response.IsSuccessStatusCode
                    ? new OktellResult()
                    : new OktellResult("Ошибка при отклонении звонка", true);
            }
            catch (HttpRequestException e) when ((e.InnerException as SocketException)?.ErrorCode == 10061)
            {
                result = new OktellStateResult("Oktell клиент не запущен", true);
            }
            catch (Exception e)
            {
                result = new OktellResult($"Ошибка при отклонении звонка: {e.Message}", true);
            }

            return result;
        }

        public bool CanCancelCall() => true;

        public async Task<OktellResult> ApplyCallAsync()
        {
            OktellResult result;

            try
            {
                HttpResponseMessage response = await client.GetAsync("headsetanswercall");

                result = response.IsSuccessStatusCode
                    ? new OktellResult()
                    : new OktellResult("Ошибка при принятии звонка", true);
            }
            catch (HttpRequestException e) when ((e.InnerException as SocketException)?.ErrorCode == 10061)
            {
                result = new OktellStateResult("Oktell клиент не запущен", true);
            }
            catch (Exception e)
            {
                result = new OktellResult($"Ошибка при принятии звонка: {e.Message}", true);
            }

            return result;
        }

        public bool CanApplyCall() => true;

        private OktellStateResult GetOktellState(string xml)
        {
            XmlDocument document = new XmlDocument();

            document.LoadXml(xml);

            XmlNode node = document.SelectSingleNode("oktellxmlmapper/data/property_set/property_simple[@key='mode']");

            OktellState state = OktellState.None;
            int? lineNum = null;
            DateTime? startTime = null;

            if (node != null)
            {
                state = GetState(node.Attributes?["value"].Value);
            }

            node = document.SelectSingleNode("oktellxmlmapper/data/property_set/property_simple[@key='linenum']");

            if (node != null && int.TryParse(node.Attributes?["value"].Value, out int line))
            {
                lineNum = line;
            }

            node = document.SelectSingleNode("oktellxmlmapper/data/property_set/property_simple[@key='starttime']");

            if (node != null && DateTime.TryParse(node.Value, out DateTime time))
            {
                startTime = time;
            }

            node = document.SelectSingleNode("oktellxmlmapper/data/property_set/property_simple[@key='number']");

            string phone = node?.Attributes?["value"].Value;

            node = document.SelectSingleNode("oktellxmlmapper/data/property_set/property_simple[@key='abonenttype']");

            string typeStr = node?.Attributes?["value"].Value;

            OktellType type = GetType(typeStr);

            node = document.SelectSingleNode("oktellxmlmapper/data/property_set/property_cdata[@key='name']");

            string name = node?.FirstChild?.Value;

            return new OktellStateResult(state, phone, startTime, lineNum, type, name);
        }

        private OktellState GetState(string stateStr)
        {
            OktellState state;

            switch (stateStr)
            {
                case "none":
                    state = OktellState.Idle;
                    break;
                case "ringing":
                    state = OktellState.Ringing;
                    break;
                case "connected":
                    state = OktellState.Connected;
                    break;
                default:
                    state = OktellState.None;
                    break;
            }

            return state;
        }

        private OktellType GetType(string typeStr)
        {
            switch (typeStr)
            {
                case "outer":
                    return OktellType.Outer;
                case "inner":
                    return OktellType.Inner;
                case "ivr":
                    return OktellType.Ivr;
                case "task":
                    return OktellType.Task;
                default:
                    return OktellType.None;
            }
        }
    }
}