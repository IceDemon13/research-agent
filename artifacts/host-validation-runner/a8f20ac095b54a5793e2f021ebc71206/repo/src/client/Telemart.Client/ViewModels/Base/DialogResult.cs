namespace Telemart.Client.ViewModels.Base
{
    public class DialogResult<T>
    {
        public DialogResult(bool isOk, T result)
        {
            IsOk = isOk;
            Result = result;
        }

        public bool IsOk { get; }

        public T Result { get; }
    }
}
