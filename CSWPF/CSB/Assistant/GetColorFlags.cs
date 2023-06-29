using System;
using System.Runtime.InteropServices;

namespace CSWPF.Helpers.Data;

[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
public delegate int GetColorFlags(IntPtr hwnd, int x, int y);