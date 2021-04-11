// modified from SamsamTS's original Find It mod
// https://github.com/SamsamTS/CS-FindIt

using UnityEngine;
using ColossalFramework.UI;

namespace FindIt.GUI
{
    public class UIQuickMenuPopUp : UIPanel
    {
        public static UIQuickMenuPopUp instance;
        private const float spacing = 5f;
        private UIDropDown instanceCounterSort;
        private UICheckBox includePOinstances;

        public override void Start()
        {
            name = "FindIt_UIQuickMenuPopUp";
            atlas = SamsamTS.UIUtils.GetAtlas("Ingame");
            backgroundSprite = "GenericPanelWhite";
            size = new Vector2(600, 250);
            relativePosition += new Vector3(-200, -200);

            UILabel title = AddUIComponent<UILabel>();
            title.text = Translations.Translate("FIF_QM_TIT");
            title.textColor = new Color32(0, 0, 0, 255);
            title.relativePosition = new Vector3(spacing * 3, spacing * 3);

            UIButton close = AddUIComponent<UIButton>();
            close.size = new Vector2(30f, 30f);
            close.text = "X";
            close.textScale = 0.9f;
            close.textColor = new Color32(0, 0, 0, 255);
            close.focusedTextColor = new Color32(0, 0, 0, 255);
            close.hoveredTextColor = new Color32(109, 109, 109, 255);
            close.pressedTextColor = new Color32(128, 128, 128, 102);
            close.textPadding = new RectOffset(8, 8, 8, 8);
            close.canFocus = false;
            close.playAudioEvents = true;
            close.relativePosition = new Vector3(width - close.width, 0);
            close.eventClicked += (c, p) => Close();

            // Sort custom tag list alphabetically. Default = sort by number of assets in each tag
            UICheckBox customTagListSort = SamsamTS.UIUtils.CreateCheckBox(this);
            customTagListSort.isChecked = Settings.customTagListSort;
            customTagListSort.label.text = Translations.Translate("FIF_SET_CTLS");
            customTagListSort.label.textScale = 0.8f;
            customTagListSort.width = size.x;
            customTagListSort.label.textColor = new Color32(0, 0, 0, 255);
            customTagListSort.relativePosition = new Vector3(title.relativePosition.x, title.relativePosition.y + title.height + 20);
            customTagListSort.eventCheckChanged += (c, i) =>
            {
                Settings.customTagListSort = customTagListSort.isChecked;
                XMLUtils.SaveSettings();
                if (UIFilterTag.instance != null)
                {
                    UIFilterTag.instance.UpdateCustomTagList();
                    UISearchBox.instance.Search();
                }
            };

            // Sort asset creator list alphabetically. Default = sort by number of assets in each tag
            UICheckBox assetCreatorListSort = SamsamTS.UIUtils.CreateCheckBox(this);
            assetCreatorListSort.isChecked = Settings.assetCreatorListSort;
            assetCreatorListSort.label.text = Translations.Translate("FIF_SET_ACLS");
            assetCreatorListSort.label.textScale = 0.8f;
            assetCreatorListSort.width = size.x;
            assetCreatorListSort.label.textColor = new Color32(0, 0, 0, 255);
            assetCreatorListSort.relativePosition = new Vector3(title.relativePosition.x, customTagListSort.relativePosition.y + customTagListSort.height + 10);
            assetCreatorListSort.eventCheckChanged += (c, i) =>
            {
                Settings.assetCreatorListSort = assetCreatorListSort.isChecked;
                XMLUtils.SaveSettings();
                if (UIFilterExtra.instance != null)
                {
                    UIFilterExtra.instance.UpdateAssetCreatorList();
                    UISearchBox.instance.Search();
                }
            };

            // Show prop markers in 'game' mode
            UICheckBox showPropMarker = SamsamTS.UIUtils.CreateCheckBox(this);
            showPropMarker.isChecked = Settings.showPropMarker;
            showPropMarker.label.text = Translations.Translate("FIF_SET_PM");
            showPropMarker.label.textScale = 0.8f;
            showPropMarker.width = size.x;
            showPropMarker.label.textColor = new Color32(0, 0, 0, 255);
            showPropMarker.tooltip = Translations.Translate("FIF_SET_PMTP");
            showPropMarker.relativePosition = new Vector3(title.relativePosition.x, assetCreatorListSort.relativePosition.y + assetCreatorListSort.height + 10);
            showPropMarker.eventCheckChanged += (c, i) =>
            {
                Settings.showPropMarker = showPropMarker.isChecked;
                XMLUtils.SaveSettings();
                UIFilterProp.instance.UpdateMarkerToggleVisibility();
            };

            // Use light background theme
            UICheckBox useLightBackground = SamsamTS.UIUtils.CreateCheckBox(this);
            useLightBackground.isChecked = Settings.useLightBackground;
            useLightBackground.label.text = Translations.Translate("FIF_SET_BACK");
            useLightBackground.label.textScale = 0.8f;
            useLightBackground.width = size.x;
            useLightBackground.label.textColor = new Color32(0, 0, 0, 255);
            useLightBackground.tooltip = Translations.Translate("FIF_SET_BACKTP");
            useLightBackground.relativePosition = new Vector3(title.relativePosition.x, showPropMarker.relativePosition.y + showPropMarker.height + 10);
            useLightBackground.eventCheckChanged += (c, i) =>
            {
                Settings.useLightBackground = useLightBackground.isChecked;
                XMLUtils.SaveSettings();
                FindIt.instance.UpdateDefaultPanelBackground();
            };

            // Use Move It to place props
            UICheckBox useMoveItForProps = SamsamTS.UIUtils.CreateCheckBox(this);
            useMoveItForProps.isChecked = Settings.useMoveItForProps;
            useMoveItForProps.label.text = Translations.Translate("FIF_SET_MIP");
            useMoveItForProps.label.textScale = 0.8f;
            useMoveItForProps.width = size.x;
            useMoveItForProps.label.textColor = new Color32(0, 0, 0, 255);
            useMoveItForProps.relativePosition = new Vector3(title.relativePosition.x, useLightBackground.relativePosition.y + useLightBackground.height + 10);
            useMoveItForProps.eventCheckChanged += (c, i) =>
            {
                Settings.useMoveItForProps = useMoveItForProps.isChecked;
                XMLUtils.SaveSettings();
            };

            // Show asset type panel
            UICheckBox showAssetTypePanel = SamsamTS.UIUtils.CreateCheckBox(this);
            showAssetTypePanel.isChecked = Settings.showAssetTypePanel;
            showAssetTypePanel.label.text = Translations.Translate("FIF_SET_ATP");
            showAssetTypePanel.label.textScale = 0.8f;
            showAssetTypePanel.width = size.x;
            showAssetTypePanel.label.textColor = new Color32(0, 0, 0, 255);
            showAssetTypePanel.tooltip = Translations.Translate("FIF_ATP_TP");
            showAssetTypePanel.relativePosition = new Vector3(title.relativePosition.x, useMoveItForProps.relativePosition.y + useMoveItForProps.height + 10);
            showAssetTypePanel.eventCheckChanged += (c, i) =>
            {
                Settings.showAssetTypePanel = showAssetTypePanel.isChecked;
                XMLUtils.SaveSettings();
                if (Settings.showAssetTypePanel) UISearchBox.instance.assetTypePanel.isVisible = true; //UISearchBox.instance.CreateAssetTypePanel();
                else UISearchBox.instance.assetTypePanel.isVisible = false; //UISearchBox.instance.DestroyAssetTypePanel();
            };

            // Show the number of existing instances of each asset
            UICheckBox showInstancesCounter = SamsamTS.UIUtils.CreateCheckBox(this);
            showInstancesCounter.isChecked = Settings.showInstancesCounter;
            showInstancesCounter.label.text = Translations.Translate("FIF_SET_IC");
            showInstancesCounter.label.textScale = 0.8f;
            showInstancesCounter.width = size.x;
            showInstancesCounter.tooltip = Translations.Translate("FIF_SET_ICTP");
            showInstancesCounter.label.textColor = new Color32(0, 0, 0, 255);
            showInstancesCounter.relativePosition = new Vector3(title.relativePosition.x, showAssetTypePanel.relativePosition.y + showAssetTypePanel.height + 10);
            showInstancesCounter.eventCheckChanged += (c, i) =>
            {
                if (showInstancesCounter.isChecked)
                {
                    height = instanceCounterSort.relativePosition.y + instanceCounterSort.height + 30;
                }
                else
                {
                    height = showInstancesCounter.relativePosition.y + showInstancesCounter.height + 30;
                }

                Settings.showInstancesCounter = showInstancesCounter.isChecked;
                instanceCounterSort.isVisible = showInstancesCounter.isChecked;
                if (FindIt.isPOEnabled)
                {
                    includePOinstances.isVisible = showInstancesCounter.isChecked;
                }
                XMLUtils.SaveSettings();
                if (Settings.showInstancesCounter && AssetTagList.instance?.prefabInstanceCountDictionary != null)
                {
                    AssetTagList.instance.UpdatePrefabInstanceCount(UISearchBox.DropDownOptions.All);
                }
                UISearchBox.instance.Search();
            };

            // include PO instances
            includePOinstances = SamsamTS.UIUtils.CreateCheckBox(this);
            includePOinstances.isChecked = Settings.includePOinstances;
            includePOinstances.label.text = Translations.Translate("FIF_SET_PO");
            includePOinstances.label.textScale = 0.8f;
            includePOinstances.width = size.x;
            includePOinstances.tooltip = Translations.Translate("FIF_SET_ICTP");
            includePOinstances.isVisible = Settings.showInstancesCounter;
            includePOinstances.label.textColor = new Color32(0, 0, 0, 255);
            includePOinstances.relativePosition = new Vector3(showInstancesCounter.relativePosition.x + 30, showInstancesCounter.relativePosition.y + showInstancesCounter.height + 10);
            includePOinstances.eventCheckChanged += (c, i) =>
            {
                Settings.includePOinstances = includePOinstances.isChecked;
                XMLUtils.SaveSettings();
                if (Settings.showInstancesCounter)
                {
                    UISearchBox.instance.Search();
                }
            };

            instanceCounterSort = SamsamTS.UIUtils.CreateDropDown(this);
            instanceCounterSort.normalBgSprite = "TextFieldPanelHovered";
            instanceCounterSort.size = new Vector2(300, 30);
            instanceCounterSort.listHeight = 300;
            instanceCounterSort.itemHeight = 30;
            instanceCounterSort.AddItem(Translations.Translate("FIF_SET_ICO"));
            instanceCounterSort.AddItem(Translations.Translate("FIF_SET_ICUS"));
            instanceCounterSort.AddItem(Translations.Translate("FIF_SET_ICUN"));
            instanceCounterSort.selectedIndex = Settings.instanceCounterSort;
            instanceCounterSort.isVisible = Settings.showInstancesCounter;
            instanceCounterSort.relativePosition = new Vector3(showInstancesCounter.relativePosition.x + 30, includePOinstances.relativePosition.y + includePOinstances.height + 10);
            instanceCounterSort.eventSelectedIndexChanged += (c, p) =>
            {
                Settings.instanceCounterSort = instanceCounterSort.selectedIndex;
                XMLUtils.SaveSettings();
                if (Settings.showInstancesCounter)
                {
                    UISearchBox.instance.Search();
                }
            };

            if (!FindIt.isPOEnabled)
            {
                includePOinstances.isVisible = false;
                instanceCounterSort.relativePosition = new Vector3(showInstancesCounter.relativePosition.x + 30, showInstancesCounter.relativePosition.y + showInstancesCounter.height + 10);
            }

            if (showInstancesCounter.isChecked)
            {
                height = instanceCounterSort.relativePosition.y + instanceCounterSort.height + 30;
            }
            else
            {
                height = showInstancesCounter.relativePosition.y + showInstancesCounter.height + 30;
            }

            customTagListSort.Focus();
        }

        private static void Close()
        {
            if (instance != null)
            {
                UIView.PopModal();
                UISearchBox.instance.quickMenuVisible = false;
                UISearchBox.instance.quickMenuIcon.opacity = 0.5f;
                instance.isVisible = false;
                Destroy(instance.gameObject);
                instance = null;
            }
        }

        protected override void OnKeyDown(UIKeyEventParameter p)
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                p.Use();
                Close();
            }

            base.OnKeyDown(p);
        }

        public static void ShowAt(UIComponent component)
        {
            if (instance == null)
            {
                instance = UIView.GetAView().AddUIComponent(typeof(UIQuickMenuPopUp)) as UIQuickMenuPopUp;

                UIView.PushModal(instance);
            }
            instance.Show(true);
        }

    }
}
