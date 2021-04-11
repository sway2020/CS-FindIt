using System.Xml.Serialization;
using UnityEngine;

namespace FindIt
{
    /// <summary>
    /// Class to hold global mod settings.
    /// </summary>
    [XmlRoot(ElementName = "FindIt2", Namespace = "", IsNullable = false)]
    internal static class Settings
    {
        internal static bool hideDebugMessages = true;

        internal static bool unlockAll = false;

        internal static bool centerToolbar = true;

        internal static bool fixBadProps = false;

        internal static bool useDefaultBrowser = false;

        // false = sort by number of assets
        // true = sort alphabetically
        internal static bool customTagListSort = false;
        internal static bool assetCreatorListSort = false;

        internal static bool showPropMarker = false;

        internal static bool showInstancesCounter = false;

        internal static int instanceCounterSort = 0;

        internal static bool includePOinstances = false;

        internal static bool useLightBackground = false;

        internal static bool useMoveItForProps = false;

        internal static bool disableUpdateNotice = false;

        internal static double lastUpdateNotice = 0.0;

        internal static bool separateSearchKeyword = false;

        internal static bool showAssetTypePanel = true;

        internal static float assetTypePanelX = -80.0f;

        internal static float assetTypePanelY = -75.0f;

        internal static KeyBinding searchKey = new KeyBinding { keyCode = (int)KeyCode.F, control = true, shift = false, alt = false };

        internal static KeyBinding allKey = new KeyBinding { keyCode = (int)KeyCode.Alpha1, control = false, shift = false, alt = true };

        internal static KeyBinding networkKey = new KeyBinding { keyCode = (int)KeyCode.Alpha2, control = false, shift = false, alt = true };

        internal static KeyBinding ploppableKey = new KeyBinding { keyCode = (int)KeyCode.Alpha3, control = false, shift = false, alt = true };

        internal static KeyBinding growableKey = new KeyBinding { keyCode = (int)KeyCode.Alpha4, control = false, shift = false, alt = true };

        internal static KeyBinding ricoKey = new KeyBinding { keyCode = (int)KeyCode.Alpha5, control = false, shift = false, alt = true };

        internal static KeyBinding grwbRicoKey = new KeyBinding { keyCode = (int)KeyCode.Alpha6, control = false, shift = false, alt = true };

        internal static KeyBinding propKey = new KeyBinding { keyCode = (int)KeyCode.Alpha7, control = false, shift = false, alt = true };

        internal static KeyBinding decalKey = new KeyBinding { keyCode = (int)KeyCode.Alpha8, control = false, shift = false, alt = true };

        internal static KeyBinding treeKey = new KeyBinding { keyCode = (int)KeyCode.Alpha9, control = false, shift = false, alt = true };

        internal static KeyBinding randomSelectionKey = new KeyBinding { keyCode = (int)KeyCode.V, control = false, shift = false, alt = true };

    }

    /// <summary>
    /// Defines the XML settings file.
    /// </summary>
    [XmlRoot(ElementName = "FindIt", Namespace = "", IsNullable = false)]
    public class XMLSettingsFile
    {
        [XmlElement("HideDebugMessages")]
        public bool HideDebugMessages { get => Settings.hideDebugMessages; set => Settings.hideDebugMessages = value; }

        [XmlElement("UnlockAll")]
        public bool UnlockAll { get => Settings.unlockAll; set => Settings.unlockAll = value; }

        [XmlElement("CenterToolbar")]
        public bool CenterToolbar { get => Settings.centerToolbar; set => Settings.centerToolbar = value; }

        [XmlElement("FixBadProps")]
        public bool FixBadProps { get => Settings.fixBadProps; set => Settings.fixBadProps = value; }

        [XmlElement("UseDefaultBrowser")]
        public bool UseDefaultBrowser { get => Settings.useDefaultBrowser; set => Settings.useDefaultBrowser = value; }

        [XmlElement("CustomTagListSort")]
        public bool CustomTagListSort { get => Settings.customTagListSort; set => Settings.customTagListSort = value; }

        [XmlElement("AssetCreatorListSort")]
        public bool AssetCreatorListSort { get => Settings.assetCreatorListSort; set => Settings.assetCreatorListSort = value; }

        [XmlElement("ShowPropMarker")]
        public bool ShowPropMarker { get => Settings.showPropMarker; set => Settings.showPropMarker = value; }

        [XmlElement("ShowInstancesCounter")]
        public bool ShowInstancesCounter { get => Settings.showInstancesCounter; set => Settings.showInstancesCounter = value; }

        [XmlElement("InstanceCounterSort")]
        public int InstanceCounterSort { get => Settings.instanceCounterSort; set => Settings.instanceCounterSort = value; }

        [XmlElement("IncludePOinstances")]
        public bool IncludePOinstances { get => Settings.includePOinstances; set => Settings.includePOinstances = value; }

        [XmlElement("UseLightBackground")]
        public bool UseLightBackground { get => Settings.useLightBackground; set => Settings.useLightBackground = value; }

        [XmlElement("UseMoveItForProps")]
        public bool UseMoveItForProps { get => Settings.useMoveItForProps; set => Settings.useMoveItForProps = value; }

        [XmlElement("DisableUpdateNotice")]
        public bool DisableUpdateNotice { get => Settings.disableUpdateNotice; set => Settings.disableUpdateNotice = value; }

        [XmlElement("SeparateSearchKeyword")]
        public bool SeparateSearchKeyword { get => Settings.separateSearchKeyword; set => Settings.separateSearchKeyword = value; }

        [XmlElement("LastUpdateNotice")]
        public double LastUpdateNotice { get => Settings.lastUpdateNotice; set => Settings.lastUpdateNotice = value; }

        [XmlElement("ShowAssetTypePanel")]
        public bool ShowAssetTypePanel { get => Settings.showAssetTypePanel; set => Settings.showAssetTypePanel = value; }

        [XmlElement("AssetTypePanelX")]
        public float AssetTypePanelX { get => Settings.assetTypePanelX; set => Settings.assetTypePanelX = value; }

        [XmlElement("AssetTypePanelY")]
        public float AssetTypePanelY { get => Settings.assetTypePanelY; set => Settings.assetTypePanelY = value; }

        [XmlElement("SearchKey")]
        public KeyBinding SearchKey { get => Settings.searchKey; set => Settings.searchKey = value; }

        [XmlElement("AllKey")]
        public KeyBinding AllKey { get => Settings.allKey; set => Settings.allKey = value; }

        [XmlElement("NetworkKey")]
        public KeyBinding NetworkKey { get => Settings.networkKey; set => Settings.networkKey = value; }

        [XmlElement("PloppableKey")]
        public KeyBinding PloppableKey { get => Settings.ploppableKey; set => Settings.ploppableKey = value; }

        [XmlElement("GrowableKey")]
        public KeyBinding GrowableKey { get => Settings.growableKey; set => Settings.growableKey = value; }

        [XmlElement("RicoKey")]
        public KeyBinding RicoKey { get => Settings.ricoKey; set => Settings.ricoKey = value; }

        [XmlElement("GrwbRicoKey")]
        public KeyBinding GrwbRicoKey { get => Settings.grwbRicoKey; set => Settings.grwbRicoKey = value; }

        [XmlElement("PropKey")]
        public KeyBinding PropKey { get => Settings.propKey; set => Settings.propKey = value; }

        [XmlElement("DecalKey")]
        public KeyBinding DecalKey { get => Settings.decalKey; set => Settings.decalKey = value; }

        [XmlElement("TreeKey")]
        public KeyBinding TreeKey { get => Settings.treeKey; set => Settings.treeKey = value; }

        [XmlElement("RandomSelectionKey")]
        public KeyBinding RandomSelectionKey { get => Settings.randomSelectionKey; set => Settings.randomSelectionKey = value; }

        [XmlElement("Language")]
        public string Language
        {
            get
            {
                return Translations.Language;
            }
            set
            {
                Translations.Language = value;
            }
        }
    }


    /// <summary>
    /// Basic keybinding class - code and modifiers.
    /// </summary>
    public struct KeyBinding
    {
        [XmlAttribute("KeyCode")]
        public int keyCode;

        [XmlAttribute("Control")]
        public bool control;

        [XmlAttribute("Shift")]
        public bool shift;

        [XmlAttribute("Alt")]
        public bool alt;
    }
}