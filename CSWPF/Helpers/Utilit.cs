using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace CSWPF.Helpers;

public class Utilit
{
    [PublicAPI]
    public static ulong GetUnixTime() => (ulong) DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    internal static ulong MathAdd(ulong first, int second) {
        if (second >= 0) {
            return first + (uint) second;
        }

        return first - (uint) -second;
    }
    
    public static async void InBackground(Action action, bool longRunning = false) {
        ArgumentNullException.ThrowIfNull(action);

        TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;

        if (longRunning) {
            options |= TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness;
        }

        await Task.Factory.StartNew(action, CancellationToken.None, options, TaskScheduler.Default).ConfigureAwait(false);
    }
    
    public static void InBackground<T>(Func<T> function, bool longRunning = false) {
        ArgumentNullException.ThrowIfNull(function);

        InBackground(void() => function(), longRunning);
    }
}