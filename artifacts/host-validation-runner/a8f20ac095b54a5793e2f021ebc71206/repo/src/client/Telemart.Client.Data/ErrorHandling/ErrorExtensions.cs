using System.Collections.Generic;
using System.Linq;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Core.ErrorHandling
{
    public static class ErrorExtensions
    {
        private static readonly Dictionary<ErrorCode, string> ErrorMessageValues;

        static ErrorExtensions()
        {
            ErrorMessageValues = new Dictionary<ErrorCode, string>
            {
                [ErrorCode.SetSourceOrderLocked] = "Заказ заблокирован",
                [ErrorCode.SetSourceAlreadySet] = "Источкик уже установлен",
                [ErrorCode.SetSourceOrderStateInvalid] = "Некорректный статус заказа",
                [ErrorCode.SetSourceCanNotGetInTime] = "Не успевает в срок",
                [ErrorCode.SetSourceInsufficientQuantity] = "Товара нет в нужном количестве",
                [ErrorCode.SetSourceInvoiceRequred] = "Накладная не существует",
                [ErrorCode.SetSourceInvoiceAlreadyAccepted] = "Накладная уже принята",
                [ErrorCode.SetSourceInvoiceInsufficientQuantity] = "Товара нет в нужном количестве в свободном остатке",
                [ErrorCode.SetSourceErrorDepartureDateCalculation] = "Ошибка расчета Даты Х",
                [ErrorCode.SetSourceInvoiceCancelled] = "Накладная отменена",
                [ErrorCode.SetSourceInvoiceLocked] = "Накладная заблокирована",
                [ErrorCode.SetSourceInvoiceClosed] = "Запрещено выбирать накладную за 30 мин до закрытия",
                [ErrorCode.SetSourceAccountingSystemError] = "Ошибка при получении остатков из 1С",
                [ErrorCode.SetSourceOrderProductStateInvalid] = "Некорректный статус товара",

                [ErrorCode.CreateCategoryParentNotFound] = "Такой родительской категории не существует",
                [ErrorCode.CreateCategoryParentAlreadyExists] = "В ветке уже есть родительская категория",
                [ErrorCode.CreateCategoryBrandAlreadyExists] = "Бренды можно создавать только в родительской категории",
                [ErrorCode.CreateCategoryFolderCantBePlacedUnderParent] = "Папка не может быть создана в родительской категории",
                [ErrorCode.DuplicateCategory] = "Такая категория уже существует",
                [ErrorCode.CreateCategory] = "Ошибка при создании категории",
                [ErrorCode.CreateCategoryParentFolderCantBePlacedUnderNonFolder] = "Родительская категория не может быть создана не в папке",
                [ErrorCode.CreateCategoryFolderParentHasOtherCategoryTypes] = "В этой категории уже есть бренды либо родители. Папка не может быть создана вместе с ними",
                [ErrorCode.CreateCategoryBrandCannotBeCreatedNearFolder] = "В этой категории уже есть папки. Бренд не может быть создан вместе с ними",

                [ErrorCode.CreateInvoice] = "Ошибка при создании накладной",
                [ErrorCode.DuplicateInvoice] = "Такая накладная уже существует",

                [ErrorCode.CreateContractorTemplate] = "Ошибка при создании шаблона контрагента",
                [ErrorCode.DuplicateContractorTemplate] = "Шаблон с таким именем уже существует",
                [ErrorCode.DuplicateContractor] = "Контрагент с таким названием уже существует",

                [ErrorCode.FeatureUsedAsValuesSource] = "Характеристика используется как источник значений",
                [ErrorCode.FeatureUsedInAssemblyFullRules] = "Характеристика используется в правилах совместимости"
            };
        }

        public static string GetErrorMessage(this Error error)
        {
            if (!ErrorMessageValues.TryGetValue(error.ErrorCode, out string errorMessage))
            {
                errorMessage = string.Join(" ", error.Details.Select(x => x.ErrorMessage));
            }

            return errorMessage;
        }

        public static string GetErrorMessageById(int id)
        {
            string errorMessage = ErrorMessageValues.FirstOrDefault(x => (int)x.Key == id).Value;
            return errorMessage;
        }

        public static bool HasErrorCodeMessage(this Error error)
        {
            return ErrorMessageValues.ContainsKey(error.ErrorCode);
        }
    }
}