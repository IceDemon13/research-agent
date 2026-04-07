using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Data.Extensions;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Money.Receive.BankPayment
{
    public class BulkCreateBankPaymentExcelImportEngine : ExcelImportEngineBase<BulkCreateBankPaymentViewItem, object>
    {
        private const string OrderIdColumn = "order_id";
        private const string PaidOnColumn = "paid_on";
        private const string AmountColumn = "amount";
        private const string StatementSupportColumn = "statement_support";
        private const string ContractorNameColumn = "contractor_name";
        private const string ReferenceColumn = "ref";
        private const string CommentColumn = "comment";
        private const string PaymentIdColumn = "id_payment";
        private const string FeeColumn = "fee";
        private const string TotalAmountColumn = "total_amount";

        private readonly Dictionary<string, int> columnPositions = new Dictionary<string, int>();
        private bool isEmptyRow;
        private int emptyRowNumber = -1;

        protected override int GetHeaderRowCount(object settings)
        {
            return 1;
        }

        protected override ExcelReadResult GetExcelData(string fileName, object settings)
        {
            isEmptyRow = false;
            emptyRowNumber = -1;

            return base.GetExcelData(fileName, settings);
        }

        protected override BulkCreateBankPaymentViewItem MapRow(object[] row, object settings, int rowNumber)
        {
            if (emptyRowNumber > 0 && emptyRowNumber <= rowNumber)
            {
                return null;
            }

            BulkCreateBankPaymentViewItem mappedRow = new BulkCreateBankPaymentViewItem();

            if (decimal.TryParse(row[columnPositions[AmountColumn]].ToString(), out decimal amount))
            {
                mappedRow.Amount = amount;
            }

            if (DateTime.TryParse(row[columnPositions[PaidOnColumn]].ToString(), out DateTime paidOn))
            {
                mappedRow.PaidOn = paidOn;
            }

            mappedRow.StatementSupport = row[columnPositions[StatementSupportColumn]].ToString() == "1";

            if (int.TryParse(row[columnPositions[OrderIdColumn]].ToString(), out int orderId))
            {
                mappedRow.OrderId = orderId;
            }

            if (decimal.TryParse(row[columnPositions[FeeColumn]].ToString(), out decimal fee))
            {
                mappedRow.Fee = fee;
            }

            if (decimal.TryParse(row[columnPositions[TotalAmountColumn]].ToString(), out decimal totalAmount))
            {
                mappedRow.TotalAmount = totalAmount;
            }

            if (int.TryParse(row[columnPositions[PaymentIdColumn]].ToString(), out int paymentId))
            {
                mappedRow.PaymentId = paymentId;
            }

            mappedRow.Comment = row[columnPositions[CommentColumn]].ToString();
            mappedRow.ContractorName = row[columnPositions[ContractorNameColumn]].ToString();
            mappedRow.Reference = row[columnPositions[ReferenceColumn]].ToString();

            return mappedRow;
        }

        protected override IEnumerable<ValidationResultItem> ValidateRowBeforeMap(object[] row, object settings, int rowNumber)
        {
            if (EmptyRow(row, rowNumber))
            {
                yield break;
            }

            string statementSupport = row[columnPositions[StatementSupportColumn]].ToString();

            if (statementSupport != "0" && statementSupport != "1")
            {
                yield return new ValidationResultItem($"statement_support в строке {rowNumber} должен иметь значение 1 или 0", true);
            }

            string orderId = row[columnPositions[OrderIdColumn]].ToString();
            string reference = row[columnPositions[ReferenceColumn]].ToString();

            if (statementSupport == "1")
            {
                if (string.IsNullOrWhiteSpace(orderId) && string.IsNullOrWhiteSpace(reference))
                {
                    yield return new ValidationResultItem($"order_id или ref в строке {rowNumber} должны быть заполнены", true);
                }

                if (!decimal.TryParse(row[columnPositions[AmountColumn]].ToString(), out decimal amount)
                    || !decimal.TryParse(row[columnPositions[FeeColumn]].ToString(), out decimal fee)
                    || !decimal.TryParse(row[columnPositions[TotalAmountColumn]].ToString(), out decimal totalAmount)
                    || (Math.Round(amount, 2) + Math.Round(fee, 2)) != Math.Round(totalAmount, 2))
                {
                    yield return new ValidationResultItem($"total_amount неккоректные данные в строке {rowNumber}. [amount + fee = total_amount]", true);
                }
            }
        }

        protected override IEnumerable<ValidationResultItem> ValidateHeader(object[][] headerRows, object settings)
        {
            if (headerRows.Length != 1)
            {
                yield return new ValidationResultItem("Шапка документа должна состоять из 1 строки", true);
            }

            string[] columnNames =
            {
                OrderIdColumn,
                PaidOnColumn,
                AmountColumn,
                StatementSupportColumn,
                ContractorNameColumn,
                ReferenceColumn,
                CommentColumn,
                FeeColumn,
                TotalAmountColumn,
                PaymentIdColumn
            };

            string[] header = headerRows[0]
                .Select(x => x.ToString())
                .ToArray();

            foreach (string column in columnNames)
            {
                columnPositions[column] = header.FindIndex(x => x == column);
            }

            foreach (KeyValuePair<string, int> column in columnPositions.Where(x => x.Value < 0))
            {
                yield return new ValidationResultItem($"Столбец \"{column.Key}\" не найден", true);
            }
        }

        private bool EmptyRow(object[] row, int rowNumber)
        {
            if (isEmptyRow)
            {
                return true;
            }

            string orderIdCol = row[columnPositions[OrderIdColumn]].ToString();
            string paidOnCol = row[columnPositions[PaidOnColumn]].ToString();
            string amountdCol = row[columnPositions[AmountColumn]].ToString();
            string statementSupportOnCol = row[columnPositions[StatementSupportColumn]].ToString();
            string contractorCol = row[columnPositions[ContractorNameColumn]].ToString();
            string refrenceCol = row[columnPositions[ReferenceColumn]].ToString();
            string commentCol = row[columnPositions[CommentColumn]].ToString();
            string paymentCol = row[columnPositions[PaymentIdColumn]].ToString();
            string feeCol = row[columnPositions[FeeColumn]].ToString();
            string totalAmountCol = row[columnPositions[TotalAmountColumn]].ToString();

            isEmptyRow = string.IsNullOrEmpty(orderIdCol)
                   && string.IsNullOrEmpty(paidOnCol)
                   && string.IsNullOrEmpty(amountdCol)
                   && string.IsNullOrEmpty(statementSupportOnCol)
                   && string.IsNullOrEmpty(contractorCol)
                   && string.IsNullOrEmpty(refrenceCol)
                   && string.IsNullOrEmpty(commentCol)
                   && string.IsNullOrEmpty(paymentCol)
                   && string.IsNullOrEmpty(totalAmountCol)
                   && string.IsNullOrEmpty(feeCol);

            if (emptyRowNumber < 0 && isEmptyRow)
            {
                emptyRowNumber = rowNumber;
            }

            return isEmptyRow;
        }
    }
}