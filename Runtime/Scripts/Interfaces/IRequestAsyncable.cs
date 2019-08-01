using System.Collections;
using UnityEngine.Events;

namespace DGTools.Database {
    /// <summary>
    /// Implement this interface on a <see cref="Request"/> to allow it to be run asynchronously
    /// </summary>
    public interface IRequestAsyncable
    {
        bool isDone { get; set; }
        IEnumerator OnExecuteAsync();
    }

    public static class RequestAsyncableExtensions
    {
        /// <summary>
        /// Executes the <see cref="Request"/> asynchronously and invoke <paramref name="OnExecutionDone"/> when its done
        /// </summary>
        /// <param name="OnExecutionDone">Will be invoked when request done</param>
        public static void ExecuteAsync(this IRequestAsyncable request, UnityAction<IRequestAsyncable> OnExecutionDone)
        {
            Database.active.StartCoroutine(request.RunExecuteAsync(OnExecutionDone));
        }
        
        static IEnumerator RunExecuteAsync(this IRequestAsyncable request, UnityAction<IRequestAsyncable> OnExecutionDone)
        {
            request.isDone = false;
            yield return request.OnExecuteAsync();
            request.isDone = true;
            OnExecutionDone.Invoke(request);
        }
    }
}
