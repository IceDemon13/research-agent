namespace Telemart.Client.Common
{
    internal static class Constants
    {
        public const string EmptyFilterItemDisplayValue = "(Не задано)";
        public const string UkraineDisplayValue = "Украина";
        public const int UkraineCountryId = 1;

        public const int TelemartContractorId = 885; // !Телемарт
        public const int RetailContractorId = 819; // Розница Саврадым Александр
        public const int ServiceContractorId = 1144; // Служебные

        public const decimal MaxProductPrice = 10_000_000;
        public const int ProductDimensionValueMaxLength = 9;

        public const int AssemblyServiceProductId = 89819;
        public const int QuickAssemblyServiceProductId = 749543;
        public const int LiqPayExpireMinutes = 10;

        public const int ParserEmployeeId = 44;

        public const string XlsFileExtension = "xls";

        public const int AssembledComputersCategoryId = 414;
        public const int AssembledComputersMinerCategoryId = 2807;

        public const int SystemEmployeeId = 1;

        public const char ZeroWidthSpace = '\u200b';

        public const int WebUserEmployee = 3;

        public const int MaxOrderCashAmount = 50_000;

        public const string ProductBaseUrl = "https://telemart.ua/products/";
        public const string BaseUrl = "https://telemart.ua";

        public const string PresaleContractor = "Предпродажа";
        public const int MainKyivWarehouseId = 75;

        public const string IngenicoComObjectName = "pos_x64.exe";

        public const char Sobaka = '@';

        public static readonly int[] UniqueComplectCategoryIds = { 1317, 406, 405, 400, 398 };

        public const int RootCategoryId = 1;
        public const int AnalyticsFormWidthMargin = 12;
    }
}