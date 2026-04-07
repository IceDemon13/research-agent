using System;
using Telemart.Client.Core.Cloning;

namespace Telemart.Client.ViewModels.Base
{
    public class TelemartCloneableViewItemBase : TelemartViewItemBase, ICloneable
    {
        object ICloneable.Clone()
        {
            return Clone();
        }

        public virtual object Clone()
        {
            return ReflectionObjectCloner.Clone(this);
        }
    }
}