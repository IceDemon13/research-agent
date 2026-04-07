namespace Telemart.Client.Dictionaries
{
    public class ServiceRequestDocumentType : DictionaryItem
    {
        public const int StatementId = 1;
        public const int RequestForReplacementFundId = 2;
        public const int ActId = 3;
        public const int ProductImageId = 4;
        public const int OtherId = 5;
        public const int RefundCreditStatementId = 6;
        public const int AgreementId = 7;
        public const int IssuanceCertificateId = 8;

        private ServiceRequestDocumentType(int id, string name)
            : base(id, name, true)
        {
        }

        public static ServiceRequestDocumentType Statement { get; } = new ServiceRequestDocumentType(StatementId, "Заявление");

        public static ServiceRequestDocumentType RequestForReplacementFund { get; } = new ServiceRequestDocumentType(RequestForReplacementFundId, "Запрос на подменный фонд");

        public static ServiceRequestDocumentType Act { get; } = new ServiceRequestDocumentType(ActId, "Акт");

        public static ServiceRequestDocumentType ProductImage { get; } = new ServiceRequestDocumentType(ProductImageId, "Фото товара");

        public static ServiceRequestDocumentType Other { get; } = new ServiceRequestDocumentType(OtherId, "Прочее");

        public static ServiceRequestDocumentType RefundCreditStatement { get; } = new ServiceRequestDocumentType(RefundCreditStatementId, "Заявление о возврате по кредиту");

        public static ServiceRequestDocumentType Agreement { get; } = new ServiceRequestDocumentType(AgreementId, "Договор");

        public static ServiceRequestDocumentType IssuanceCertificate { get; } = new ServiceRequestDocumentType(IssuanceCertificateId, "Акт выполненных работ");
    }
}
