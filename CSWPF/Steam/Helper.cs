using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace CSWPF.Steam;

public static class Helper
{
    public static async void InBackground(Action action, bool longRunning = false)
    {
        ArgumentNullException.ThrowIfNull(action);

        TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;

        if (longRunning)
        {
            options |= TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness;
        }

        await Task.Factory.StartNew(action, CancellationToken.None, options, TaskScheduler.Default).ConfigureAwait(false);
    }
    public static void InBackground<T>(Func<T> function, bool longRunning = false)
    {
        ArgumentNullException.ThrowIfNull(function);

        InBackground(void () => function(), longRunning);
    }
    public static bool IsClientErrorCode(this HttpStatusCode statusCode) => statusCode is >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError;
    public static bool IsRedirectionCode(this HttpStatusCode statusCode) => statusCode is >= HttpStatusCode.Ambiguous and < HttpStatusCode.BadRequest;
    public static bool IsServerErrorCode(this HttpStatusCode statusCode) => statusCode is >= HttpStatusCode.InternalServerError and < (HttpStatusCode)600;
    public static bool IsSuccessCode(this HttpStatusCode statusCode) => statusCode is >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous;
    public static Task<T> ToLongRunningTask<T>(this AsyncJob<T> job) where T : CallbackMsg
    {
        ArgumentNullException.ThrowIfNull(job);

        job.Timeout = TimeSpan.FromSeconds(60);

        return job.ToTask();
    }
}