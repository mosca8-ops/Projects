using System;
using System.Runtime.InteropServices;

namespace TXT.WEAVR.Simulation
{
  public class SharedMemory
  {

    [DllImport("WeavrSharedMemory", EntryPoint = "WeavrShmClose", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern int WeavrShmCloseAll();

    [DllImport("WeavrSharedMemory", EntryPoint = "WeavrShmClose", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern int WeavrShmClose([MarshalAs(UnmanagedType.LPStr)] string iName);
    
    [DllImport("WeavrSharedMemory", EntryPoint = "WeavrShmOpen", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr WeavrShmOpen([MarshalAs(UnmanagedType.LPStr)] string iName, int iSize, int iOffset);

    [DllImport("WeavrSharedMemory", EntryPoint = "WeavrShmCreate", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr WeavrShmCreate([MarshalAs(UnmanagedType.LPStr)] string iName, int iSize);

    [DllImport("WeavrSharedMemory", EntryPoint = "WeavrShmCreateOrOpen", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr WeavrShmCreateOrOpen([MarshalAs(UnmanagedType.LPStr)] string iName, int iSize);

    [DllImport("WeavrSharedMemory", EntryPoint = "GetWeavrShmPtr", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetWeavrShmPtr([MarshalAs(UnmanagedType.LPStr)] string iName, int iSize);

    [DllImport("WeavrSharedMemory", EntryPoint = "WeavrShmRead", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern void WeavrShmRead([MarshalAs(UnmanagedType.LPStr)] string  iName, IntPtr iPtr, int iOffset, int iSize);

    [DllImport("WeavrSharedMemory", EntryPoint = "WeavrShmWrite", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern void WeavrShmWrite([MarshalAs(UnmanagedType.LPStr)] string  iName, IntPtr iPtr, int iOffset, int iSize);
  }
}
