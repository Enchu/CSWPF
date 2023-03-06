using System.Threading.Tasks;

namespace CSWPF.Steam;

public interface ICrossProcessSemaphore
{
    void Release();
    Task WaitAsync();
    Task<bool> WaitAsync(int millisecondsTimeout);
}