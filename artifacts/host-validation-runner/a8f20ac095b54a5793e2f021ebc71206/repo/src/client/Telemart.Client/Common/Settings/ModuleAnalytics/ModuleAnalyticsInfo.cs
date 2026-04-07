using System.Collections.Generic;

namespace Telemart.Client.Common.Settings.ModuleAnalytics
{
    public class ModuleAnalyticsSettings
    {
        public Dictionary<string, ModuleAnalyticsSetting> Settings { get; set; } = new Dictionary<string, ModuleAnalyticsSetting>();
    }
}