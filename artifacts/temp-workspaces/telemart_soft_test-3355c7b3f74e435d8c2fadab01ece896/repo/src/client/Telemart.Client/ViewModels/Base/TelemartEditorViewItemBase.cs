using System;
using Telemart.Client.Core.Cloning;

namespace Telemart.Client.ViewModels.Base
{
    public abstract class TelemartEditorViewItemBase : TelemartCloneableViewItemBase, ILockableEntity
    {
        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }
    }
}
