// modified from SamsamTS's original Find It mod
// https://github.com/SamsamTS/CS-FindIt

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.DataBinding;
using ColossalFramework.Globalization;
using FindIt.GUI;
using System;

namespace FindIt
{
    public class Asset
    {
        private PrefabInfo m_prefab;

        public enum AssetType
        {
            Invalid = -1,
            All,
            Network,
            Ploppable,
            Growable,
            Rico,
            Prop,
            Decal,
            Tree
        }

        public enum PropType
        {
            Invalid = -1,
            PropsIndustrial,
            PropsParks,
            PropsCommon,
            PropsResidential,
            PropsBillboards,
            PropsSpecialBillboards,
            PropsLights,
            Natural,
            Unsorted,
            PropsMarker,
            PropsTree,
            PropsMoterVehicle,
            PropsRailwayVehicle,
            PropsAircraft,
            PropsWaterCraft,
            PropsUnsortedVehicle
        }

        public enum TreeType
        {
            Invalid = -1,
            SmallTree,
            MediumTree,
            LargeTree
        }

        public enum NetworkType
        {
            Invalid = -1,
            TinyRoads,
            SmallRoads,
            MediumRoads,
            LargeRoads,
            Highway,
            Path,
            Fence,
            WaterStructures,
            Utility,
            Train,
            Unsorted
        }

        public string name;
        public string title;

        public PrefabInfo prefab
        {
            get { return m_prefab; }
            set
            {
                m_prefab = value;

                if (m_prefab != null)
                {
                    service = m_prefab.GetService();
                    subService = m_prefab.GetSubService();

                    // check if an asset is from a content creator pack and set up author names
                    isCCP = CheckCCP(m_prefab.m_dlcRequired);

                    BuildingInfo buildingPrefab = m_prefab as BuildingInfo;
                    if (buildingPrefab != null)
                    {
                        // check sub-buildings
                        if (buildingPrefab.m_placementStyle == ItemClass.Placement.Procedural && buildingPrefab.m_buildingAI.GetType() != typeof(BuildingAI))
                        {
                            isSubBuilding = true;
                        }

                        else if (buildingPrefab.m_placementStyle != ItemClass.Placement.Manual)
                        {
                            assetType = AssetType.Growable;
                        }
                        else
                        {
                            assetType = AssetType.Ploppable;
                        }

                        size = new Vector2(buildingPrefab.m_cellWidth, buildingPrefab.m_cellLength);
                        if (buildingPrefab.m_generatedInfo?.m_heights != null)
                        {
                            if (buildingPrefab.m_generatedInfo.m_heights.Length != 0)
                            {
                                buildingHeight = buildingPrefab.m_generatedInfo.m_heights.Max();
                            }
                        }
                        SetFindIt2Description();
                        return;
                    }

                    PropInfo propPrefab = m_prefab as PropInfo;
                    if (propPrefab != null)
                    {
                        assetType = AssetType.Prop;
                        propType = SetPropType(prefab.editorCategory);

                        // For whatever reason CO thinks kid paddle cars are props marker. We fix it here
                        if (propType == Asset.PropType.PropsMarker && m_prefab.name.StartsWith("Paddle Car")) propType = Asset.PropType.Unsorted;

                        if (propPrefab.m_material != null)
                        {
                            if (propPrefab.m_material.shader == AssetTagList.shaderBlend || propPrefab.m_material.shader == AssetTagList.shaderSolid)
                            {
                                assetType = AssetType.Decal;
                            }
                        }
                        SetFindIt2Description();
                        return;
                    }

                    else if (m_prefab is TreeInfo)
                    {
                        assetType = AssetType.Tree;
                        TreeInfo info = m_prefab as TreeInfo;
                        treeType = SetTreeType(info);
                    }

                    else if (m_prefab is NetInfo)
                    {
                        assetType = AssetType.Network;
                        NetInfo info = m_prefab as NetInfo;
                        uiPriority = m_prefab.m_UIPriority;
                        networkType = SetNetworkType(info);
                    }

                    SetFindIt2Description();
                }
            }
        }

        public void RefreshRico()
        {
            if (FindIt.isRicoEnabled && m_prefab != null && assetType == AssetType.Ploppable)
            {
                service = m_prefab.GetService();
                subService = m_prefab.GetSubService();

                if (service == ItemClass.Service.Residential ||
                   service == ItemClass.Service.Industrial ||
                   service == ItemClass.Service.Commercial ||
                   service == ItemClass.Service.Office)
                {
                    assetType = AssetType.Rico;
                    SetFindIt2Description();
                }
            }
        }

        public AssetType assetType = AssetType.Invalid;
        public ItemClass.Service service = ItemClass.Service.None;
        public ItemClass.SubService subService = ItemClass.SubService.None;
        public Vector2 size;
        public ulong steamID;
        public string author;
        public float score;
        public ulong downloadTime;
        public uint createdTime;
        public uint updatedTime;
        public bool isCCP = false;
        public bool isSubBuilding = false;
        public float buildingHeight = 0;
        public PropType propType = PropType.Invalid;
        public TreeType treeType = TreeType.Invalid;
        public NetworkType networkType = NetworkType.Invalid;
        public string findIt2Description;
        public int uiPriority = int.MaxValue;
        public uint instanceCount = 0;
        public uint poInstanceCount = 0;

        public delegate void OnButtonClicked(UIComponent comp);
        public OnButtonClicked onButtonClicked;

        public HashSet<string> tagsTitle = new HashSet<string>();
        public HashSet<string> tagsDesc = new HashSet<string>();
        public HashSet<string> tagsCustom = new HashSet<string>();

        public static string GetName(PrefabInfo prefab)
        {
            string name = prefab.name;
            if (name.EndsWith("_Data"))
            {
                name = name.Substring(0, name.LastIndexOf("_Data"));
            }
            return name;
        }

        public static string GetLocalizedTitle(PrefabInfo prefab)
        {
            string name = prefab.name;

            if (prefab is BuildingInfo)
            {
                if (!Locale.GetUnchecked("BUILDING_TITLE", prefab.name, out name))
                {
                    name = prefab.name;
                }
                else
                {
                    name = name.Replace(".", "");
                }
            }
            else if (prefab is PropInfo)
            {
                if (!Locale.GetUnchecked("PROPS_TITLE", prefab.name, out name))
                {
                    name = prefab.name;
                }
                else
                {
                    name = name.Replace(".", "");
                }
            }
            else if (prefab is TreeInfo)
            {
                if (!Locale.GetUnchecked("TREE_TITLE", prefab.name, out name))
                {
                    name = prefab.name;
                }
                else
                {
                    name = name.Replace(".", "");
                }
            }
            else if (prefab is NetInfo)
            {
                if (!Locale.GetUnchecked("NET_TITLE", prefab.name, out name))
                {
                    name = prefab.name;
                }
                else
                {
                    name = name.Replace(".", "");
                }
            }

            int index = name.IndexOf('.');
            if (index >= 0)
            {
                name = name.Substring(index + 1);
            }

            if (name.IsNullOrWhiteSpace())
            {
                name = prefab.name;
            }

            index = name.LastIndexOf("_Data");
            if (index >= 0)
            {
                name = name.Substring(0, index);
            }

            /*
            name = Regex.Replace(name, @"[_-]+", " ");
            name = Regex.Replace(name, @"([A-Z][a-z]+)", " $1");
            name = Regex.Replace(name, @"([^\d])(\d+)", "$1 $2");
            name = Regex.Replace(name, @"\b(.) (\d+)", " $1$2 ");
            name = Regex.Replace(name, @"(\d+) ?x (\d+)", " $1x$2 ");
            name = Regex.Replace(name, @"\s+", " ");

            name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name).Trim();
            */

            return name;
        }

        public static string GetLocalizedDescription(PrefabInfo prefab)
        {
            string result;

            if (prefab is BuildingInfo)
            {
                if (Locale.GetUnchecked("BUILDING_DESC", prefab.name, out result))
                {
                    return result;
                }
            }
            else if (prefab is PropInfo)
            {
                if (Locale.GetUnchecked("PROPS_DESC", prefab.name, out result))
                {
                    return result;
                }
            }
            else if (prefab is TreeInfo)
            {
                if (Locale.GetUnchecked("TREE_DESC", prefab.name, out result))
                {
                    return result;
                }
            }
            else if (prefab is NetInfo)
            {
                if (Locale.GetUnchecked("NET_DESC", prefab.name, out result))
                {
                    return result;
                }
            }

            return "";
        }

        public static string GetLocalizedTooltip(Asset asset, PrefabInfo prefab, string title)
        {
            MilestoneInfo unlockMilestone = prefab.GetUnlockMilestone();

            string text = TooltipHelper.Format(new string[]
            {
                LocaleFormatter.Title,
                title,
                LocaleFormatter.Sprite,
                (!string.IsNullOrEmpty(prefab.m_InfoTooltipThumbnail)) ? prefab.m_InfoTooltipThumbnail : prefab.name,
                LocaleFormatter.Text,
                (asset.findIt2Description + Asset.GetLocalizedDescription(prefab)),
                LocaleFormatter.Locked,
                (!ToolsModifierControl.IsUnlocked(unlockMilestone)).ToString()
            });

            ToolsModifierControl.GetUnlockingInfo(unlockMilestone, out string unlockDesc, out string currentValue, out string targetValue, out string progress, out string locked);

            string addTooltip = TooltipHelper.Format(new string[]
            {
                LocaleFormatter.LockedInfo,
                locked,
                LocaleFormatter.UnlockDesc,
                unlockDesc,
                LocaleFormatter.UnlockPopulationProgressText,
                progress,
                LocaleFormatter.UnlockPopulationTarget,
                targetValue,
                LocaleFormatter.UnlockPopulationCurrent,
                currentValue
            });

            text = TooltipHelper.Append(text, addTooltip);
            PrefabAI aI = prefab.GetAI();
            if (aI != null)
            {
                text = TooltipHelper.Append(text, aI.GetLocalizedTooltip());
            }

            if (prefab is PropInfo || prefab is TreeInfo)
            {
                text = TooltipHelper.Append(text, TooltipHelper.Format(new string[]
                {
                    LocaleFormatter.Cost,
                    LocaleFormatter.FormatCost(prefab.GetConstructionCost(), false)
                }));
            }

            return text;
        }

        // check the type of a prop based on editor category
        private Asset.PropType SetPropType(string propEditorCategory)
        {
            if (propEditorCategory.StartsWith("PropsIndustrial")) return Asset.PropType.PropsIndustrial;
            else if (propEditorCategory.StartsWith("PropsParks")) return Asset.PropType.PropsParks;

            else if (propEditorCategory.StartsWith("PropsCommonLights") || propEditorCategory.StartsWith("PropsCommonStreets"))
                return Asset.PropType.PropsLights;

            else if (propEditorCategory.StartsWith("PropsCommon")) return Asset.PropType.PropsCommon;
            else if (propEditorCategory.StartsWith("PropsResidential")) return Asset.PropType.PropsResidential;
            else if (propEditorCategory.StartsWith("PropsBillboards")) return Asset.PropType.PropsBillboards;
            else if (propEditorCategory.StartsWith("PropsSpecialBillboards")) return Asset.PropType.PropsSpecialBillboards;
            else if (propEditorCategory.StartsWith("PropsRocks")) return Asset.PropType.Natural;
            else if (propEditorCategory.StartsWith("Beautification")) return Asset.PropType.Natural;
            else if (propEditorCategory.StartsWith("PropsMarker")) return Asset.PropType.PropsMarker;

            return Asset.PropType.Unsorted;
        }

        // check the size of a tree and decide its type. Same catergorization as the vanilla game
        private Asset.TreeType SetTreeType(TreeInfo info)
        {
            if (info == null) return Asset.TreeType.Invalid;

            float size = info.m_generatedInfo.m_size.y * (info.m_minScale + info.m_maxScale);

            if (size <= 16f) return Asset.TreeType.SmallTree;
            if (size > 16f && size <= 30f) return Asset.TreeType.MediumTree;
            //if (size > 30f) 
            return Asset.TreeType.LargeTree;
        }

        // check the type of network assets. A bit ugly and messy but I don't know a better way to do the categorization.
        // Network Extension 2 adds its own types
        private Asset.NetworkType SetNetworkType(NetInfo info)
        {
            if (info == null) return Asset.NetworkType.Invalid;

            else if (info.category.StartsWith("RoadsTiny"))
            {
                AssetTagList.instance.tinyRoadsExist = true;
                return Asset.NetworkType.TinyRoads;
            }

            else if (info.category.StartsWith("RoadsSmall")) return Asset.NetworkType.SmallRoads;
            else if (info.m_class.name.StartsWith("Small Road")) return Asset.NetworkType.SmallRoads;
            else if (info.category.StartsWith("RoadsSmallHV")) return Asset.NetworkType.SmallRoads;

            else if (info.category.StartsWith("RoadsMedium")) return Asset.NetworkType.MediumRoads;
            else if (info.m_class.name.StartsWith("Medium Road")) return Asset.NetworkType.MediumRoads;

            else if (info.category.StartsWith("RoadsLarge")) return Asset.NetworkType.LargeRoads;
            else if (info.m_class.name.StartsWith("Large Road")) return Asset.NetworkType.LargeRoads;

            else if (info.category.StartsWith("RoadsHighway")) return Asset.NetworkType.Highway;

            else if (info.m_class.name.StartsWith("Pedestrian")) return Asset.NetworkType.Path;
            else if (info.category.StartsWith("RoadsPedestrians")) return Asset.NetworkType.Path;

            else if (info.editorCategory.StartsWith("LandscapingWaterStructures")) return Asset.NetworkType.WaterStructures;

            else if (info.editorCategory.StartsWith("LandscapingFences")) return Asset.NetworkType.Fence;
            else if (info.m_class.name.StartsWith("Beautification")) return Asset.NetworkType.Fence;
            else if (info.m_class.name.StartsWith("Landscaping Quay")) return Asset.NetworkType.Fence;

            else if (info.m_class.m_service == ItemClass.Service.Electricity) return Asset.NetworkType.Utility;
            else if (info.m_class.m_service == ItemClass.Service.Water) return Asset.NetworkType.Utility;

            else if (info.m_class.m_subService == ItemClass.SubService.PublicTransportTrain) return Asset.NetworkType.Train;

            return NetworkType.Unsorted;
        }

        /// <summary>
        /// Add Find It 2 description: asset type, sub-type, size, height
        /// </summary>
        public void SetFindIt2Description()
        {
            if (assetType == AssetType.Decal)
            {
                findIt2Description = Translations.Translate("FIF_SE_ID");
            }
            else if (assetType == AssetType.Network)
            {
                findIt2Description = $"{Translations.Translate("FIF_SE_IN")}, ";

                if (networkType == Asset.NetworkType.TinyRoads) findIt2Description += Translations.Translate("FIF_NET_TNR");
                else if (networkType == Asset.NetworkType.SmallRoads) findIt2Description += Translations.Translate("FIF_NET_SMR");
                else if (networkType == Asset.NetworkType.MediumRoads) findIt2Description += Translations.Translate("FIF_NET_MDR");
                else if (networkType == Asset.NetworkType.LargeRoads) findIt2Description += Translations.Translate("FIF_NET_LGR");
                else if (networkType == Asset.NetworkType.Highway) findIt2Description += Translations.Translate("FIF_NET_HGHW");
                else if (networkType == Asset.NetworkType.Path) findIt2Description += Translations.Translate("FIF_NET_PATH");
                else if (networkType == Asset.NetworkType.Fence) findIt2Description += Translations.Translate("FIF_NET_WALL");
                else if (networkType == Asset.NetworkType.WaterStructures) findIt2Description += Translations.Translate("FIF_NET_WAT");
                else if (networkType == Asset.NetworkType.Utility) findIt2Description += Translations.Translate("FIF_NET_UTI");
                else if (networkType == Asset.NetworkType.Train) findIt2Description += Translations.Translate("FIF_NET_TRA");
                else if (networkType == Asset.NetworkType.Unsorted) findIt2Description += Translations.Translate("FIF_PROP_UNS");

                if (UIFilterNetwork.IsNormalRoads(networkType))
                {
                    NetInfo info = prefab as NetInfo;
                    if (UIFilterNetwork.IsOneWay(info)) findIt2Description += ($", {Translations.Translate("FIF_NET_ONE")}");

                    if (UIFilterNetwork.HasParking(info)) findIt2Description += ($", {Translations.Translate("FIF_NET_PAR")}");
                    else findIt2Description += ($", {Translations.Translate("FIF_NET_NOP")}");
                }
            }
            else if (assetType == AssetType.Growable || assetType == AssetType.Rico)
            {
                if (assetType == AssetType.Rico)
                {
                    findIt2Description = $"{Translations.Translate("FIF_SE_IR")}, ";
                }
                else
                {
                    findIt2Description = $"{Translations.Translate("FIF_SE_IG")}, ";
                }
                if (subService == ItemClass.SubService.ResidentialLow) findIt2Description += Translations.Translate("FIF_GR_LDR");
                else if (subService == ItemClass.SubService.ResidentialHigh) findIt2Description += Translations.Translate("FIF_GR_HDR");
                else if (subService == ItemClass.SubService.ResidentialLowEco) findIt2Description += Translations.Translate("FIF_GR_LDRE");
                else if (subService == ItemClass.SubService.ResidentialHighEco) findIt2Description += Translations.Translate("FIF_GR_HDRE");
                else if (subService == ItemClass.SubService.CommercialLow) findIt2Description += Translations.Translate("FIF_GR_LDC");
                else if (subService == ItemClass.SubService.CommercialHigh) findIt2Description += Translations.Translate("FIF_GR_HDC");
                else if (subService == ItemClass.SubService.CommercialEco) findIt2Description += Translations.Translate("FIF_GR_CE");
                else if (subService == ItemClass.SubService.CommercialLeisure) findIt2Description += Translations.Translate("FIF_GR_LC");
                else if (subService == ItemClass.SubService.CommercialTourist) findIt2Description += Translations.Translate("FIF_GR_TC");
                else if (subService == ItemClass.SubService.IndustrialGeneric) findIt2Description += Translations.Translate("FIF_GR_GI");
                else if (subService == ItemClass.SubService.IndustrialFarming) findIt2Description += Translations.Translate("FIF_GR_FAI");
                else if (subService == ItemClass.SubService.IndustrialForestry) findIt2Description += Translations.Translate("FIF_GR_FOI");
                else if (subService == ItemClass.SubService.IndustrialOil) findIt2Description += Translations.Translate("FIF_GR_OII");
                else if (subService == ItemClass.SubService.IndustrialOre) findIt2Description += Translations.Translate("FIF_GR_ORI");
                else if (subService == ItemClass.SubService.OfficeGeneric) findIt2Description += Translations.Translate("FIF_GR_O");
                else if (subService == ItemClass.SubService.OfficeHightech) findIt2Description += Translations.Translate("FIF_GR_ITC");
                else findIt2Description += Translations.Translate("FIF_PROP_UNS");

                findIt2Description += $", {size.x}x{size.y}, {(int)buildingHeight} {Translations.Translate("FIF_EF_MET")} ({(int)(buildingHeight * 3.28084f)} {Translations.Translate("FIF_EF_FEE")})";
                BuildingInfo buildingInfo = m_prefab as BuildingInfo;
                // terrain conforming?
                if (buildingInfo.m_material?.shader == AssetTagList.shaderBuildingFence) findIt2Description += $", {Translations.Translate("FIF_PROP_TC")}";
            }
            else if (assetType == AssetType.Prop)
            {
                findIt2Description = $"{Translations.Translate("FIF_SE_IPR")}, ";
                if (propType == Asset.PropType.PropsIndustrial) findIt2Description += Translations.Translate("FIF_PROP_IND");
                else if (propType == Asset.PropType.PropsParks) findIt2Description += Translations.Translate("FIF_PROP_PAR");
                else if (propType == Asset.PropType.PropsCommon) findIt2Description += Translations.Translate("FIF_PROP_COM");
                else if (propType == Asset.PropType.PropsResidential) findIt2Description += Translations.Translate("FIF_PROP_RES");
                else if (propType == Asset.PropType.PropsBillboards) findIt2Description += Translations.Translate("FIF_PROP_BIL");
                else if (propType == Asset.PropType.PropsSpecialBillboards) findIt2Description += Translations.Translate("FIF_PROP_SPE");
                else if (propType == Asset.PropType.PropsLights) findIt2Description += Translations.Translate("FIF_PROP_LIG");
                else if (propType == Asset.PropType.Natural) findIt2Description += Translations.Translate("FIF_PROP_NAT");
                else if (propType == Asset.PropType.Unsorted) findIt2Description += Translations.Translate("FIF_PROP_UNS");
                else if (propType == Asset.PropType.PropsMarker) findIt2Description += Translations.Translate("FIF_PROP_MAR");
                else if (propType == Asset.PropType.PropsUnsortedVehicle) findIt2Description += Translations.Translate("FIF_PROP_VEH");
                else if (propType == Asset.PropType.PropsTree) findIt2Description += Translations.Translate("FIF_PROP_TRE");
                else if (propType == Asset.PropType.PropsMoterVehicle) findIt2Description += Translations.Translate("FIF_PROP_MOT");
                else if (propType == Asset.PropType.PropsRailwayVehicle) findIt2Description += Translations.Translate("FIF_PROP_RAI");
                else if (propType == Asset.PropType.PropsAircraft) findIt2Description += Translations.Translate("FIF_PROP_AIR");
                else if (propType == Asset.PropType.PropsWaterCraft) findIt2Description += Translations.Translate("FIF_PROP_WAT");

                PropInfo propInfo = m_prefab as PropInfo;
                // terrain conforming?
                if (propInfo.m_material?.shader == AssetTagList.shaderPropFence) findIt2Description += $", {Translations.Translate("FIF_PROP_TC")}";
            }
            else if (assetType == AssetType.Tree)
            {
                findIt2Description = $"{Translations.Translate("FIF_SE_IT")}, ";
                if (treeType == Asset.TreeType.SmallTree) findIt2Description += Translations.Translate("FIF_TREE_SM");
                else if (treeType == Asset.TreeType.MediumTree) findIt2Description += Translations.Translate("FIF_TREE_MD");
                else if (treeType == Asset.TreeType.LargeTree) findIt2Description += Translations.Translate("FIF_TREE_LG");
            }
            else if (assetType == AssetType.Ploppable)
            {
                findIt2Description = $"{Translations.Translate("FIF_SE_IP")}, ";

                if (service == ItemClass.Service.Electricity) findIt2Description += Translations.Translate("FIF_PLOP_E");
                else if (service == ItemClass.Service.Water) findIt2Description += Translations.Translate("FIF_PLOP_W");
                else if (service == ItemClass.Service.Garbage) findIt2Description += Translations.Translate("FIF_PLOP_G");
                else if (service == ItemClass.Service.PlayerIndustry) findIt2Description += Translations.Translate("FIF_PLOP_I");
                else if (service == ItemClass.Service.Fishing) findIt2Description += Translations.Translate("FIF_PLOP_FI");
                else if (service == ItemClass.Service.HealthCare) findIt2Description += Translations.Translate("FIF_PLOP_H");
                else if (service == ItemClass.Service.FireDepartment) findIt2Description += Translations.Translate("FIF_PLOP_F");
                else if (service == ItemClass.Service.Disaster) findIt2Description += Translations.Translate("FIF_PLOP_D");
                else if (service == ItemClass.Service.PoliceDepartment) findIt2Description += Translations.Translate("FIF_PLOP_P");
                else if (service == ItemClass.Service.Education) findIt2Description += Translations.Translate("FIF_PLOP_ED");
                else if (service == ItemClass.Service.PlayerEducation) findIt2Description += Translations.Translate("FIF_PLOP_C");
                else if (service == ItemClass.Service.Museums) findIt2Description += Translations.Translate("FIF_PLOP_C");
                else if (service == ItemClass.Service.VarsitySports) findIt2Description += Translations.Translate("FIF_PLOP_V");
                else if (service == ItemClass.Service.PublicTransport) findIt2Description += Translations.Translate("FIF_PLOP_PT");
                else if (service == ItemClass.Service.Tourism) findIt2Description += Translations.Translate("FIF_PLOP_PT");
                else if (service == ItemClass.Service.Beautification) findIt2Description += Translations.Translate("FIF_PLOP_PPW");
                else if (service == ItemClass.Service.Monument) findIt2Description += Translations.Translate("FIF_PLOP_U");
                else findIt2Description += Translations.Translate("FIF_PROP_UNS");

                findIt2Description += $", {size.x}x{size.y}, {(int)buildingHeight} {Translations.Translate("FIF_EF_MET")} ({(int)(buildingHeight * 3.28084f)} {Translations.Translate("FIF_EF_FEE")})";
                BuildingInfo buildingInfo = m_prefab as BuildingInfo;
                // terrain conforming?
                if (buildingInfo.m_material?.shader == AssetTagList.shaderBuildingFence) findIt2Description += $", {Translations.Translate("FIF_PROP_TC")}";
            }

            findIt2Description += "\n";
        }

        private bool CheckCCP(SteamHelper.DLC_BitMask dlc)
        {
            if (dlc == SteamHelper.DLC_BitMask.ModderPack1) author = "Shroomblaze";
            else if (dlc == SteamHelper.DLC_BitMask.ModderPack2) author = "GCVos";
            else if (dlc == SteamHelper.DLC_BitMask.ModderPack3) author = "Avanya";
            else if (dlc == SteamHelper.DLC_BitMask.ModderPack4) author = "KingLeno";
            else if (dlc == SteamHelper.DLC_BitMask.ModderPack5) author = "AmiPolizeiFunk";
            else if (dlc == SteamHelper.DLC_BitMask.ModderPack6) author = "Ryuichi Kaminogi";
            else if (dlc == SteamHelper.DLC_BitMask.ModderPack7) author = "BadPeanut";
            else if (dlc == SteamHelper.DLC_BitMask.ModderPack8) author = "Armesto";

            else return false;

            return true;
        }

        /// <summary>
        /// Set up prop types of vehicle props generated by Elektrix's TVP mod. Need the TVP Patch mod
        /// </summary>
        public void SetVehiclePropType(Dictionary<PropInfo, VehicleInfo> propVehicleInfoTable, PropInfo info)
        {
            propType = Asset.PropType.PropsUnsortedVehicle;
            if (!propVehicleInfoTable.ContainsKey(info)) return;
            PrefabAI ai = propVehicleInfoTable[info].GetAI();
            if (ai == null) return;
            Type aiType = ai.GetType();

            if (IsMotorVehicle(aiType)) propType = Asset.PropType.PropsMoterVehicle;
            else if (IsRailwayVehicle(aiType)) propType = Asset.PropType.PropsRailwayVehicle;
            else if (IsAircraft(aiType)) propType = Asset.PropType.PropsAircraft;
            else if (IsWatercraft(aiType)) propType = Asset.PropType.PropsWaterCraft;
        }

        public bool IsMotorVehicle(Type type)
        {
            if (type.IsSubclassOf(typeof(CarAI))) return true;
            return false;
        }

        public bool IsRailwayVehicle(Type type)
        {
            if (type.IsSubclassOf(typeof(TrainAI))) return true;
            if (type.IsSubclassOf(typeof(TramBaseAI))) return true;
            return false;
        }

        public bool IsAircraft(Type type)
        {
            if (type.IsSubclassOf(typeof(AircraftAI))) return true;
            if (type.IsSubclassOf(typeof(HelicopterAI))) return true;
            if (type.IsSubclassOf(typeof(BlimpAI))) return true;
            if (type == (typeof(PassengerHelicopterAI))) return true;
            if (type == (typeof(PassengerPlaneAI))) return true;
            if (type == (typeof(PrivatePlaneAI))) return true;
            if (type == (typeof(BalloonAI))) return true;
            if (type == (typeof(CargoPlaneAI))) return true;
            if (type == (typeof(RocketAI))) return true;
            return false;
        }

        public bool IsWatercraft(Type type)
        {
            if (type.IsSubclassOf(typeof(FerryAI))) return true;
            if (type.IsSubclassOf(typeof(ShipAI))) return true;
            return false;
        }
    }
}
