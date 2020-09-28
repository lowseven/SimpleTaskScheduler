using System;
using System.Threading.Tasks;

namespace TPL.Interfaces
{
    public interface IWorkItem<TData> where TData : class
    {
        Task<TData> Task { get; }
    }

    public interface IWorkItem : IDisposable
    {
        int Id { get; }
        bool IsValid { get; }
        Task Task { get; }
        Action DoWork { get; }

        void SetCanceled();
        void SetCompletition(object result);
        void SetException(Exception ex);
    }
}
