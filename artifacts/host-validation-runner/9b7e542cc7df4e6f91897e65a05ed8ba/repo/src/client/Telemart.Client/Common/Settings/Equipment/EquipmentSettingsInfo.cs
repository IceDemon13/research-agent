using System;
using Newtonsoft.Json;
using Telemart.Client.Dictionaries;
using Telemart.Client.FiscalRegistrar;

namespace Telemart.Client.Common.Settings.Equipment
{
    public sealed class EquipmentSettingsInfo
    {
        public EquipmentSettingsInfo(FiscalRegistrarSettingsInfo fiscalRegistrar, PosSettingsInfo pos, TimeSpan? lockTimeout, Guid uniqueDeviceGuid, string themeName)
        {
            Pos = pos;
            FiscalRegistrar = fiscalRegistrar;
            LockTimeout = lockTimeout;
            UniqueDeviceGuid = uniqueDeviceGuid;
            ThemeName = themeName;
        }

        public EquipmentSettingsInfo()
        {
        }

        [JsonProperty("fiscal_registrar")]
        public FiscalRegistrarSettingsInfo FiscalRegistrar { get; set; }

        [JsonProperty("pos")]
        public PosSettingsInfo Pos { get; set; }

        [JsonProperty("lock_timeout")]
        public TimeSpan? LockTimeout { get; set; }

        [JsonProperty("unique_device_guid")]
        public Guid? UniqueDeviceGuid { get; set;  }

        [JsonProperty("theme_name")]
        public string ThemeName { get; set;  }

        public bool ShowFiscalRegistrarModule()
        {
            return FiscalRegistrar != null && FiscalRegistrar.CashboxId.HasValue;
        }

        public int? GetPaymentIdByCashbox(int cashboxId)
        {
            if (cashboxId == FiscalRegistrar.CashboxId)
            {
                return Payment.CashId;
            }

            if (cashboxId == Pos.CashboxId)
            {
                return Payment.TerminalId;
            }

            return null;
        }
    }
}