namespace Telemart.Client.Dictionaries
{
    public sealed class PosTerminalType : DictionaryItem
    {
        public const int PrivatBankId = 1;

        public const int IngenicoId = 2;

        public const int UkrsibbankId = 3;

        public PosTerminalType(int id, string name)
            : base(id, name, true)
        {
        }
    }
}