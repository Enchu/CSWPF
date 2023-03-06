using System;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CSWPF.Helpers;

public class MobileAuthenticator
{
    private const byte CodeInterval = 30;
    internal const byte CodeDigits = 5;
    private const byte SteamTimeTTL = 15;
    private static readonly SemaphoreSlim TimeSemaphore = new(1, 1);
    internal static readonly ImmutableSortedSet<char> CodeCharacters = ImmutableSortedSet.Create('2', '3', '4', '5', '6', '7', '8', '9', 'B', 'C', 'D', 'F', 'G', 'H', 'J', 'K', 'M', 'N', 'P', 'Q', 'R', 'T', 'V', 'W', 'X', 'Y');
    [JsonProperty("shared_secret", Required = Required.Always)]
    private static readonly string SharedSecret = "";
    private static int? SteamTimeDifference;
    private static DateTime LastSteamTimeCheck;

    static public async Task<string?> GenerateToken()
    {
        ulong time = await GetSteamTime().ConfigureAwait(false);

        if (time == 0) {
            throw new InvalidOperationException(nameof(time));
        }

        return GenerateTokenForTime(time);
    }

    private static string? GenerateTokenForTime(ulong time)
    {
        byte[] sharedSecret;
        try
        {
            sharedSecret = Convert.FromBase64String(SharedSecret);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
        byte[] timeArray = BitConverter.GetBytes(time / CodeInterval);

        if (BitConverter.IsLittleEndian) {
            Array.Reverse(timeArray);
        }
        
#pragma warning disable CA5350        
        byte[] hash = HMACSHA1.HashData(sharedSecret, timeArray);
#pragma warning restore CA5350
        
        int start = hash[^1] & 0x0f;
        byte[] bytes = new byte[4];
        
        Array.Copy(hash, start, bytes, 0, 4);
        
        if (BitConverter.IsLittleEndian) {
            Array.Reverse(bytes);
        }
        
        uint fullCode = BitConverter.ToUInt32(bytes, 0) & 0x7fffffff;
        return String.Create(
            CodeDigits, fullCode, static (buffer, state) => {
                for (byte i = 0; i < CodeDigits; i++) {
                    buffer[i] = CodeCharacters[(byte) (state % CodeCharacters.Count)];
                    state /= (byte) CodeCharacters.Count;
                }
            }
        );
        
    }
    private static async Task<ulong> GetSteamTime() {
        int? steamTimeDifference = SteamTimeDifference;

        if (steamTimeDifference.HasValue && (DateTime.UtcNow.Subtract(LastSteamTimeCheck).TotalMinutes < SteamTimeTTL)) {
            return Utilities.MathAdd(Utilities.GetUnixTime(), steamTimeDifference.Value);
        }

        await TimeSemaphore.WaitAsync().ConfigureAwait(false);

        try {
            steamTimeDifference = SteamTimeDifference;

            if (steamTimeDifference.HasValue && (DateTime.UtcNow.Subtract(LastSteamTimeCheck).TotalMinutes < SteamTimeTTL)) {
                return Utilities.MathAdd(Utilities.GetUnixTime(), steamTimeDifference.Value);
            }

            ulong serverTime = await Web.WebHandler.GetServerTime().ConfigureAwait(false);

            if (serverTime == 0) {
                return Utilities.GetUnixTime();
            }
            
            steamTimeDifference = unchecked((int) (serverTime - Utilities.GetUnixTime()));

            SteamTimeDifference = steamTimeDifference;
            LastSteamTimeCheck = DateTime.UtcNow;
        } finally {
            TimeSemaphore.Release();
        }

        return Utilities.MathAdd(Utilities.GetUnixTime(), steamTimeDifference.Value);
    }
}