using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Telemart.Client.FiscalRegistrar.Responses.Base
{
    public abstract class FiscalRegistrarResponseBase
    {
        private readonly IReadOnlyCollection<string> errors = Array.Empty<string>();

        protected FiscalRegistrarResponseBase(string raw)
        {
            Raw = raw;
            ResponseObject = JToken.Parse(raw);

            if (ResponseObject is JObject jObject && jObject.TryGetValue("err", StringComparison.OrdinalIgnoreCase, out JToken errToken))
            {
                List<string> errorValues = new List<string>();

                ProcessErrorToken(errorValues, errToken);

                errors = errorValues.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                IsError = errors.Any();
            }
        }

        protected FiscalRegistrarResponseBase(int statusCode, string reason)
        {
            IsError = true;
            HttpErrorStatusCode = statusCode;
            HttpErrorReason = reason;

            errors = new[] { reason };
        }

        public string HttpErrorReason { get; }

        public int HttpErrorStatusCode { get; }

        public bool IsError { get; protected set; }

        public bool IsOk => !IsError;

        public string Raw { get; }

        protected JToken ResponseObject { get; }

        public IReadOnlyCollection<string> GetErrorMessages()
        {
            return errors;
        }

        private static void ProcessErrorToken(List<string> errorValues, JToken jToken)
        {
            switch (jToken)
            {
                case JValue errorValue:
                    string value = errorValue.ToObject<string>();
                    errorValues.Add(ProcessErrorValue(value));
                    break;
                case JObject errorObj:
                    ProcessErrorToken(errorValues, errorObj.GetValue("e", StringComparison.OrdinalIgnoreCase));
                    break;
                case JArray errorArr:
                    foreach (JToken x in errorArr)
                    {
                        ProcessErrorToken(errorValues, x);
                    }

                    break;
            }
        }

        private static string ProcessErrorValue(string errorValue)
        {
            string result;

            switch (errorValue)
            {
                case "x01": result = "Цена не указана"; break;
                case "x02": result = "Количество не указано"; break;
                case "x03": result = "Отдел не указан"; break;
                case "x04": result = "Группа не указана"; break;
                case "x25": result = " Нет бумаги"; break;
                case "x31": result = "Пользователь уже зарегистрирован"; break;
                case "x32": result = "Неверный пароль"; break;
                case "x33": result = "Неверный номер таблицы"; break;
                case "x34": result = "Доступ к таблице запрещен"; break;
                case "x35": result = "Умолчание не найдено"; break;
                case "x36": result = "Неверный индекс"; break;
                case "x37": result = "Неверное поле"; break;
                case "x38": result = "Таблица переполнена"; break;
                case "x39": result = "Неверная длина двоичных данных"; break;
                case "x3A": result = "Попытка модификации поля только для чтения"; break;
                case "x3B": result = "Неверное значение поля"; break;
                case "x3C": result = "Товар уже существует"; break;
                case "x3D": result = "По товару были продажи"; break;
                case "x3E": result = "Запрос запрещен"; break;
                case "x3F": result = "Неверная закладка"; break;
                case "x40": result = "Ключ не найден"; break;
                case "x41": result = "Процедура уже исполняется"; break;
                case "x42": result = "Количество товара отрицательно"; break;
                case "x43": result = "Включено перенаправление с карты памяти"; break;
                case "x44": result = "501 отчет не пуст"; break;
                case "x87": result = "Ошибка фискальной памяти "; break;
                case "x88": result = "шибка карты памяти"; break;
                case "x89": result = "Переполнение карты памяти"; break;
                case "x8A": result = "Нет бумаги для контрольной ленты"; break;
                case "x8B": result = "Нет бумаги"; break;
                case "x8C": result = "Переполнение фискальной памяти"; break;
                case "x8D": result = "Выдача сдачи запрещена"; break;
                case "xA3": result = "Операция прекращена устройством"; break;
                case "xA5": result = "Дневной отчет не найден"; break;
                case "xA7": result = "MMC запрещено"; break;
                case "xA8": result = "Неверен номер фискальной памяти"; break;
                case "xA9": result = "Фискальная память не пуста"; break;
                case "xBB": result = "Лента не пуста"; break;
                case "xBC": result = "Режим тренировки"; break;
                case "xBD": result = "Текущая дата неверна"; break;
                case "xBE": result = "Запрещено изменение времени"; break;
                case "xBF": result = "Истек сервисный таймер"; break;
                case "xC0": result = "Ошибка работы с терминалом НСМЕП"; break;
                case "xC1": result = "Неверный номер налога"; break;
                case "xC2": result = "Неверный параметр у процедуры"; break;
                case "xC3": result = "Режим фискального принтера не активен"; break;
                case "xC4": result = "Изменялось название товара или его налог"; break;
                case "xC5": result = "Необходима персонализация"; break;
                case "xC6": result = "Отсутствует обмен с сервером НСМЭП на протяжении 72 часов"; break;
                case "xC7": result = "Запрещено обнуление данных ЭКЛ"; break;
                case "xC8": result = "Запрещена работа с GPRS модемом"; break;
                case "xCC": result = "Начата операция возврата"; break;
                case "xCF": result = "Не выведен отчет Z"; break;
                case "xD0": result = "Не сделана инкассация денег"; break;
                case "xD1": result = "Сейф не закрыт"; break;
                case "xD2": result = "Печать ленты прервана"; break;
                case "xD3": result = "Достигнут конец текущей смены, или изменилась дата"; break;
                case "xD4": result = "Не указано значение процентной скидки по умолчанию"; break;
                case "xD5": result = "Не указано значение скидки по умолчанию"; break;
                case "xD6": result = "Дневной отчет не выведен"; break;
                case "xD7": result = "Дневной отчет уже выведен (и пуст)"; break;
                case "xD8": result = "Нельзя отменить товар на который сделана скидка без ее предварительной отмены"; break;
                case "xD9": result = "Товар не продавался в этом чеке"; break;
                case "xDA": result = "Нечего отменять"; break;
                case "xDB": result = "Отрицательная сумма продажи товара"; break;
                case "xDC": result = "Неверный процент"; break;
                case "xDD": result = "Нет ни одной продажи"; break;
                case "xDE": result = "Скидки запрещены"; break;
                case "xDF": result = "Неверная сумма платежа"; break;
                case "xE0": result = "Тип оплаты не предполагает введения кода клиента"; break;
                case "xE1": result = "Неверная сумма платежа"; break;
                case "xE2": result = "Идет оплата чека"; break;
                case "xE3": result = "Товар закончился"; break;
                case "xE4": result = "Номер группы не может меняться"; break;
                case "xE5": result = "Неверная группа"; break;
                case "xE6": result = "Номер отдела не может меняться"; break;
                case "xE7": result = "Неверный отдел"; break;
                case "xE8": result = "Нулевое произведение количества на цену"; break;
                case "xE9": result = "Переполнение внутренних сумм"; break;
                case "xEA": result = "Дробное количество запрещено"; break;
                case "xEB": result = "Неверное количество"; break;
                case "xEC": result = "Цена не может быть изменена"; break;
                case "xED": result = "Неверная цена"; break;
                case "xEE": result = "Товар не существует"; break;
                case "xEF": result = "Начат чек внесения-изъятия денег"; break;
                case "xF0": result = "Чек содержит продажи"; break;
                case "xF1": result = "Не существующий или запрещенный тип оплаты"; break;
                case "xF2": result = "Поле в строке переполнено"; break;
                case "xF3": result = "Отрицательная сумма по дневному отчету"; break;
                case "xF4": result = "Отрицательная сумма по чеку"; break;
                case "xF5": result = "Чек переполнен"; break;
                case "xF6": result = "Дневной отчет переполнен"; break;
                case "xF7": result = "Чек для копии не найден"; break;
                case "xF8": result = "Оплата чека не завершена"; break;
                case "xF9": result = "Кассир не зарегистрирован"; break;
                case "xFA": result = "У кассира нет прав на эту операцию"; break;
                case "xFB": result = "Нефискальный чек не открыт"; break;
                case "xFC": result = "Чек не открыт"; break;
                case "xFD": result = "Нефискальный чек уже открыт"; break;
                case "xFE": result = "Чек уже открыт"; break;
                case "xFF": result = "Переполнение ленты"; break;
                default: result = errorValue; break;
            }

            return result;
        }
    }
}
