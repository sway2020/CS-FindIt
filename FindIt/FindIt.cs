// modified from SamsamTS's original Find It mod
// https://github.com/SamsamTS/CS-FindIt

using ICities;
using UnityEngine;
using System;
using System.Reflection;
using ColossalFramework.UI;
using ColossalFramework.Plugins;
using ColossalFramework.Globalization;
using FindIt.GUI;

namespace FindIt
{
    public class FindIt : MonoBehaviour
    {
        public const string settingsFileName = "FindIt";

        public static FindIt instance;
        public static UITextureAtlas atlas = LoadResources();
        public static bool inEditor = false;
        /// <summary>
        /// Ploppable RICO (Revisited) mod enabled?
        /// </summary>
        public static bool isRicoEnabled = false;
        /// <summary>
        /// Procedural Object mod enabled?
        /// </summary>
        public static bool isPOEnabled = false;
        /// <summary>
        /// Tree & Vehicle Props Patch mod enabled?
        /// </summary>
        public static bool isTVPPatchEnabled = false;

        /// <summary>
        /// Move It mod enabled?
        /// </summary>
        public static bool isMoveItEnabled = false;

        public bool firstVisibleFlag = false;

        public static AssetTagList list;

        public UIButton mainButton;
        public UISearchBox searchBox;
        public UIScrollPanel scrollPanel;

        private UIGroupPanel m_groupPanel;
        private RoadsPanel m_roadsPanel;
        private BeautificationPanel m_beautificationPanel;

        private UIPanel defaultPanel;
        private UITextureAtlas defaultPanelAtlas;
        private string defaultPanelBackgroundSprite;

        private float m_defaultXPos;

        public void Start()
        {
            try
            {
                GameObject gameObject = GameObject.Find("FindItMainButton");
                if (gameObject != null)
                {
                    return;
                }

                isRicoEnabled = IsModEnabled("ploppablerico");
                isPOEnabled = IsModEnabled("proceduralobjects");
                isTVPPatchEnabled = IsModEnabled("tvproppatch");
                isMoveItEnabled = IsModEnabled("moveit");

                if (isPOEnabled)
                {
                    POTool = new ProceduralObjectsTool();
                }

                list = AssetTagList.instance;

                UITabstrip tabstrip = ToolsModifierControl.mainToolbar.component as UITabstrip;

                m_defaultXPos = tabstrip.relativePosition.x;
                UpdateMainToolbar();

                GameObject asGameObject = UITemplateManager.GetAsGameObject("MainToolbarButtonTemplate");
                GameObject asGameObject2 = UITemplateManager.GetAsGameObject("ScrollableSubPanelTemplate");

                mainButton = tabstrip.AddTab("FindItMainButton", asGameObject, asGameObject2, new Type[] { typeof(UIGroupPanel) }) as UIButton;
                mainButton.atlas = atlas;

                mainButton.normalBgSprite = "ToolbarIconGroup6Normal";
                mainButton.focusedBgSprite = "ToolbarIconGroup6Focused";
                mainButton.hoveredBgSprite = "ToolbarIconGroup6Hovered";
                mainButton.pressedBgSprite = "ToolbarIconGroup6ressed";
                mainButton.disabledBgSprite = "ToolbarIconGroup6Disabled";

                mainButton.normalFgSprite = "FindIt";
                mainButton.focusedFgSprite = "FindItFocused";
                mainButton.hoveredFgSprite = "FindItHovered";
                mainButton.pressedFgSprite = "FindItPressed";
                mainButton.disabledFgSprite = "FindItDisabled";

                mainButton.tooltip = "Find It! " + (ModInfo.isBeta ? "[BETA] " : "") + ModInfo.version;

                Locale locale = (Locale)typeof(LocaleManager).GetField("m_Locale", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(LocaleManager.instance);
                Locale.Key key = new Locale.Key
                {
                    m_Identifier = "TUTORIAL_ADVISER_TITLE",
                    m_Key = mainButton.name
                };
                if (!locale.Exists(key))
                {
                    locale.AddLocalizedString(key, "Find It! " + ModInfo.version);
                }
                key = new Locale.Key
                {
                    m_Identifier = "TUTORIAL_ADVISER",
                    m_Key = mainButton.name
                };
                if (!locale.Exists(key))
                {
                    locale.AddLocalizedString(key, "Thanks for subscribing to Find It! 2.\n\nStart typing some keywords into the input field to find the desired asset.\n\nCheck the workshop page occasionally for new features or bug reports.");
                }

                FieldInfo m_ObjectIndex = typeof(MainToolbar).GetField("m_ObjectIndex", BindingFlags.Instance | BindingFlags.NonPublic);
                m_ObjectIndex.SetValue(ToolsModifierControl.mainToolbar, (int)m_ObjectIndex.GetValue(ToolsModifierControl.mainToolbar) + 1);

                mainButton.gameObject.GetComponent<TutorialUITag>().tutorialTag = name;
                m_groupPanel = tabstrip.GetComponentInContainer(mainButton, typeof(UIGroupPanel)) as UIGroupPanel;

                if (m_groupPanel != null)
                {
                    m_groupPanel.name = "FindItGroupPanel";
                    m_groupPanel.enabled = true;
                    m_groupPanel.component.isInteractive = true;
                    m_groupPanel.m_OptionsBar = ToolsModifierControl.mainToolbar.m_OptionsBar;
                    m_groupPanel.m_DefaultInfoTooltipAtlas = ToolsModifierControl.mainToolbar.m_DefaultInfoTooltipAtlas;
                    if (ToolsModifierControl.mainToolbar.enabled)
                    {
                        m_groupPanel.RefreshPanel();
                    }

                    scrollPanel = UIScrollPanel.Create(m_groupPanel.GetComponentInChildren<UIScrollablePanel>());
                    scrollPanel.eventClicked += OnButtonClicked;
                    scrollPanel.eventVisibilityChanged += (c, p) =>
                    {
                        HideAllOptionPanels();

                        if (p && scrollPanel.selectedItem != null)
                        {
                            // Simulate item click
                            UIScrollPanelItem.ItemData item = scrollPanel.selectedItem;

                            UIScrollPanelItem panelItem = scrollPanel.GetItem(0);
                            panelItem.Display(scrollPanel.selectedItem, 0);
                            panelItem.component.SimulateClick();

                            scrollPanel.selectedItem = item;

                            scrollPanel.Refresh();
                        }
                    };

                    scrollPanel.eventTooltipEnter += (c, p) =>
                    {
                        UIScrollPanelItem.RefreshTooltipAltas(p.source);
                    };

                    searchBox = scrollPanel.parent.AddUIComponent<UISearchBox>();
                    searchBox.scrollPanel = scrollPanel;
                    searchBox.relativePosition = new Vector3(0, 0);
                    searchBox.Search();
                }
                else
                {
                    Debugging.Message("GroupPanel not found");
                }

                m_roadsPanel = FindObjectOfType<RoadsPanel>();
                m_beautificationPanel = FindObjectOfType<BeautificationPanel>();

                defaultPanel = GameObject.Find("FindItDefaultPanel").GetComponent<UIPanel>();
                defaultPanelAtlas = defaultPanel.atlas;
                defaultPanelBackgroundSprite = defaultPanel.backgroundSprite;
                UpdateDefaultPanelBackground();

                Debugging.Message("Initialized");

            }
            catch (Exception e)
            {
                Debugging.Message("Start failed");
                Debugging.LogException(e);
                enabled = false;
            }
        }

        public void UpdateMainToolbar()
        {
            UITabstrip tabstrip = ToolsModifierControl.mainToolbar.component as UITabstrip;
            if (tabstrip == null) return;

            tabstrip.eventComponentAdded -= new ChildComponentEventHandler(UpdatePosition);
            tabstrip.eventComponentRemoved -= new ChildComponentEventHandler(UpdatePosition);

            if (Settings.centerToolbar)
            {
                tabstrip.eventComponentAdded += new ChildComponentEventHandler(UpdatePosition);
                tabstrip.eventComponentRemoved += new ChildComponentEventHandler(UpdatePosition);
                UpdatePosition(tabstrip, null);
            }
            else
            {
                tabstrip.relativePosition = new Vector3(m_defaultXPos, tabstrip.relativePosition.y);
            }
        }

        private void UpdatePosition(UIComponent c, UIComponent p)
        {
            UITabstrip tabstrip = c as UITabstrip;

            float width = 0;
            foreach (UIComponent child in tabstrip.tabs)
            {
                width += child.width;
            }

            float newXPos = (tabstrip.parent.width - width) / 2;
            tabstrip.relativePosition = new Vector3(Mathf.Min(m_defaultXPos, newXPos), tabstrip.relativePosition.y);
        }

        public void HideAllOptionPanels()
        {
            OptionPanelBase[] panels = ToolsModifierControl.mainToolbar.m_OptionsBar.GetComponentsInChildren<OptionPanelBase>();

            foreach (OptionPanelBase panel in panels)
            {
                panel.Hide();
            }

            UIComponent brushPanel = ToolsModifierControl.mainToolbar.m_OptionsBar.Find("BrushPanel");
            if (brushPanel != null)
            {
                brushPanel.isVisible = false;
            }
        }

        public void OnButtonClicked(UIComponent c, UIMouseEventParameter p)
        {
            UIButton uIButton = p.source as UIButton;
            if (uIButton != null && uIButton.parent is UIScrollPanel)
            {
                HideAllOptionPanels();

                PrefabInfo prefab = uIButton.objectUserData as PrefabInfo;
                string key = Asset.GetName(prefab);
                LocateNextInstanceTool.selectedPrefab = prefab;
                if (AssetTagList.instance.assets.ContainsKey(key) && AssetTagList.instance.assets[key].onButtonClicked != null)
                {
                    AssetTagList.instance.assets[key].onButtonClicked(uIButton);
                }
                else if (m_roadsPanel != null && prefab is NetInfo)
                {
                    typeof(RoadsPanel).GetMethod("OnButtonClicked", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(m_roadsPanel, new object[] { uIButton });

                }
                else if (m_beautificationPanel != null)
                {
                    typeof(BeautificationPanel).GetMethod("OnButtonClicked", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(m_beautificationPanel, new object[] { uIButton });
                }
                else
                {
                    SelectPrefab(prefab);
                }

                Event e = Event.current;
                if ((e.shift || e.control) && isMoveItEnabled && !(prefab is NetInfo))
                {
                    MoveItClone(prefab);
                }
            }
        }

        public static void SelectPrefab(PrefabInfo prefab)
        {
            BuildingInfo buildingInfo = prefab as BuildingInfo;
            NetInfo netInfo = prefab as NetInfo;
            TransportInfo transportInfo = prefab as TransportInfo;
            TreeInfo treeInfo = prefab as TreeInfo;
            PropInfo propInfo = prefab as PropInfo;
            if (buildingInfo != null)
            {
                BuildingTool buildingTool = ToolsModifierControl.SetTool<BuildingTool>();
                if (buildingTool != null)
                {
                    buildingTool.m_prefab = buildingInfo;
                    buildingTool.m_relocate = 0;
                }
            }
            if (netInfo != null)
            {
                NetTool netTool = ToolsModifierControl.SetTool<NetTool>();
                if (netTool != null)
                {
                    netTool.Prefab = netInfo;
                }
            }
            if (transportInfo != null)
            {
                TransportTool transportTool = ToolsModifierControl.SetTool<TransportTool>();
                if (transportTool != null)
                {
                    transportTool.m_prefab = transportInfo;
                    transportTool.m_building = 0;
                }
            }
            if (treeInfo != null)
            {
                TreeTool treeTool = ToolsModifierControl.SetTool<TreeTool>();
                if (treeTool != null)
                {
                    treeTool.m_prefab = treeInfo;
                    treeTool.m_mode = TreeTool.Mode.Single;
                }
            }
            if (propInfo != null)
            {
                PropTool propTool = ToolsModifierControl.SetTool<PropTool>();
                if (propTool != null)
                {
                    propTool.m_prefab = propInfo;
                    propTool.m_mode = PropTool.Mode.Single;
                }
            }
        }

        private static bool IsModEnabled(string assemblyName)
        {
            foreach (PluginManager.PluginInfo plugin in PluginManager.instance.GetPluginsInfo())
            {
                foreach (Assembly assembly in plugin.GetAssemblies())
                {
                    if (assembly.GetName().Name.ToLower() == assemblyName)
                    {
                        if (plugin.isEnabled)
                        {
                            Debugging.Message($"Found enabled mod: {assemblyName}. Find It 2 integration will be applied");
                        }
                        return plugin.isEnabled;
                    }
                }
            }
            return false;
        }

        public static void FixBadProps()
        {
            PropInstance[] buffer = PropManager.instance.m_props.m_buffer;
            uint size = PropManager.instance.m_props.m_size;

            string log = "";

            for (uint i = 0; i < size; i++)
            {
                try
                {
                    if (buffer[i].m_flags != 0)
                    {
                        PropInfo info = buffer[i].Info;

                        if (info == null) continue;

                        if (info.m_requireWaterMap && info.m_lodWaterHeightMap == null)
                        {
                            PropManager.instance.ReleaseProp((ushort)i);
                            log += "Removed " + info.name + "\n";
                        }
                    }
                }
                catch
                { }
            }

            if (log != "") Debugging.Message(log);
        }

        public static UITextureAtlas LoadResources()
        {
            if (atlas == null)
            {
                string[] spriteNames = new string[]
                {
                    "FindIt",
                    "FindItDisabled",
                    "FindItFocused",
                    "FindItHovered",
                    "FindItPressed",
                    "Tag",
                    "ZoningCommercialEco",
                    "ZoningCommercialLeisure",
                    "ZoningCommercialTourist",
                    "ZoningOfficeHightech",
                    "ZoningResidentialHighEco",
                    "ZoningResidentialLowEco",
                    "ToolbarIconPropsBillboards",
                    "ToolbarIconPropsSpecialBillboards",
                    "ExtraFilters",
                    "Dice",
                    "TreeLg",
                    "TreeMd",
                    "TreeSm",
                    "TinyRoads",
                    "QuickMenu",
                    "Clear",
                    "Oneway",
                    "Parking",
                    "NoParking",
                    "GrwbRico",
                    "Runway",
                    "RunwayDisabled",
                    "RunwayFocused",
                    "RunwayHovered",
                    "RunwayPressed",
                    "Taxiway",
                    "TaxiwayDisabled",
                    "TaxiwayFocused",
                    "TaxiwayHovered",
                    "TaxiwayPressed",
                    "Refresh",
                    "Locate"
                };

                atlas = ResourceLoader.CreateTextureAtlas("FindItAtlas", spriteNames, "FindIt.Icons.");

                UITextureAtlas defaultAtlas = ResourceLoader.GetAtlas("Ingame");
                Texture2D[] textures = new Texture2D[]
                {
                    defaultAtlas["ToolbarIconGroup6Focused"].texture,
                    defaultAtlas["ToolbarIconGroup6Hovered"].texture,
                    defaultAtlas["ToolbarIconGroup6Normal"].texture,
                    defaultAtlas["ToolbarIconGroup6Pressed"].texture
                };

                ResourceLoader.AddTexturesInAtlas(atlas, textures);
            }

            return atlas;
        }

        public void UpdateDefaultPanelBackground()
        {
            if (!Settings.useLightBackground)
            {
                defaultPanel.atlas = defaultPanelAtlas;
                defaultPanel.backgroundSprite = defaultPanelBackgroundSprite;
                if (UISearchBox.instance?.panel != null)
                    UISearchBox.instance.panel.backgroundSprite = "GenericTabHovered";
            }
            else
            {
                defaultPanel.atlas = SamsamTS.UIUtils.GetAtlas("Ingame");
                defaultPanel.backgroundSprite = "GenericTabHovered";
                if (UISearchBox.instance?.panel != null)
                    UISearchBox.instance.panel.backgroundSprite = "GenericTab";
            }
        }

        public void MoveItClone(PrefabInfo prefab)
        {
            Debugging.Message("MoveItClone called");

            // make a fake move it export xml

            // call move it Import
            ToolController toolController = UnityEngine.Object.FindObjectOfType<ToolController>();
            Component moveItTool = toolController.GetComponent("MoveItTool");
            toolController.CurrentTool = moveItTool as ToolBase;
            Type MoveItToolType = Type.GetType("MoveIt.MoveItTool");

            MethodInfo ImportMI = MoveItToolType.GetMethod("Import", new Type[] { typeof(string)});
            if (ImportMI == null) Debugging.Message("Import is null");
            ImportMI.Invoke(moveItTool, new object[] { "FindIt2FakeMoveItExport" });

            // delete fake move it export xml


            Debugging.Message("MoveItClone returned");
        }
    }

    public class FindItLoader : LoadingExtensionBase
    {
        public override void OnCreated(ILoading loading)
        {
            if ((ToolManager.instance.m_properties.m_mode & ItemClass.Availability.GameAndMap) != ItemClass.Availability.None)
            {
                //if (FindIt.detourGeneratedScrollPanels.value)
                //{
                //    Redirector<Detours.GeneratedScrollPanelDetour>.Deploy();
                //}
            }
        }

        public override void OnReleased()
        {
            //Redirector<Detours.GeneratedScrollPanelDetour>.Revert();
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            AssetTagList.instance.Init();

            if (Settings.fixBadProps)
            {
                Debugging.Message("Fixing bad props");
                FindIt.FixBadProps();
                Settings.fixBadProps = false;
                Debugging.Message("Bad props fixed");
            }

            if ((ToolManager.instance.m_properties.m_mode & ItemClass.Availability.GameAndMap) != ItemClass.Availability.None)
            {

                if (mode == LoadMode.LoadMap || mode == LoadMode.NewMap) FindIt.inEditor = true;
                else FindIt.inEditor = false;

                if (FindIt.instance == null)
                {
                    // Creating the instance
                    FindIt.instance = new GameObject("FindIt").AddComponent<FindIt>();
                }
                else
                {
                    FindIt.instance.Start();
                }
            }
            else if (mode == LoadMode.NewAsset || mode == LoadMode.LoadAsset)
            {
                FindIt.inEditor = true;

                ToolsModifierControl.toolController.eventEditPrefabChanged += (p) =>
                {
                    if (FindIt.instance == null)
                    {
                        // Creating the instance
                        FindIt.instance = new GameObject("FindIt").AddComponent<FindIt>();
                    }
                    else
                    {
                        FindIt.instance.Start();
                    }
                };
            }
        }
    }
}