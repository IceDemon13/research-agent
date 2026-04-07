using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Store
{
    public class ProductEditSerialsParameter
    {
        public ProductEditSerialsParameter(List<string> serialNumbers, bool readOnly = false)
        {
            SerialNumbers = serialNumbers;
            ReadOnly = readOnly;
        }

        public List<string> SerialNumbers { get; set; }

        public bool ReadOnly { get; set; }
    }
}
