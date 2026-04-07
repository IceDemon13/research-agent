using System.IO.Abstractions;
using Telemart.Client.Core;
using Telemart.Client.Core.Serialization;

namespace Telemart.Client.Common.Settings.ModuleAnalytics
{
    internal sealed class ModuleAnalyticsSettingsStore : SettingsStore<ModuleAnalyticsSettings>, IModuleAnalyticsSettingsStore
    {
        public ModuleAnalyticsSettingsStore(ISerializerBuilder serializerBuilder, IFileSystem fileSystem)
            : base(serializerBuilder, fileSystem)
        {
        }

        public override string FileName { get; protected set; } = "module_analytics_settings.json";
        protected override string GetBasePath()
        {
            return ApplicationFolders.CommonApplicationData;
        }

        protected override ModuleAnalyticsSettings GetDefault()
        {
            return new ModuleAnalyticsSettings();
        }
    }
}