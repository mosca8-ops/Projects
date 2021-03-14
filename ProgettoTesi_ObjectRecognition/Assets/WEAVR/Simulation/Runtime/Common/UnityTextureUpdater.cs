using System;
using System.Runtime.InteropServices;

namespace TXT.WEAVR.Simulation
{
    public class UnityTextureUpdater
    {
    
        public enum SyncGroups
        {
            NoSync = -1,
            Instruments = 0,
            Skybox = 1,
            
        }

        [DllImport("UnityTextureUpdater", EntryPoint = "RegisterShm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterShm([MarshalAs(UnmanagedType.LPStr)] string iName, int iId, int iSize, int iSyncGroupId);
    
        [DllImport("UnityTextureUpdater", EntryPoint = "UnregisterShm", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void UnregisterShm(int iId);
    
        [DllImport("UnityTextureUpdater", EntryPoint = "ClearShmList", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearShmList();
    }
}
