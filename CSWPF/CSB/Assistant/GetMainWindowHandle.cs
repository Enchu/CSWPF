using System;
using System.Runtime.InteropServices;

namespace CSWPF.Helpers.Data;

[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
public delegate IntPtr GetMainWindowHandle(AccountData acc);