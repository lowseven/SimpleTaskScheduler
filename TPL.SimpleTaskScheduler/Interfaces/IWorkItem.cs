using System;
using System.Threading.Tasks;

namespace TPL.Interfaces
{
    public interface IWorkItem<TData> : IDisposable
    {
        int Id { get; }
        bool IsValid { get; }
        Task<TData> Task { get; }
        Func<TData> DoWork { get; }
        Action<TData, object> DoWorkCallback { get; }

        void SetCanceled();
        void SetResult(TData result);
        void SetException(Exception ex);
    }

    public interface IWorkItem : IDisposable
    {
        int Id { get; }
        bool IsValid { get; }
        Task Task { get; }
        Action DoWork { get; }
        Action<object> DoWorkCallback { get; }

        void SetCanceled();
        void SetCompletition();
        void SetException(Exception ex);
    }
}
