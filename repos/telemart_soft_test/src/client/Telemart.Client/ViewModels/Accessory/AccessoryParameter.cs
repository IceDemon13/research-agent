using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Accessory
{
    public class AccessoryParameter : IEditorParameterCopy
    {
        public AccessoryParameter(int id, bool copy = false)
        {
            Id = id;
            Copy = copy;
        }

        public int Id { get; }

        public bool IsNew => Id == 0;

        public bool Copy { get; }
    }
}