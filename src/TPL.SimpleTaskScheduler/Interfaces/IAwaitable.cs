using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace TPL.Interfaces
{
    public interface IAwaitable<TAwaiter> : INotifyCompletion where TAwaiter : class
    {
        /// <summary>
        /// Returns the awaitable object intance [this]
        /// </summary>
        /// <returns></returns>
        TAwaiter GetAwaiter();
        /// <summary>
        /// Sets the complition of the awaitable duty
        /// </summary>
        void GetResult();
        /// <summary>
        /// Checks if the awaitable duty is completed
        /// </summary>
        bool IsCompleted { get; }
    }
}
