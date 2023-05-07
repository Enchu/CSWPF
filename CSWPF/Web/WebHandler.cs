using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CSWPF.Helpers;
using CSWPF.Steam;
using JetBrains.Annotations;
using Microsoft.VisualBasic;
using SteamKit2;

namespace CSWPF.Web;

public sealed class WebHandler
{
    private const string TwoFactorService = "ITwoFactorService";
    private static ushort WebLimiterDelay => ASF.GlobalConfig?.WebLimiterDelay ?? GlobalConfig.DefaultWebLimiterDelay;
    
    internal HttpClient GenerateDisposableHttpClient() => WebBrowser.GenerateDisposableHttpClient();
    internal static async Task<ulong> GetServerTime() {
        KeyValue? response = null;

        for (byte i = 0; (i < WebBrowser.MaxTries) && (response == null); i++) {
            if ((i > 0) && (WebLimiterDelay > 0)) {
                await Task.Delay(WebLimiterDelay).ConfigureAwait(false);
            }

            using WebAPI.AsyncInterface twoFactorService = Bot.SteamConfiguration.GetAsyncWebAPIInterface(TwoFactorService);

            twoFactorService.Timeout = WebBrowser.Timeout;

            try {
                response = await WebLimitRequest(
                    WebAPI.DefaultBaseAddress,
                    async () => await twoFactorService.CallAsync(HttpMethod.Post, "QueryTime").ConfigureAwait(false)
                ).ConfigureAwait(false);
            } catch (TaskCanceledException e)
            {
                MessageBox.Show("" + e);
            } catch (Exception e)
            {
                MessageBox.Show("" + e);
            }
        }
        
        if (response == null) {
            return 0;
        }
        
        ulong result = response["server_time"].AsUnsignedLong();
        
        if (result == 0) {
            return 0;
        }

        return result;
    }
    
    public static async Task<T> WebLimitRequest<T>(Uri service, Func<Task<T>> function)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(function);

        if (WebLimiterDelay == 0)
        {
            return await function().ConfigureAwait(false);
        }

        if (!ASF.WebLimitingSemaphores.TryGetValue(service, out (ICrossProcessSemaphore RateLimitingSemaphore, SemaphoreSlim OpenConnectionsSemaphore) limiters))
        {
            limiters.RateLimitingSemaphore = ASF.RateLimitingSemaphore;
            limiters.OpenConnectionsSemaphore = ASF.OpenConnectionsSemaphore;
        }
        await limiters.OpenConnectionsSemaphore.WaitAsync().ConfigureAwait(false);
        
        try
        {
            await limiters.RateLimitingSemaphore.WaitAsync().ConfigureAwait(false);
            
            Utilities.InBackground(
                async () =>
                {
                    await Task.Delay(WebLimiterDelay).ConfigureAwait(false);
                    limiters.RateLimitingSemaphore.Release();
                }
            );

            return await function().ConfigureAwait(false);
        }
        finally
        {
            limiters.OpenConnectionsSemaphore.Release();
        }
        
    }
}