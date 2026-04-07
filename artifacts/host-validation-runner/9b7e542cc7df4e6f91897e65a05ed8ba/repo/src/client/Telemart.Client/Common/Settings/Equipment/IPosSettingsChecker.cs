using System;
using System.Threading.Tasks;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Common.Settings.Equipment
{
    public interface IPosSettingsChecker
    {
        Task<Result<bool>> CheckPosSettingAsync(Guid? uniqueDeviceGuid, PosSettingsInfo posInfo);
    }
}