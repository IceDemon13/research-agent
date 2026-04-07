namespace Telemart.Client.ViewModels.Base
{
    public interface IViewModelResolver
    {
        (bool ViewModelSupport, object Message) Resolve(
            int? entityId,
            int documentId);
    }
}