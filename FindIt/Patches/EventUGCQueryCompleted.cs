using ColossalFramework.PlatformServices;
using HarmonyLib;
using System.Collections.Generic;

namespace FindIt
{
    [HarmonyPatch(typeof(Workshop))]
    [HarmonyPatch("EventUGCQueryCompleted")]
    internal static class EventUGCQueryCompletedPatch
    {
        public static Dictionary<ulong, uint> createdTimes = new Dictionary<ulong, uint>();
        public static Dictionary<ulong, uint> updatedTimes = new Dictionary<ulong, uint>();

        private static void Postfix(ref UGCDetails result, ref bool ioError)
        {
            if (ioError) return;

            ulong steamid = ulong.Parse(result.publishedFileId.ToString());
            uint timeCreated = result.timeCreated;
            uint timeUpdated = result.timeUpdated;

            if (!createdTimes.ContainsKey(steamid))
            {
                createdTimes.Add(steamid, timeCreated);
            }
            if (!updatedTimes.ContainsKey(steamid))
            {
                updatedTimes.Add(steamid, timeUpdated);
            }
        }

    }
}
