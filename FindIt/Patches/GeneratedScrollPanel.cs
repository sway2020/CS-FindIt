using System;
using System.Reflection;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.PlatformServices;
using HarmonyLib;
using FindIt.GUI;

namespace FindIt
{
    // This patch adds some of Find It's own UI stuff (like the custom tag and steam sprites) to the game's default panels
    [HarmonyPatch(typeof(GeneratedScrollPanel))]
    [HarmonyPatch("CreateButton")]
    [HarmonyPatch(new Type[] { typeof(string), typeof(string), typeof(string), typeof(int), typeof(UITextureAtlas), typeof(UIComponent), typeof(bool) })]
    internal static class CreateButtonPatch
    {
        private static void Postfix(UIButton __result, GeneratedScrollPanel __instance, string name, string tooltip, string baseIconName, int index, UITextureAtlas atlas, UIComponent tooltipBox, bool enabled)
        {
            __result.eventVisibilityChanged += new PropertyChangedEventHandler<bool>(Init);

            GeneratedScrollPanel panel = __instance;
            SimulationManager.instance.AddAction(() =>
            {
                if (__result.objectUserData is PrefabInfo prefab)
                {
                    string key = Asset.GetName(prefab);

                    if (AssetTagList.instance.assets.ContainsKey(key))
                    {
                        if (AssetTagList.instance.assets[key].onButtonClicked == null)
                        {
                            MethodInfo onButtonClicked = panel.GetType().GetMethod("OnButtonClicked", BindingFlags.NonPublic | BindingFlags.Instance);
                            AssetTagList.instance.assets[key].onButtonClicked = Delegate.CreateDelegate(typeof(Asset.OnButtonClicked), panel, onButtonClicked, false) as Asset.OnButtonClicked;
                        }
                    }
                }
            });
        }


        private static void Init(UIComponent component, bool b)
        {
            component.eventVisibilityChanged -= new PropertyChangedEventHandler<bool>(Init);

            try
            {
                if (component.objectUserData is PrefabInfo prefab)
                {
                    string name = Asset.GetName(prefab);

                    if (AssetTagList.instance.assets.ContainsKey(name))
                    {
                        ImageUtils.FixThumbnails(prefab, component as UIButton);

                        Asset asset = AssetTagList.instance.assets[name];

                        component.eventVisibilityChanged += (c, p) =>
                        {
                            if (Settings.unlockAll)
                            {
                                c.isEnabled = true;
                            }
                            else
                            {
                                c.isEnabled = ToolsModifierControl.IsUnlocked(prefab.GetUnlockMilestone());
                            }
                        };

                        // Fixing focused texture
                        component.eventClicked += new MouseEventHandler(FixFocusedTexture);

                        // Adding custom tag icon
                        UISprite tagSprite = component.AddUIComponent<UISprite>();
                        tagSprite.size = new Vector2(20, 16);
                        tagSprite.atlas = FindIt.atlas;
                        tagSprite.spriteName = "Tag";
                        tagSprite.opacity = 0.5f;
                        tagSprite.tooltipBox = UIView.GetAView().defaultTooltipBox;
                        tagSprite.relativePosition = new Vector3(component.width - tagSprite.width - 5, 5);
                        tagSprite.isVisible = false;

                        if (CustomTagsLibrary.assetTags.ContainsKey(name))
                        {
                            tagSprite.tooltip = CustomTagsLibrary.assetTags[name];
                        }
                        else
                        {
                            tagSprite.tooltip = null;
                        }

                        tagSprite.eventMouseEnter += (c, p) =>
                        {
                            tagSprite.opacity = 1f;
                        };

                        tagSprite.eventMouseLeave += (c, p) =>
                        {
                            tagSprite.opacity = 0.5f;
                        };

                        tagSprite.eventClick += (c, p) =>
                        {
                            p.Use();

                            UITagsWindow.ShowAt(asset, tagSprite);
                        };

                        component.eventMouseEnter += (c, p) =>
                        {
                            tagSprite.isVisible = true;
                        };

                        component.eventMouseLeave += (c, p) =>
                        {
                            if (asset.tagsCustom.Count == 0)
                            {
                                tagSprite.isVisible = false;
                            }
                        };

                        // Adding steam icon
                        if (asset.steamID != 0)
                        {
                            UISprite steamSprite = component.AddUIComponent<UISprite>();
                            steamSprite.size = new Vector2(26, 16);
                            steamSprite.atlas = SamsamTS.UIUtils.GetAtlas("Ingame");
                            steamSprite.spriteName = "SteamWorkshop";
                            steamSprite.opacity = 0.05f;
                            steamSprite.tooltipBox = UIView.GetAView().defaultTooltipBox;
                            steamSprite.relativePosition = new Vector3(component.width - steamSprite.width - 5, component.height - steamSprite.height - 5);
                            steamSprite.eventMouseLeave += (c, p) =>
                            {
                                steamSprite.tooltipBox.Hide();
                            };

                            if (!asset.author.IsNullOrWhiteSpace())
                            {
                                steamSprite.tooltip = "By " + asset.author + "\n" + Translations.Translate("FIF_UIS_WS");
                            }

                            if (PlatformService.IsOverlayEnabled())
                            {
                                steamSprite.eventMouseUp += (c, p) =>
                                {
                                    if (!p.used && p.buttons == UIMouseButton.Right)
                                    {
                                        PublishedFileId publishedFileId = new PublishedFileId(asset.steamID);

                                        if (publishedFileId != PublishedFileId.invalid)
                                        {
                                            if (!Settings.useDefaultBrowser)
                                            {
                                                PlatformService.ActivateGameOverlayToWorkshopItem(publishedFileId);
                                            }
                                            else
                                            {
                                                System.Diagnostics.Process.Start("https://steamcommunity.com/sharedfiles/filedetails/?id=" + publishedFileId);
                                            }
                                            p.Use();
                                        }
                                    }
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debugging.LogException(e);
            }
        }

        private static void FixFocusedTexture(UIComponent component, UIMouseEventParameter p)
        {
            component.eventClicked -= new MouseEventHandler(FixFocusedTexture);

            try
            {
                if (component.objectUserData is PrefabInfo prefab)
                {
                    if (ImageUtils.FixFocusedTexture(prefab))
                    {
                        // Debugging.Message("Fixed focused texture: " + prefab.name);
                    }
                    UIScrollPanelItem.fixedFocusedTexture.Add(prefab);
                }
            }
            catch (Exception e)
            {
                Debugging.LogException(e);
            }
        }
    }
}
