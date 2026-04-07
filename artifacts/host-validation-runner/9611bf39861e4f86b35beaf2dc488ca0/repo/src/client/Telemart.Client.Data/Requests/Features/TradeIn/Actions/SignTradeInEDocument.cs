using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.TradeIn.Actions
{
    public class SignTradeInEDocument : CallEntityActionRequestBase<Result>
    {
        public SignTradeInEDocument(int id)
            : base(id, ApiResources.TradeIns, "e_document_sign")
        {
        }
    }
}
