using System;
using System.Collections.Generic;
using System.IO;
using ColossalFramework;
using FindIt.GUI;
using ColossalFramework.UI;
using ColossalFramework.IO;

namespace FindIt
{
    public static class ExportUnunsedTool
    {
        public static void ExportUnused()
        {
            // get steam id of all workship assets
            HashSet<ulong> steamIds = new HashSet<ulong>();
            foreach (Asset asset in AssetTagList.instance.assets.Values)
            {
                if (!asset.prefab.m_isCustomContent) continue;
                if (asset.steamID == 0) continue;
                steamIds.Add(asset.steamID);
            }

            // update instance count
            AssetTagList.instance.UpdatePrefabInstanceCount(UISearchBox.DropDownOptions.All);
            if (FindIt.isPOEnabled && Settings.includePOinstances) FindIt.instance.POTool.UpdatePOInfoList();

            // filter out used assets
            foreach (Asset asset in AssetTagList.instance.assets.Values)
            {
                if (!asset.prefab.m_isCustomContent) continue;
                if (asset.steamID == 0) continue;
                AssetTagList.instance.UpdateAssetInstanceCount(asset);
                if (asset.instanceCount > 0 || asset.poInstanceCount > 0) 
                {
                    steamIds.Remove(asset.steamID);
                }
            }

            string path = Path.Combine(DataLocation.localApplicationData, "FindItExportUnusedWorkshopID.html");
            if (File.Exists(path)) File.Delete(path);

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, true))
            {
                file.WriteLine($"City Name: {GetCityName()}<br>");
                file.WriteLine($"Export Date: {GetFormattedDateTime()}<br>");
                file.WriteLine($"<br>This list only considers asset types that are monitored by Find It 2<br>");
                file.WriteLine($"<br>If you ever copied assets from the workshop download folder to the local asset folder, the information here can be inaccurate<br>");
                foreach (ulong id in steamIds)
                {
                    file.WriteLine($"<br><a href=\"https://steamcommunity.com/sharedfiles/filedetails/?id={id}\">{id}</a><br>");
                }
            }

            ExceptionPanel panel = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
            panel.SetMessage("Find It 2", $"FindItExportUnusedWorkshopID.html is exported.\n\nIt only considers asset types that are monitored by Find It 2.\n\n{path}", false);
        }

        private static string GetCityName()
        {
            if (Singleton<SimulationManager>.exists)
            {
                string cityName = Singleton<SimulationManager>.instance.m_metaData.m_CityName;
                if (cityName != null) return cityName;
            }
            return "";
        }

        private static string GetFormattedDateTime()
        {
            return DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
        }
    }
}
