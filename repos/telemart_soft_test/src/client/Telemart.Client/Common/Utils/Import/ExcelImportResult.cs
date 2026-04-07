using System.Collections.Generic;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.Common.Utils.Import
{
    public sealed class ExcelImportResult<TResult>
    {
        private ExcelImportResult()
        {
        }

        public IReadOnlyCollection<TResult> ResultItems { get; private set; }

        public bool IsSuccess { get; private set; }

        public List<ValidationResultItem> Errors { get; private set; }

        public static ExcelImportResult<TResult> Success(IReadOnlyCollection<TResult> data, List<ValidationResultItem> errors = null)
        {
            return new ExcelImportResult<TResult> { ResultItems = data, IsSuccess = true, Errors = errors };
        }

        public static ExcelImportResult<TResult> Error(List<ValidationResultItem> errors)
        {
            return new ExcelImportResult<TResult> { Errors = errors };
        }
    }
}
