using System;
using System.IO.Abstractions;
using System.Threading.Tasks;
using DevExpress.Xpf.Core;
using Telemart.Client.Core;
using Telemart.Client.Core.Security;
using Telemart.Client.Core.Serialization;
using Telemart.Client.FiscalRegistrar;

namespace Telemart.Client.Common.Settings.Equipment
{
    internal sealed class EquipmentSettingsStore : SettingsStore<EquipmentSettingsInfo>, IEquipmentSettingsStore
    {
        public EquipmentSettingsStore(ISerializerBuilder serializerBuilder, IFileSystem fileSystem, ICryptoManager cryptoManager)
            : base(serializerBuilder, fileSystem)
        {
            CryptoManager = cryptoManager;
        }

        public override string FileName { get; protected set; } = "equipment_settings.json";

        private ICryptoManager CryptoManager { get; }

        protected override string GetBasePath()
        {
            return ApplicationFolders.CommonApplicationData;
        }

        protected override EquipmentSettingsInfo GetDefault()
        {
            return new EquipmentSettingsInfo(
                new FiscalRegistrarSettingsInfo
                {
                    Type = FiscalRegistrarType.Hardware.Name
                },
                new PosSettingsInfo(),
                null,
                Guid.NewGuid(),
                Theme.Win10SystemName);
        }

        protected override EquipmentSettingsInfo MergeSettings(EquipmentSettingsInfo settings, EquipmentSettingsInfo defaultSettings)
        {
            settings.Pos ??= defaultSettings.Pos;
            settings.FiscalRegistrar ??= defaultSettings.FiscalRegistrar;
            settings.ThemeName ??= defaultSettings.ThemeName;

            return settings;
        }

        protected override async Task PreProcessAsync(EquipmentSettingsInfo settings)
        {
            await base.PreProcessAsync(settings);

            settings.FiscalRegistrar.Password = await CryptoManager.DecryptAsync(settings.FiscalRegistrar.Password);
            settings.FiscalRegistrar.Type ??= FiscalRegistrarType.Hardware.Name;

            if (settings.UniqueDeviceGuid == null)
            {
                settings.UniqueDeviceGuid = Guid.NewGuid();
                await SaveAsync(settings);
            }
        }

        protected override async Task PostProcessAsync(EquipmentSettingsInfo settings)
        {
            await base.PostProcessAsync(settings);

            settings.FiscalRegistrar.Password = await CryptoManager.EncryptAsync(settings.FiscalRegistrar.Password);
        }
    }
}