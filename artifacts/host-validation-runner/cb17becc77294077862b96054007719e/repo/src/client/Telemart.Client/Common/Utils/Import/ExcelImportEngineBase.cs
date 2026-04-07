using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.Common.Utils.Import
{
    public abstract class ExcelImportEngineBase<TRow, TSettings> :
        IExcelImportSettingsEngine<TRow, TSettings>,
        IExcelImportSettingsEngineAsync<TRow, TSettings>,
        IExcelImportEngine<TRow>,
        IExcelImportEngineAsync<TRow>
        where TRow : class
    {
        private List<TRow> rows;

        protected IReadOnlyCollection<TRow> Rows => rows;

        protected int HeaderRowCount { get; private set; }

        public async Task<ExcelImportResult<TRow>> ImportFromXlsxAsync(string fileName, TSettings settings)
        {
            ExcelImportResult<TRow> result = ImportFromXlsx(fileName, settings);

            if (result.IsSuccess)
            {
                ValidationResultItem[] errors = (await PostProcessAsync(settings)).ToArray();

                result.Errors.AddRange(errors);

                if (errors.Any(x => x.IsError))
                {
                    result = ExcelImportResult<TRow>.Error(result.Errors);
                }
            }

            return result;
        }

        public Task<ExcelImportResult<TRow>> ImportFromXlsxAsync(string fileName)
        {
            return ImportFromXlsxAsync(fileName, default);
        }

        public ExcelImportResult<TRow> ImportFromXlsx(string fileName)
        {
            return ImportFromXlsx(fileName, default);
        }

        public ExcelImportResult<TRow> ImportFromXlsx(string fileName, TSettings settings)
        {
            HeaderRowCount = GetHeaderRowCount(settings);

            Init(settings);

            rows = new List<TRow>();

            List<ValidationResultItem> validationItems = new List<ValidationResultItem>();

            ExcelReadResult result = GetExcelData(fileName, settings);

            if (result == null)
            {
                validationItems.Add(new ValidationResultItem("Лист не найден", true));
            }

            bool hasErrors = validationItems.Any(x => x.IsError);

            if (!hasErrors)
            {
                validationItems.AddRange(ValidateFile(result, settings));

                hasErrors = validationItems.Any(x => x.IsError);
            }

            if (!hasErrors)
            {
                validationItems.AddRange(ValidateHeader(result.HeaderRows, settings));
            }

            hasErrors = validationItems.Any(x => x.IsError);

            if (!hasErrors)
            {
                int rowNumber = HeaderRowCount;

                foreach (object[] dataRow in result.DataRows)
                {
                    rowNumber++;

                    List<ValidationResultItem> rowValidationItems = ValidateRowBeforeMap(dataRow, settings, rowNumber).ToList();

                    if (rowValidationItems.Any())
                    {
                        validationItems.AddRange(rowValidationItems);

                        if (rowValidationItems.Any(x => x.IsError))
                        {
                            hasErrors = true;
                            break;
                        }
                    }

                    TRow row = MapRow(dataRow, settings, rowNumber);

                    if (row == null)
                    {
                        continue;
                    }

                    rowValidationItems = ValidateRowAfterMap(row, settings, rowNumber).ToList();

                    if (rowValidationItems.Any())
                    {
                        validationItems.AddRange(rowValidationItems);

                        if (rowValidationItems.Any(x => x.IsError))
                        {
                            hasErrors = true;
                            break;
                        }
                    }
                    else
                    {
                        rows.Add(row);
                    }
                }
            }

            if (!hasErrors)
            {
                List<ValidationResultItem> rowsValidationItems = ValidateRows(settings).ToList();

                if (rowsValidationItems.Any())
                {
                    if (rowsValidationItems.Any(x => x.IsError))
                    {
                        hasErrors = true;
                    }

                    validationItems.AddRange(rowsValidationItems);
                }
            }

            return hasErrors ? ExcelImportResult<TRow>.Error(validationItems) : ExcelImportResult<TRow>.Success(Rows, validationItems);
        }

        protected virtual IEnumerable<ValidationResultItem> ValidateFile(ExcelReadResult excelData, TSettings settings)
        {
            yield break;
        }

        protected virtual IEnumerable<ValidationResultItem> ValidateHeader(object[][] headerRows, TSettings settings)
        {
            yield break;
        }

        protected virtual IEnumerable<ValidationResultItem> ValidateRowBeforeMap(object[] row, TSettings settings, int rowNumber)
        {
            yield break;
        }

        protected virtual IEnumerable<ValidationResultItem> ValidateRowAfterMap(TRow row, TSettings settings, int rowNumber)
        {
            yield break;
        }

        protected virtual IEnumerable<ValidationResultItem> ValidateRows(TSettings settings)
        {
            yield break;
        }

        protected virtual void Init(TSettings settings)
        {
            return;
        }

        protected virtual Task<IReadOnlyCollection<ValidationResultItem>> PostProcessAsync(TSettings settings)
        {
            return Task.FromResult((IReadOnlyCollection<ValidationResultItem>)Array.Empty<ValidationResultItem>());
        }

        protected abstract TRow MapRow(object[] row, TSettings settings, int rowNumber);

        protected abstract int GetHeaderRowCount(TSettings settings);

        protected virtual ExcelReadResult GetExcelData(string fileName, TSettings settings)
        {
            using Stream stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

            using ExcelPackage package = new ExcelPackage(stream);

            var worksheets = GetWorksheetsToProcess(package, settings);

            List<object[]> excelRows = new List<object[]>();

            foreach (var worksheet in worksheets)
            {
                if (worksheet != null)
                {
                    excelRows.AddRange(GetWorksheetRows(worksheet));
                }
            }

            if (excelRows.Any())
            {
                return new ExcelReadResult(
                    excelRows.Take(HeaderRowCount).ToArray(),
                    excelRows.Skip(HeaderRowCount).ToArray());
            }

            return null;
        }

        protected virtual IEnumerable<ExcelWorksheet> GetWorksheetsToProcess(ExcelPackage package, TSettings settings)
        {
            return [package.Workbook.Worksheets.FirstOrDefault()];
        }

        private static IEnumerable<object> ReadRow(ExcelWorksheet worksheet, int row)
        {
            for (int i = worksheet.Dimension.Start.Column; i <= worksheet.Dimension.End.Column; i++)
            {
                yield return worksheet.Cells[row, i].Value ?? string.Empty;
            }
        }

        private static IEnumerable<object[]> ReadRows(ExcelWorksheet worksheet)
        {
            if (worksheet.Dimension != null)
            {
                for (int i = worksheet.Dimension.Start.Row; i <= worksheet.Dimension.End.Row; i++)
                {
                    yield return ReadRow(worksheet, i).ToArray();
                }
            }
        }

        private static object[][] DuplicateValueInMergedCells(object[][] values, ExcelWorksheet worksheet)
        {
            foreach (string mergedCell in worksheet.MergedCells)
            {
                ExcelAddress address = new ExcelAddress(mergedCell);

                object value = values[address.Start.Row - 1][address.Start.Column - 1];

                for (int i = address.Start.Row; i <= address.End.Row; i++)
                {
                    for (int j = address.Start.Column; j <= address.End.Column; j++)
                    {
                        values[i - 1][j - 1] = value;
                    }
                }
            }

            return values;
        }

        private object[][] GetWorksheetRows(ExcelWorksheet worksheet)
        {
            return DuplicateValueInMergedCells(ReadRows(worksheet).ToArray(), worksheet);
        }

        protected class ExcelReadResult
        {
            public ExcelReadResult(object[][] headerRows, object[][] dataRows)
            {
                HeaderRows = headerRows;
                DataRows = dataRows;
            }

            public object[][] HeaderRows { get; }

            public object[][] DataRows { get; }
        }
    }
}