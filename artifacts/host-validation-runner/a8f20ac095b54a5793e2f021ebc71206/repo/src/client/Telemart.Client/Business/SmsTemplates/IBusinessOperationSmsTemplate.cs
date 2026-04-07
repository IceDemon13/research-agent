using Telemart.Client.Data.WebClient.Security;

namespace Telemart.Client.Business.SmsTemplates
{
    public interface IBusinessOperationSmsTemplate : ISmsTemplate
    {
        BusinessOperation Operation { get; }
    }
}