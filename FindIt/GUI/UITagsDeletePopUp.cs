﻿// modified from SamsamTS's original Find It mod
// https://github.com/SamsamTS/CS-FindIt

using UnityEngine;
using ColossalFramework.UI;

namespace FindIt.GUI
{
    public class UITagsDeletePopUp : UIPanel
    {
        public static UITagsDeletePopUp instance;
        private UIComponent m_button;

        private const float spacing = 5f;

        private UIButton confirmButton;
        private UIButton cancelButton;
        private string tagToDelete;

        public override void Start()
        {
            name = "FindIt_TagsWindow";
            atlas = SamsamTS.UIUtils.GetAtlas("Ingame");
            backgroundSprite = "GenericPanelWhite";
            size = new Vector2(400, 145);

            UILabel title = AddUIComponent<UILabel>();
            title.text = Translations.Translate("FIF_DE_TIT");
            title.textColor = new Color32(0, 0, 0, 255);
            title.relativePosition = new Vector3(spacing, spacing);

            UILabel message = AddUIComponent<UILabel>();
            message.text = "\n" + Translations.Translate("FIF_DE_MSG") + "\n" + Translations.Translate("FIF_POP_NU");
            message.textColor = new Color32(0, 0, 0, 255);
            message.relativePosition = new Vector3(spacing, spacing + title.height + spacing);

            confirmButton = SamsamTS.UIUtils.CreateButton(this);
            confirmButton.size = new Vector2(100, 40);
            confirmButton.text = Translations.Translate("FIF_POP_CON");
            confirmButton.relativePosition = new Vector3(spacing, message.relativePosition.y + message.height + spacing * 2);
            confirmButton.eventClick += (c, p) =>
            {
                DeleteTag(tagToDelete);
                ((UIFilterTag)m_button.parent).UpdateCustomTagList();
                Close();
            };

            cancelButton = SamsamTS.UIUtils.CreateButton(this);
            cancelButton.size = new Vector2(100, 40);
            cancelButton.text = Translations.Translate("FIF_POP_CAN");
            cancelButton.relativePosition = new Vector3(confirmButton.relativePosition.x + confirmButton.width + spacing*4, confirmButton.relativePosition.y);
            cancelButton.eventClick += (c, p) =>
            {
                Close();
            };
        }

        private static void Close()
        {
            if (instance != null)
            {
                UIView.PopModal();
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

        public static void ShowAt(UIComponent component, string tag)
        {
            if (instance == null)
            {
                instance = UIView.GetAView().AddUIComponent(typeof(UITagsDeletePopUp)) as UITagsDeletePopUp;
                instance.m_button = component;
                instance.Show(true);
                UIView.PushModal(instance);
            }
            else
            {
                instance.m_button = component;
                instance.Show(true);
            }
            instance.tagToDelete = tag;
        }

        // delete a tag and remove it from all tagged assets
        public void DeleteTag(string tag)
        {
            foreach (Asset asset in AssetTagList.instance.assets.Values)
            {
                if (!asset.tagsCustom.Contains(tag)) continue;
                // remove tag
                AssetTagList.instance.RemoveCustomTag(asset, tag);
            }
            Debugging.Message("Custom tag: " + tag + " deleted");
        }
    }
}