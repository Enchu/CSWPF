using System.Collections.Generic;
using System.Threading.Tasks;
using CSWPF.Web;
using SteamKit2;

namespace CSWPF.Steam.Plugns.Interface;

public interface ISteamPICSChanges : IPlugin {
	/// <summary>
	///     ASF uses this method for determining the point in time from which it should keep history going upon a restart. The actual point in time that will be used is calculated as the lowest change number from all loaded plugins, to guarantee that no plugin will miss any changes, while allowing possible duplicates for those plugins that were already synchronized with newer changes. If you don't care about persistent state and just want to receive the ongoing history, you should return 0 (which is equal to "I'm fine with any"). If there won't be any plugin asking for a specific point in time, ASF will start returning entries since the start of the program.
	/// </summary>
	/// <returns>The most recent change number from which you're fine to receive <see cref="OnPICSChanges" />.</returns>
	Task<uint> GetPreferredChangeNumberToStartFrom();

	/// <summary>
	///     ASF will call this method upon receiving any app/package PICS changes. The history is guaranteed to be precise and continuous starting from <see cref="GetPreferredChangeNumberToStartFrom" /> until <see cref="OnPICSChangesRestart" /> is called. It's possible for this method to have duplicated calls across different runs, in particular when some other plugin asks for lower <see cref="GetPreferredChangeNumberToStartFrom" />, therefore you should keep that in mind (and refer to change number of standalone apps/packages).
	/// </summary>
	/// <param name="currentChangeNumber">The change number of current callback.</param>
	/// <param name="appChanges">App changes that happened since the previous call of this method. Can be empty.</param>
	/// <param name="packageChanges">Package changes that happened since the previous call of this method. Can be empty.</param>
	Task OnPICSChanges(uint currentChangeNumber, IReadOnlyDictionary<uint, SteamApps.PICSChangesCallback.PICSChangeData> appChanges, IReadOnlyDictionary<uint, SteamApps.PICSChangesCallback.PICSChangeData> packageChanges);

	/// <summary>
	///     ASF will call this method when it'll be necessary to restart the history of PICS changes. This can happen due to Steam limitation in which we're unable to keep history going if we're too far behind (approx 5k changeNumbers). If you're relying on continuous history of app/package PICS changes sent by <see cref="OnPICSChanges" />, ASF can no longer guarantee that upon calling this method, therefore you should start clean.
	/// </summary>
	/// <param name="currentChangeNumber">The change number from which we're restarting the PICS history.</param>
	Task OnPICSChangesRestart(uint currentChangeNumber);
}