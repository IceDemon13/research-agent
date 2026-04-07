using System.Collections.Generic;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Common.Messages
{
    public class ReturnInvoicesMessage
    {
        public ReturnInvoicesMessage(ReturnInvoiceDto[] returnInvoiceDtos, MessageType messageType)
        {
            ReturnInvoiceDtos = returnInvoiceDtos;
            MessageType = messageType;
        }

        public ReturnInvoiceDto[] ReturnInvoiceDtos { get; }

        public MessageType MessageType { get; }
    }
}