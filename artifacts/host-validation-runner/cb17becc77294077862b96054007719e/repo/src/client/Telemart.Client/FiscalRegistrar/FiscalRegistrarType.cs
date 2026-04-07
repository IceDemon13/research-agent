using System;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.FiscalRegistrar
{
    public class FiscalRegistrarType : DictionaryItem
    {
        public const int HardwareId = 1;

        public const int SoftwareId = 2;

        private FiscalRegistrarType(int id, string name, Type type)
            : base(id, name, true)
        {
            Type = type;
        }

        public static FiscalRegistrarType Hardware { get; } = new FiscalRegistrarType(HardwareId, "Аппаратный", typeof(FiscalRegistrarClient));

        public static FiscalRegistrarType Software { get; } = new FiscalRegistrarType(SoftwareId, "Программный", typeof(ProgrammicalFiscalRegistrarClient));

        public Type Type { get; }
    }
}