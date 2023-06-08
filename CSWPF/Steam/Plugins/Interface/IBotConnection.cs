using System.Threading.Tasks;
using CSWPF.Web;
using SteamKit2;

namespace CSWPF.Steam.Plugns.Interface;

public interface IBotConnection : IPlugin {
    /// <summary>
    ///     ASF will call this method when bot gets disconnected from Steam network.
    /// </summary>
    /// <param name="bot">Bot object related to this callback.</param>
    /// <param name="reason">Reason for disconnection, or <see cref="EResult.OK" /> if the disconnection was initiated by ASF (e.g. as a result of a command).</param>
    Task OnBotDisconnected(Bot bot, EResult reason);

    /// <summary>
    ///     ASF will call this method when bot successfully connects to Steam network.
    /// </summary>
    /// <param name="bot">Bot object related to this callback.</param>
    Task OnBotLoggedOn(Bot bot);
}