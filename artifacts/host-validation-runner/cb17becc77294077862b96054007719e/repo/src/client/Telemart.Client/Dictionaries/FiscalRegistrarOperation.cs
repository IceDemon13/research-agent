namespace Telemart.Client.Dictionaries
{
    public class FiscalRegistrarOperation : DictionaryItem
    {
        public const int OpenSessionId = 1;
        public const int CashCollectionId = 2;
        public const int XReportId = 3;
        public const int CloseSessionId = 4;
        public const int ZReportId = 5;

        private FiscalRegistrarOperation(int id, string name)
            : base(id, name, true)
        {
        }

        public static FiscalRegistrarOperation OpenSession { get; } = new FiscalRegistrarOperation(OpenSessionId, "Открытие смены");

        public static FiscalRegistrarOperation CashCollection { get; } = new FiscalRegistrarOperation(CashCollectionId, "Инкассация");

        public static FiscalRegistrarOperation XReport { get; } = new FiscalRegistrarOperation(XReportId, "X-отчет");

        public static FiscalRegistrarOperation CloseSession { get; } = new FiscalRegistrarOperation(CloseSessionId, "Закрытие смены");

        public static FiscalRegistrarOperation ZReport { get; } = new FiscalRegistrarOperation(ZReportId, "Z-отчет (печать)");
    }
}