using System;
using System.Collections.Generic;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store
{
    public class ProductComparisonViewItem : BindableBase
        {
            private readonly HashSet<string> serials;

            public ProductComparisonViewItem(
                int productId,
                string fullName,
                int quantityReal,
                bool keepSerial,
                bool selfBarcode,
                int minQuantity,
                IEnumerable<string> serials)
            {
                ProductId = productId;
                FullName = fullName;
                QuantityReal = quantityReal;
                KeepSerial = keepSerial;
                SelfBarcode = selfBarcode;
                MinQuantity = minQuantity;
                this.serials = new HashSet<string>(serials, StringComparer.OrdinalIgnoreCase);
            }

            public string FullName
            {
                get { return GetProperty(() => FullName); }
                private set { SetProperty(() => FullName, value); }
            }

            public int ProductId
            {
                get { return GetProperty(() => ProductId); }
                private set { SetProperty(() => ProductId, value); }
            }

            public bool KeepSerial
            {
                get { return GetProperty(() => KeepSerial); }
                set { SetProperty(() => KeepSerial, value); }
            }

            public int QuantityReal
            {
                get { return GetProperty(() => QuantityReal); }
                set { SetProperty(() => QuantityReal, value); }
            }

            public bool SelfBarcode
            {
                get { return GetProperty(() => SelfBarcode); }
                set { SetProperty(() => SelfBarcode, value); }
            }

            public int SupplierId
            {
                get { return GetProperty(() => SupplierId); }
                set { SetProperty(() => SupplierId, value); }
            }

            public int MinQuantity { get; }

            public IReadOnlyCollection<string> Serials => serials;

            public void AddSerials(IEnumerable<string> serialNumbers)
            {
                foreach (string serial in serialNumbers)
                {
                    serials.Add(serial);
                }
            }

            public void ClearSerials()
            {
                serials.Clear();
            }

            public void ReplaceSerials(IEnumerable<string> serialNumbers)
            {
                ClearSerials();
                AddSerials(serialNumbers);
            }
        }
}