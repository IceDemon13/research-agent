using System.Collections.Generic;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.RobotProperties
{
    public class RobotPropertyParameter : EditorParameter
    {
        public RobotPropertyParameter(int id, IReadOnlyCollection<string> groupNames)
            : base(id)
        {
            GroupNames = groupNames;
        }

        public IReadOnlyCollection<string> GroupNames { get; }
    }
}