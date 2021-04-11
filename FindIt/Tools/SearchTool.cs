using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using FindIt.GUI;

namespace FindIt
{
    public partial class AssetTagList
    {
        public List<Asset> matches = new List<Asset>();

        /// <summary>
        /// main backend search method. called by UISearchBox's search method
        /// </summary>
        public List<Asset> Find(string text, UISearchBox.DropDownOptions filter)
        {
            matches.Clear();
            text = text.ToLower().Trim();

            // if showing instance counts, refresh
            try
            {
                bool usingUsedUnusedFilterFlag = UISearchBox.instance?.extraFiltersPanel != null &&　(UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex == (int)UIFilterExtra.DropDownOptions.UnusedAssets ||
                    UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex == (int)UIFilterExtra.DropDownOptions.UsedAssets);

                if (Settings.showInstancesCounter || usingUsedUnusedFilterFlag)
                {
                    UpdatePrefabInstanceCount(filter);

                    if ((filter != UISearchBox.DropDownOptions.Tree) && (filter != UISearchBox.DropDownOptions.Network))
                    {
                        if (FindIt.isPOEnabled && (Settings.includePOinstances || usingUsedUnusedFilterFlag)) ProceduralObjectsTool.UpdatePOInfoList();
                    }
                }
            }
            catch (Exception ex)
            {
                Debugging.LogException(ex);
            }

            // if there is something in the search input box
            if (!text.IsNullOrWhiteSpace())
            {
                string[] keywords = Regex.Split(text, @"([^\w!#+%]|[-]|\s)+", RegexOptions.IgnoreCase);
                bool matched, orSearch;
                float score, orScore;

                foreach (Asset asset in assets.Values)
                {
                    asset.RefreshRico();
                    if (asset.prefab != null)
                    {
                        if (!CheckAssetFilters(asset, filter)) continue;
                        matched = true;
                        asset.score = 0;
                        score = 0;
                        orSearch = false;
                        orScore = 0;
                        foreach (string keyword in keywords)
                        {
                            if (!keyword.IsNullOrWhiteSpace())
                            {
                                if (keyword == "!" || keyword == "#" || keyword == "+" || keyword == "%") continue;
                                if (keyword.StartsWith("!") && keyword.Length > 1) // exclude search
                                {
                                    score = GetOverallScore(asset, keyword.Substring(1), filter);
                                    if (score > 0)
                                    {
                                        matched = false;
                                        break;
                                    }
                                }
                                else if (keyword.StartsWith("#") && keyword.Length > 1) // search for custom tag only
                                {
                                    foreach (string tag in asset.tagsCustom)
                                    {
                                        score = GetScore(keyword.Substring(1), tag, tagsCustomDictionary);
                                    }
                                    if (score <= 0)
                                    {
                                        matched = false;
                                        break;
                                    }
                                }
                                else if (keyword.StartsWith("+") && keyword.Length > 1) // OR search
                                {
                                    orSearch = true;
                                    score = GetOverallScore(asset, keyword.Substring(1), filter);
                                    orScore += score;
                                    asset.score += score;
                                }
                                else if (keyword.StartsWith("%") && keyword.Length > 1) // search by workshop id
                                {
                                    if (asset.prefab.m_isCustomContent && asset.steamID != 0)
                                    {
                                        score = GetScore(keyword.Substring(1), asset.steamID.ToString(), null);
                                        if (score <= 0)
                                        {
                                            matched = false;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        matched = false;
                                        break;
                                    }
                                }

                                else
                                {
                                    // Calculate relevance score. Algorithm decided by Sam. Unchanged.
                                    score = GetOverallScore(asset, keyword, filter);
                                    if (score <= 0)
                                    {
                                        matched = false;
                                        break;
                                    }
                                    else asset.score += score;
                                }
                            }
                        }
                        if (orSearch && orScore <= 0) continue;
                        if (matched)
                        {
                            matches.Add(asset);
                            if (Settings.instanceCounterSort != 0) UpdateAssetInstanceCount(asset);
                        }
                    }
                }
            }

            // if there isn't anything in the search input box
            else
            {
                foreach (Asset asset in assets.Values)
                {
                    asset.RefreshRico();
                    if (asset.prefab != null)
                    {
                        if (!CheckAssetFilters(asset, filter)) continue;
                        matches.Add(asset);
                        if (Settings.instanceCounterSort != 0) UpdateAssetInstanceCount(asset);
                    }
                }
            }

            return matches;
        }

        private bool CheckAssetFilters(Asset asset, UISearchBox.DropDownOptions filter)
        {
            if (!CheckAssetTypeFilter(asset, filter)) return false;

            if (filter == UISearchBox.DropDownOptions.Growable || filter == UISearchBox.DropDownOptions.Rico || filter == UISearchBox.DropDownOptions.GrwbRico)
            {
                if (!CheckGrowableRICOFilter(asset, filter)) return false;
            }
            else if (filter == UISearchBox.DropDownOptions.Ploppable)
            {
                if (!CheckPloppableFilter(asset)) return false;
            }
            else if (filter == UISearchBox.DropDownOptions.Prop)
            {
                // filter by prop type
                if (!UIFilterProp.instance.IsAllSelected())
                {
                    UIFilterProp.Category category = UIFilterProp.GetCategory(asset.propType);
                    if (category == UIFilterProp.Category.None || !UIFilterProp.instance.IsSelected(category)) return false;
                }
            }
            else if (filter == UISearchBox.DropDownOptions.Tree)
            {
                // filter by tree type
                if (!UIFilterTree.instance.IsAllSelected())
                {
                    UIFilterTree.Category category = UIFilterTree.GetCategory(asset.treeType);
                    if (category == UIFilterTree.Category.None || !UIFilterTree.instance.IsSelected(category)) return false;
                }
            }
            else if (filter == UISearchBox.DropDownOptions.Network)
            {
                if (!CheckNetworkFilter(asset)) return false;
            }

            // filter out marker prop if not in editor mode
            if ((!FindIt.inEditor && !Settings.showPropMarker) && (asset.propType == Asset.PropType.PropsMarker)) return false;

            try
            {
                if (UISearchBox.instance?.workshopFilter != null && UISearchBox.instance?.vanillaFilter != null)
                {
                    // filter out custom asset
                    if (asset.prefab.m_isCustomContent && !UISearchBox.instance.workshopFilter.isChecked) return false;

                    // filter out vanilla asset. will not filter out content creater pack assets
                    if (!asset.prefab.m_isCustomContent && !UISearchBox.instance.vanillaFilter.isChecked && !asset.isCCP) return false;

                    // filter out assets without matching custom tag
                    if (UISearchBox.instance?.tagPanel != null)
                    {
                        if (UISearchBox.instance.tagPanel.tagDropDownCheckBox.isChecked && UISearchBox.instance.tagPanel.customTagListStrArray.Length > 0)
                        {
                            if (!asset.tagsCustom.Contains(UISearchBox.instance.tagPanel.GetDropDownListKey())) return false;
                        }
                    }

                    // extra filters check
                    if (UISearchBox.instance?.extraFiltersPanel != null)
                    {
                        if (UISearchBox.instance.extraFiltersPanel.optionDropDownCheckBox.isChecked)
                        {
                            if (!CheckExtraFilters(asset)) return false;
                        }
                        else
                        {
                            if (asset.isSubBuilding) return false;
                        }
                    }
                    // skip sub-buildings if not using the extra filters panel
                    else
                    {
                        if (asset.isSubBuilding) return false;
                    }
                }
            }
            catch (Exception e)
            {
                Debugging.LogException(e);
            }

            return true;
        }

        private bool CheckAssetTypeFilter(Asset asset, UISearchBox.DropDownOptions filter)
        {
            if (asset.assetType != Asset.AssetType.Network && filter == UISearchBox.DropDownOptions.Network) return false;
            if (asset.assetType != Asset.AssetType.Prop && filter == UISearchBox.DropDownOptions.Prop) return false;
            if (asset.assetType != Asset.AssetType.Rico && filter == UISearchBox.DropDownOptions.Rico) return false;
            if (asset.assetType != Asset.AssetType.Ploppable && filter == UISearchBox.DropDownOptions.Ploppable) return false;
            if (asset.assetType != Asset.AssetType.Growable && filter == UISearchBox.DropDownOptions.Growable) return false;
            if (asset.assetType != Asset.AssetType.Tree && filter == UISearchBox.DropDownOptions.Tree) return false;
            if (asset.assetType != Asset.AssetType.Decal && filter == UISearchBox.DropDownOptions.Decal) return false;
            if ((asset.assetType != Asset.AssetType.Rico && asset.assetType != Asset.AssetType.Growable) && filter == UISearchBox.DropDownOptions.GrwbRico) return false;
            return true;
        }

        private bool CheckGrowableRICOFilter(Asset asset, UISearchBox.DropDownOptions filter)
        {
            BuildingInfo buildingInfo = asset.prefab as BuildingInfo;
            // Distinguish growable and rico
            if ((filter == UISearchBox.DropDownOptions.Growable) && (asset.assetType == Asset.AssetType.Rico)) return false;
            if ((filter == UISearchBox.DropDownOptions.Rico) && (asset.assetType == Asset.AssetType.Growable)) return false;
            // filter by size
            if (!CheckBuildingSize(asset.size, UISearchBox.instance.buildingSizeFilterIndex)) return false;
            // filter by growable type
            if (!UIFilterGrowable.instance.IsAllSelected())
            {
                UIFilterGrowable.Category category = UIFilterGrowable.GetCategory(buildingInfo.m_class);
                if (category == UIFilterGrowable.Category.None || !UIFilterGrowable.instance.IsSelected(category)) return false;
            }
            return true;
        }

        private bool CheckPloppableFilter(Asset asset)
        {
            BuildingInfo buildingInfo = asset.prefab as BuildingInfo;
            // filter by size
            if (!CheckBuildingSize(asset.size, UISearchBox.instance.buildingSizeFilterIndex)) return false;
            // filter by ploppable type
            if (!UIFilterPloppable.instance.IsAllSelected())
            {
                UIFilterPloppable.Category category = UIFilterPloppable.GetCategory(buildingInfo.m_class);
                if (category == UIFilterPloppable.Category.None || !UIFilterPloppable.instance.IsSelected(category)) return false;
            }
            return true;
        }

        private bool CheckNetworkFilter(Asset asset)
        {
            // filter by network type
            if (!UIFilterNetwork.instance.IsAllSelected())
            {
                UIFilterNetwork.Category category = UIFilterNetwork.GetCategory(asset.networkType);
                NetInfo info = asset.prefab as NetInfo;
                if (info == null) return false;
                // not mutually exclusive with other categories. Handle them differently.
                if (UIFilterNetwork.instance.IsOnlySelected(UIFilterNetwork.Category.OneWay))
                {
                    if (!UIFilterNetwork.IsNormalRoads(asset.networkType)) return false;
                    if (!UIFilterNetwork.IsOneWay(info)) return false;
                }
                else if (UIFilterNetwork.instance.IsOnlySelected(UIFilterNetwork.Category.Parking))
                {
                    if (!UIFilterNetwork.IsNormalRoads(asset.networkType)) return false;
                    if (!UIFilterNetwork.HasParking(info)) return false;
                }
                else if (UIFilterNetwork.instance.IsOnlySelected(UIFilterNetwork.Category.NoParking))
                {
                    if (!UIFilterNetwork.IsNormalRoads(asset.networkType)) return false;
                    if (UIFilterNetwork.HasParking(info)) return false;
                }
                else
                {
                    if (category == UIFilterNetwork.Category.None || !UIFilterNetwork.instance.IsSelected(category)) return false;
                }
            }
            return true;
        }

        // return true if the asset size matches the building size filter options
        // index 0 = all
        // index 1 - 4 = corresponding sizes
        // index 5 = size 5 - 8
        // index 6 = size 9 - 12
        // index 7 = size 13+
        private bool CheckBuildingSize(Vector2 assetSize, Vector2 buildingSizeFilterIndex)
        {
            if (buildingSizeFilterIndex.x > 0.0f && buildingSizeFilterIndex.y > 0.0f)
            {
                if (!CheckBuildingSizeXY(assetSize.x, buildingSizeFilterIndex.x) || !CheckBuildingSizeXY(assetSize.y, buildingSizeFilterIndex.y)) return false;
            }
            // if filter = (All, not All)
            if (buildingSizeFilterIndex.x == 0.0f && buildingSizeFilterIndex.y != 0.0f)
            {
                if (!CheckBuildingSizeXY(assetSize.y, buildingSizeFilterIndex.y)) return false;
            }
            // if filter = (not All, All)
            if (buildingSizeFilterIndex.x != 0.0f && buildingSizeFilterIndex.y == 0.0f)
            {
                if (!CheckBuildingSizeXY(assetSize.x, buildingSizeFilterIndex.x)) return false;
            }
            return true;
        }

        public bool CheckBuildingSizeXY(float assetSizeXY, float buildingSizeFilterIndex)
        {
            if (buildingSizeFilterIndex == 0.0f) return true; // all
            if (buildingSizeFilterIndex < 5.0f) // size of 1 - 4
            {
                if (assetSizeXY == buildingSizeFilterIndex) return true;
                else return false;
            }
            if (buildingSizeFilterIndex == 5.0f) // size 5 - 8
            {
                if (assetSizeXY <= 8.0f && assetSizeXY >= 5.0f) return true;
                else return false;
            }
            if (buildingSizeFilterIndex == 6.0f) // size 9 - 12
            {
                if (assetSizeXY <= 12.0f && assetSizeXY >= 9.0f) return true;
                else return false;
            }
            if (buildingSizeFilterIndex == 7.0f) // size 13+
            {
                if (assetSizeXY >= 13.0f) return true;
                else return false;
            }
            return true;
        }

        private bool CheckExtraFilters(Asset asset)
        {
            // filter out sub-builsings if sub-building filter not enabled
            if (asset.isSubBuilding && UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex != (int)UIFilterExtra.DropDownOptions.SubBuildings) return false;
            // filter asset by asset creator
            if (UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex == (int)UIFilterExtra.DropDownOptions.AssetCreator)
            {
                if (asset.author != UISearchBox.instance.extraFiltersPanel.GetAssetCreatorDropDownListKey()) return false;
            }
            // filter asset by building height
            else if (UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex == (int)UIFilterExtra.DropDownOptions.BuildingHeight)
            {
                if (asset.assetType == Asset.AssetType.Ploppable || asset.assetType == Asset.AssetType.Rico || asset.assetType == Asset.AssetType.Growable)
                {
                    if (asset.buildingHeight > UISearchBox.instance.extraFiltersPanel.maxBuildingHeight) return false;
                    if (asset.buildingHeight < UISearchBox.instance.extraFiltersPanel.minBuildingHeight) return false;
                }
                else return false;
            }
            // filter asset by building level
            else if (UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex == (int)UIFilterExtra.DropDownOptions.BuildingLevel)
            {
                if (!(asset.prefab is BuildingInfo)) return false;
                BuildingInfo info = asset.prefab as BuildingInfo;
                ItemClass.Level level = (ItemClass.Level)UISearchBox.instance.extraFiltersPanel.buildingLevelDropDownMenu.selectedIndex;
                if (info.m_class.m_level != level) return false;
            }
            // only show sub-buildings
            else if (UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex == (int)UIFilterExtra.DropDownOptions.SubBuildings)
            {
                if (!asset.isSubBuilding) return false;
                if (asset.assetType != Asset.AssetType.Invalid) return false;
            }
            // only show unused assets
            else if (UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex == (int)UIFilterExtra.DropDownOptions.UnusedAssets)
            {
                if (prefabInstanceCountDictionary.ContainsKey(asset.prefab))
                {
                    if (prefabInstanceCountDictionary[asset.prefab] > 0) return false;
                }
                if (FindIt.isPOEnabled && Settings.includePOinstances)
                {
                    if (ProceduralObjectsTool.GetPrefabPOInstanceCount(asset.prefab) > 0) return false;
                }
            }
            // only show used assets
            else if (UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex == (int)UIFilterExtra.DropDownOptions.UsedAssets)
            {
                uint counter = 0;
                if (prefabInstanceCountDictionary.ContainsKey(asset.prefab))
                {
                    counter += prefabInstanceCountDictionary[asset.prefab];
                }
                if (FindIt.isPOEnabled && Settings.includePOinstances)
                {
                    counter += ProceduralObjectsTool.GetPrefabPOInstanceCount(asset.prefab);
                }
                if (counter < 1) return false;
            }
            // only show assets with custom tags
            else if (UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex == (int)UIFilterExtra.DropDownOptions.WithCustomTag)
            {
                if (asset.tagsCustom.Count < 1) return false;
            }
            // only show assets without custom tags
            else if (UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex == (int)UIFilterExtra.DropDownOptions.WithoutCustomTag)
            {
                if (asset.tagsCustom.Count > 0) return false;
            }

            // DLC & CCP filter
            else if (UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex == (int)UIFilterExtra.DropDownOptions.DLC)
            {
                if (!CheckDLCFilters(asset.prefab.m_dlcRequired)) return false;
            }

            // local custom filter
            else if (UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex == (int)UIFilterExtra.DropDownOptions.LocalCustom)
            {
                if (!asset.prefab.m_isCustomContent) return false;
                if (!localWorkshopIDs.Contains(asset.steamID) && asset.steamID != 0) return false;
            }

            // workshop subscription assets
            else if (UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex == (int)UIFilterExtra.DropDownOptions.WorkshopCustom)
            {
                if (!asset.prefab.m_isCustomContent) return false;
                if (localWorkshopIDs.Contains(asset.steamID)) return false;
                if (asset.steamID == 0) return false;
            }

            // Terrain conforming
            else if (UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex == (int)UIFilterExtra.DropDownOptions.TerrainConforming)
            {
                if (!CheckTerrainConforming(asset, true)) return false;
            }

            // Non-Terrain conforming
            else if (UISearchBox.instance.extraFiltersPanel.optionDropDownMenu.selectedIndex == (int)UIFilterExtra.DropDownOptions.NonTerrainConforming)
            {
                if (!CheckTerrainConforming(asset, false)) return false;
            }

            return true;
        }

        private bool CheckTerrainConforming(Asset asset, bool checkTCFlag)
        {

            if (asset.assetType == Asset.AssetType.Decal || asset.assetType == Asset.AssetType.Tree) return checkTCFlag;

            else if (asset.assetType == Asset.AssetType.Prop)
            {
                PropInfo propInfo = asset.prefab as PropInfo;

                if (checkTCFlag) // check terrain conforming
                {
                    if (propInfo.m_material?.shader != shaderPropFence) return false;
                }
                else // check non-terrain conforming
                {
                    if (propInfo.m_material?.shader == shaderPropFence) return false;
                }
            }

            else if ((asset.assetType == Asset.AssetType.Ploppable) || (asset.assetType == Asset.AssetType.Growable) || (asset.assetType == Asset.AssetType.Rico))
            {
                BuildingInfo buildingInfo = asset.prefab as BuildingInfo;
                if (checkTCFlag)
                {
                    if (buildingInfo.m_material?.shader != shaderBuildingFence) return false;
                }
                else
                {
                    if (buildingInfo.m_material?.shader == shaderBuildingFence) return false;
                }
            }

            else if (asset.assetType == Asset.AssetType.Network)
            {
                NetInfo netInfo = asset.prefab as NetInfo;
                bool noFenceShader = true;
                foreach (NetInfo.Segment segment in netInfo.m_segments)
                {
                    if (checkTCFlag) // check terrain conforming
                    {
                        // if all segments are not using the fence shader, it is non-terrain conforming
                        if (segment.m_material?.shader == shaderNetworkFence) noFenceShader = false;
                    }
                    else // check non-terrain conforming
                    {
                        // if any segment is using the fence shader, it is terrain conforming
                        if (segment.m_material?.shader == shaderNetworkFence) return false;
                    }
                }
                if (checkTCFlag && noFenceShader) return false;
            }
            return true;
        }
        private bool CheckDLCFilters(SteamHelper.DLC_BitMask dlc)
        {
            if (dlc != SteamHelper.DLC_BitMask.None && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.BaseGame) return false;

            else if (dlc != SteamHelper.DLC_BitMask.DeluxeDLC && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.DeluxeUpgrade) return false;

            else if (dlc != SteamHelper.DLC_BitMask.AfterDarkDLC && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.AfterDark) return false;

            else if (dlc != SteamHelper.DLC_BitMask.SnowFallDLC && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.SnowFall) return false;

            else if (dlc != SteamHelper.DLC_BitMask.NaturalDisastersDLC && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.NaturalDisasters) return false;

            else if (dlc != SteamHelper.DLC_BitMask.InMotionDLC && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.MassTransit) return false;

            else if (dlc != SteamHelper.DLC_BitMask.GreenCitiesDLC && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.GreenCities) return false;

            else if (dlc != SteamHelper.DLC_BitMask.ParksDLC && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.Parklife) return false;

            else if (dlc != SteamHelper.DLC_BitMask.IndustryDLC && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.Industries) return false;

            else if (dlc != SteamHelper.DLC_BitMask.CampusDLC && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.Campus) return false;

            else if (dlc != SteamHelper.DLC_BitMask.UrbanDLC && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.SunsetHarbor) return false;

            else if (dlc != SteamHelper.DLC_BitMask.Football && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.MatchDay) return false;

            else if (dlc != SteamHelper.DLC_BitMask.Football2345 && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.Stadiums) return false;

            else if (dlc != SteamHelper.DLC_BitMask.OrientalBuildings && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.PearlsFromTheEast) return false;

            else if (dlc != SteamHelper.DLC_BitMask.MusicFestival && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.Concerts) return false;

            else if (dlc != SteamHelper.DLC_BitMask.ModderPack1 && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.ArtDeco) return false;

            else if (dlc != SteamHelper.DLC_BitMask.ModderPack2 && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.HighTechBuildings) return false;

            else if (dlc != SteamHelper.DLC_BitMask.ModderPack3 && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.EuropeanSuburbias) return false;

            else if (dlc != SteamHelper.DLC_BitMask.ModderPack4 && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.UniverisityCity) return false;

            else if (dlc != SteamHelper.DLC_BitMask.ModderPack5 && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.ModernCityCenter) return false;

            else if (dlc != SteamHelper.DLC_BitMask.ModderPack6 && UISearchBox.instance.extraFiltersPanel.DLCDropDownMenu.selectedIndex
                == (int)UIFilterExtra.DLCDropDownOptions.ModernJapan) return false;

            return true;
        }

        /// <summary>
        /// Get relevance score. Metrics decided by SamsamTS. Unchanged in Find It 2
        /// </summary>
        private float GetOverallScore(Asset asset, string keyword, UISearchBox.DropDownOptions filter)
        {
            float score = 0;

            if (!asset.author.IsNullOrWhiteSpace())
            {
                score += 10 * GetScore(keyword, asset.author.ToLower(), null);
            }

            if (filter == UISearchBox.DropDownOptions.All && asset.assetType != Asset.AssetType.Invalid)
            {
                score += 10 * GetScore(keyword, asset.assetType.ToString().ToLower(), null);
            }

            if (asset.service != ItemClass.Service.None)
            {
                score += 10 * GetScore(keyword, asset.service.ToString().ToLower(), null);
            }

            if (asset.subService != ItemClass.SubService.None)
            {
                score += 10 * GetScore(keyword, asset.subService.ToString().ToLower(), null);
            }

            if (asset.size != Vector2.zero)
            {
                score += 10 * GetScore(keyword, asset.size.x + "x" + asset.size.y, null);
            }

            foreach (string tag in asset.tagsCustom)
            {
                score += 20 * GetScore(keyword, tag, tagsCustomDictionary);
            }

            foreach (string tag in asset.tagsTitle)
            {
                score += 5 * GetScore(keyword, tag, tagsTitleDictionary);
            }

            foreach (string tag in asset.tagsDesc)
            {
                score += GetScore(keyword, tag, tagsDescDictionary);
            }

            return score;
        }
        private float GetScore(string keyword, string tag, Dictionary<string, int> dico)
        {
            int index = tag.IndexOf(keyword);
            float scoreMultiplier = 1f;

            if (index >= 0)
            {
                if (index == 0)
                {
                    scoreMultiplier = 10f;
                }
                if (dico != null && dico.ContainsKey(tag))
                {
                    return scoreMultiplier / dico[tag] * ((tag.Length - index) / (float)tag.Length) * (keyword.Length / (float)tag.Length);
                }
                else
                {
                    if (dico != null)
                    {
                        // Debugging.Message("Tag not found in dico: " + tag);
                    }
                    return scoreMultiplier * ((tag.Length - index) / (float)tag.Length) * (keyword.Length / (float)tag.Length);
                }
            }

            return 0;
        }
    }
}
