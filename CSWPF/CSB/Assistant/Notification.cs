using System.Runtime.InteropServices;

namespace CSWPF.Helpers.Data;

[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
public delegate void Notification(string str);
