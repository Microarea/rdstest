using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

#pragma warning disable SA1300, IDE1006
namespace Prelude
{
    /// <summary>
    ///     Utility static methods.
    /// </summary>
    internal static class Functional
    {
        /// <summary>
        ///     Calls the specified function block with the given receiver as input and returns its result (same as kotlin).
        /// </summary>
        /// <returns>Returns the result of block(receiver).</returns>
        public static TR with<T, TR>(this T receiver, Func<T, TR> block) => block(receiver);

        /// <summary>
        ///     Calls the specified function block with the given receiver as input and returns its result (same as kotlin).
        /// </summary>
        /// <returns>Returns the result of block(receiver).</returns>
        public static async Task<TR> withAsync<T, TR>(this T receiver, Func<T, Task<TR>> block) => await block(receiver);

        /// <summary>
        ///     Calls the specified function block with the given receiver as input and returns the receiver itself (same as kotlin).
        /// </summary>
        /// <returns>Returns the receiver itself.</returns>
        public static T apply<T>(this T receiver, Action<T> block)
        {
            block(receiver);
            return receiver;
        }

        /// <summary>
        ///     Async Calls the specified function block with the given receiver as input and returns the receiver itself (same as kotlin).
        /// </summary>
        /// <returns>Returns the receiver itself wrapped in a Task.</returns>
        public static async Task<T> applyAsync<T>(this T receiver, Func<T, Task> block)
        {
            await block(receiver);
            return receiver;
        }

        public static Func<A, R> memo<A, R>(this Func<A, R> f)
        {
            var cache = new ConcurrentDictionary<A, R>();
            return argument => cache.GetOrAdd(argument, f);
        }

        public static Func<R> memo<R>(this Func<R> f)
            where R : class
        {
            R cache = null;
            return () => cache ??= f();
        }
    }
}