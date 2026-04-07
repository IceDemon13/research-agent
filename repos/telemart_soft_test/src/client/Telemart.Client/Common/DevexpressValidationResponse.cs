using DevExpress.XtraEditors.DXErrorProvider;

namespace Telemart.Client.Common
{
    public record DevexpressValidationResponse(bool IsValid, ErrorType? ErrorType, string Message);
}