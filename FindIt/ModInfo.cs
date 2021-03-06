﻿// modified from SamsamTS's original Find It mod
// https://github.com/SamsamTS/CS-FindIt

using ICities;
using System;
using ColossalFramework.UI;
using CitiesHarmony.API;
using System.IO;
using ColossalFramework.IO;
using UnityEngine;

namespace FindIt
{
    public class ModInfo : IUserMod
    {
        public const string version = "2.6.2";
        public const bool isBeta = false;
        public const double updateNoticeDate = 20210702;
        public const string updateNotice =

            "- Marker type props are no longer hidden by default in Game mode\n\n" +

            "- Recent DLs sorting now also works with local custom assets\n\n" +

            "- Add a shortcut button to open the parent folder of an asset\n\n" +

            "- Custom non-workshop assets have a new icon on their thumbnails\n\n" +

            "- Add shortcut buttons to see assets in following mods:\n" +
            "       Ploppable RICO Revisited\n" +
            "       Mesh Info\n\n" +

            "Double-click asset thumbnails to see the shortcut menu\n";

        public string Name
        {
            get { return "Find It! " + (isBeta ? "[BETA] " : "") + version; }
        }

        public string Description
        {
            get { return Translations.Translate("FIF_DESC"); }
        }

        /// <summary>
        /// Called by the game when mod is enabled.
        /// </summary>
        public void OnEnabled()
        {
            // Apply Harmony patches via Cities Harmony.
            // Called here instead of OnCreated to allow the auto-downloader to do its work prior to launch.
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
            Debugging.Message("Harmony patches applied");
            // Load settings here.
            XMLUtils.LoadSettings();
            Debugging.Message("XML Settings loaded");
        }

        /// <summary>
        /// Called by the game when the mod is disabled.
        /// </summary>
        public void OnDisabled()
        {
            // Unapply Harmony patches via Cities Harmony.
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                Patcher.UnpatchAll();
            }
        }

        /// <summary>
        /// Called by the game when the mod options panel is setup.
        /// </summary>
        public void OnSettingsUI(UIHelperBase helper)
        {
            try
            {
                if (FindIt.instance == null)
                {
                    AssetTagList.instance = new AssetTagList();
                }

                UIHelper group = helper.AddGroup(Name) as UIHelper;
                UIPanel panel = group.self as UIPanel;

                // Disable debug messages logging
                UICheckBox hideDebugMessages = (UICheckBox)group.AddCheckbox(Translations.Translate("FIF_SET_DM"), Settings.hideDebugMessages, (b) =>
               {
                   Settings.hideDebugMessages = b;
                   XMLUtils.SaveSettings();
               });
                hideDebugMessages.tooltip = Translations.Translate("FIF_SET_DMTP");
                group.AddSpace(10);

                // Center the main toolbar
                UICheckBox centerToolbar = (UICheckBox)group.AddCheckbox(Translations.Translate("FIF_SET_CMT"), Settings.centerToolbar, (b) =>
                {
                    Settings.centerToolbar = b;
                    XMLUtils.SaveSettings();

                    if (FindIt.instance != null)
                    {
                        FindIt.instance.UpdateMainToolbar();
                    }
                });
                centerToolbar.tooltip = Translations.Translate("FIF_SET_CMTTP");
                group.AddSpace(10);

                // Unlock all
                UICheckBox unlockAll = (UICheckBox)group.AddCheckbox(Translations.Translate("FIF_SET_UL"), Settings.unlockAll, (b) =>
                {
                    Settings.unlockAll = b;
                    XMLUtils.SaveSettings();
                });
                unlockAll.tooltip = Translations.Translate("FIF_SET_ULTP");
                group.AddSpace(10);

                /*
                // Fix bad props next loaded save
                // Implemented by samsamTS. Only needed for pre-2018 savefiles 
                UICheckBox fixProps = (UICheckBox)group.AddCheckbox(Translations.Translate("FIF_SET_BP"), false, (b) =>
                {
                    Settings.fixBadProps = b;
                    XMLUtils.SaveSettings();
                });
                fixProps.tooltip = Translations.Translate("FIF_SET_BPTP");
                group.AddSpace(10);
                */

                // Use system default browser instead of steam overlay
                UICheckBox useDefaultBrowser = (UICheckBox)group.AddCheckbox(Translations.Translate("FIF_SET_DB"), Settings.useDefaultBrowser, (b) =>
                {
                    Settings.useDefaultBrowser = b;
                    XMLUtils.SaveSettings();
                });
                useDefaultBrowser.tooltip = Translations.Translate("FIF_SET_DBTP");
                group.AddSpace(10);

                // Do not show extra Find It 2 UI on vanilla panels
                UICheckBox hideExtraUIonVP = (UICheckBox)group.AddCheckbox(Translations.Translate("FIF_SET_UIVP"), Settings.hideExtraUIonVP, (b) =>
                {
                    Settings.hideExtraUIonVP = b;
                    XMLUtils.SaveSettings();
                });
                hideExtraUIonVP.tooltip = Translations.Translate("FIF_SET_UIVPTP");
                group.AddSpace(10);

                // Disable update notice
                UICheckBox disableUpdateNotice = (UICheckBox)group.AddCheckbox(Translations.Translate("FIF_SET_DUN"), Settings.disableUpdateNotice, (b) =>
                {
                    Settings.disableUpdateNotice = b;
                    XMLUtils.SaveSettings();
                });
                disableUpdateNotice.tooltip = Translations.Translate("FIF_SET_DBTP");
                group.AddSpace(10);

                // Disable update notice
                UICheckBox separateSearchKeyword = (UICheckBox)group.AddCheckbox(Translations.Translate("FIF_SET_SSK"), Settings.separateSearchKeyword, (b) =>
                {
                    Settings.separateSearchKeyword = b;
                    XMLUtils.SaveSettings();
                });
                separateSearchKeyword.tooltip = Translations.Translate("FIF_SET_SSKTP");
                group.AddSpace(10);

                // languate settings
                UIDropDown languageDropDown = (UIDropDown)group.AddDropdown(Translations.Translate("TRN_CHOICE"), Translations.LanguageList, Translations.Index, (value) =>
                {
                    Translations.Index = value;
                    XMLUtils.SaveSettings();
                });

                languageDropDown.width = 300;
                group.AddSpace(10);

                // show path to FindItCustomTags.xml
                string path = Path.Combine(DataLocation.localApplicationData, "FindItCustomTags.xml");
                UITextField customTagsFilePath = (UITextField)group.AddTextfield(Translations.Translate("FIF_SET_CTFL"), path, _ => { }, _ => { });
                customTagsFilePath.width = panel.width - 30;
                group.AddButton(Translations.Translate("FIF_SET_CTFOP"), () => UnityEngine.Application.OpenURL(DataLocation.localApplicationData));

                // shortcut keys
                panel.gameObject.AddComponent<MainButtonKeyMapping>();
                panel.gameObject.AddComponent<AllKeyMapping>();
                panel.gameObject.AddComponent<NetworkKeyMapping>();
                panel.gameObject.AddComponent<PloppableKeyMapping>();
                panel.gameObject.AddComponent<GrowableKeyMapping>();
                panel.gameObject.AddComponent<RicoKeyMapping>();
                panel.gameObject.AddComponent<GrwbRicoKeyMapping>();
                panel.gameObject.AddComponent<PropKeyMapping>();
                panel.gameObject.AddComponent<DecalKeyMapping>();
                panel.gameObject.AddComponent<TreeKeyMapping>();
                panel.gameObject.AddComponent<RandomSelectionKeyMapping>();
                group.AddSpace(10);

            }
            catch (Exception e)
            {
                Debugging.Message("OnSettingsUI failed");
                Debugging.LogException(e);
            }
        }
    }
}
