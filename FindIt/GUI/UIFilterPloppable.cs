// modified from SamsamTS's original Find It mod
// https://github.com/SamsamTS/CS-FindIt
// Filter tabs panel for ploppable

using UnityEngine;
using ColossalFramework.UI;

namespace FindIt.GUI
{
    public class UIFilterPloppable : UIPanel
    {
        public static UIFilterPloppable instance;

        public enum Category
        {
            None = -1,
            Electricity = 0,
            Water,
            Garbage,
            Healthcare,
            FireDepartment,
            Police,
            Education,
            PublicTransport,
            Beautification,
            Monuments,
            PlayerIndustry,
            Disaster,
            PlayerEducation,
            VarsitySports,
            Fishing,
            Unsorted,
            All
        }

        public UICheckBox[] toggles;
        public UIButton all;
        private UICheckBox randomIcon;

        public static Category GetCategory(ItemClass itemClass)
        {
            if (itemClass.m_service == ItemClass.Service.Electricity) return Category.Electricity;
            if (itemClass.m_service == ItemClass.Service.Water) return Category.Water;
            if (itemClass.m_service == ItemClass.Service.Garbage) return Category.Garbage;
            if (itemClass.m_service == ItemClass.Service.PlayerIndustry) return Category.PlayerIndustry;
            if (itemClass.m_service == ItemClass.Service.Fishing) return Category.Fishing;
            if (itemClass.m_service == ItemClass.Service.HealthCare) return Category.Healthcare;
            if (itemClass.m_service == ItemClass.Service.FireDepartment) return Category.FireDepartment;
            if (itemClass.m_service == ItemClass.Service.Disaster) return Category.Disaster;
            if (itemClass.m_service == ItemClass.Service.PoliceDepartment) return Category.Police;
            if (itemClass.m_service == ItemClass.Service.Education) return Category.Education;
            if (itemClass.m_service == ItemClass.Service.PlayerEducation) return Category.PlayerEducation;
            if (itemClass.m_service == ItemClass.Service.Museums) return Category.PlayerEducation;
            if (itemClass.m_service == ItemClass.Service.VarsitySports) return Category.VarsitySports;
            if (itemClass.m_service == ItemClass.Service.PublicTransport) return Category.PublicTransport;
            if (itemClass.m_service == ItemClass.Service.Tourism) return Category.PublicTransport;
            if (itemClass.m_service == ItemClass.Service.Beautification) return Category.Beautification;
            if (itemClass.m_service == ItemClass.Service.Monument) return Category.Monuments;

            return Category.Unsorted;
            //if (itemClass.m_service == ItemClass.Service.Wonders) return Category.Wonders; ???????

            //return Category.None;
        }

        public class CategoryIcons
        {
            public static readonly string[] atlases =
            {
                "Ingame",
                "Ingame",
                "Ingame",
                "Ingame",
                "Ingame",
                "Ingame",
                "Ingame",
                "Ingame",
                "Ingame",
                "Ingame",
                "Ingame",
                "Ingame",
                "Ingame",
                "Ingame",
                "Ingame",
                "Ingame"
            };

            public static readonly string[] spriteNames =
            {
                "ToolbarIconElectricity",
                "ToolbarIconWaterAndSewage",
                "SubBarIndustryGarbage",
                "ToolbarIconHealthcare",
                "ToolbarIconFireDepartment",
                "ToolbarIconPolice",
                "ToolbarIconEducation",
                "ToolbarIconPublicTransport",
                "ToolbarIconBeautification",
                "ToolbarIconMonuments",
                "ToolbarIconIndustry",
                "SubBarFireDepartmentDisaster",
                "SubBarCampusAreaUniversity",
                "SubBarCampusAreaVarsitySports",
                "SubBarIndustryFishing",
                "ToolbarIconHelp"
            };

            public static readonly string[] tooltips =
            {
                Translations.Translate("FIF_PLOP_E"), // electricity
                Translations.Translate("FIF_PLOP_W"), // water
                Translations.Translate("FIF_PLOP_G"), // garbage
                Translations.Translate("FIF_PLOP_H"), // healthcare
                Translations.Translate("FIF_PLOP_F"), // fire department
                Translations.Translate("FIF_PLOP_P"), // police
                Translations.Translate("FIF_PLOP_ED"), // education
                Translations.Translate("FIF_PLOP_PT"), // public transportation
                Translations.Translate("FIF_PLOP_PPW"), // parks
                Translations.Translate("FIF_PLOP_U"), // unique buildings
                Translations.Translate("FIF_PLOP_I"), // industry
                Translations.Translate("FIF_PLOP_D"), // disaster
                Translations.Translate("FIF_PLOP_C"), // campus
                Translations.Translate("FIF_PLOP_V"), // varsity sports
                Translations.Translate("FIF_PLOP_FI"), // fishing
                Translations.Translate("FIF_PROP_UNS") // unsorted
            };
        }

        public bool IsSelected(Category category)
        {
            return toggles[(int)category].isChecked;
        }

        public bool IsAllSelected()
        {
            for (int i = 0; i < (int)Category.All; i++)
            {
                if (!toggles[i].isChecked)
                {
                    return false;
                }
            }
            return true;
        }

        public void SelectAll()
        {
            for (int i = 0; i < (int)Category.All; i++)
            {
                toggles[i].isChecked = true;
            }
        }

        public event PropertyChangedEventHandler<int> eventFilteringChanged;

        public override void Start()
        {
            instance = this;

            size = new Vector2(605, 45);

            // generate filter tabs UI
            toggles = new UICheckBox[(int)Category.All];
            for (int i = 0; i < (int)Category.All; i++)
            {
                toggles[i] = SamsamTS.UIUtils.CreateIconToggle(this, CategoryIcons.atlases[i], CategoryIcons.spriteNames[i], CategoryIcons.spriteNames[i], 0.4f);
                toggles[i].tooltip = CategoryIcons.tooltips[i] + "\n" + Translations.Translate("FIF_SE_SC");
                toggles[i].relativePosition = new Vector3(5 + 40 * i, 5);
                toggles[i].isChecked = true;
                toggles[i].readOnly = true;
                toggles[i].checkedBoxObject.isInteractive = false; // Don't eat my double click event please

                toggles[i].eventClick += (c, p) =>
                {
                    Event e = Event.current;

                    if (e.shift || e.control)
                    {
                        ((UICheckBox)c).isChecked = !((UICheckBox)c).isChecked;
                        eventFilteringChanged(this, 0);
                    }
                    else
                    {
                        // when all tabs are checked, toggle a tab will uncheck all the other tabs
                        bool check = true;
                        for (int j = 0; j < (int)Category.All; j++)
                        {
                            check = check && toggles[j].isChecked;
                        }
                        if (check)
                        {
                            for (int j = 0; j < (int)Category.All; j++)
                            {
                                toggles[j].isChecked = false;
                            }
                            ((UICheckBox)c).isChecked = true;
                            eventFilteringChanged(this, 0);
                            return;
                        }

                        // when a tab is unchecked, toggle it will uncheck all the other tabs
                        if (((UICheckBox)c).isChecked == false)
                        {
                            for (int j = 0; j < (int)Category.All; j++)
                                toggles[j].isChecked = false;
                            ((UICheckBox)c).isChecked = true;
                            eventFilteringChanged(this, 0);
                            return;
                        }

                        // when a tab is already checked, toggle it will move back to select all
                        if (((UICheckBox)c).isChecked == true)
                        {
                            for (int j = 0; j < (int)Category.All; j++)
                                toggles[j].isChecked = true;
                            eventFilteringChanged(this, 0);
                            return;
                        }
                    }
                };
            }

            UICheckBox last = toggles[toggles.Length - 1];

            randomIcon = SamsamTS.UIUtils.CreateIconToggle(this, "FindItAtlas", "Dice", "Dice");
            randomIcon.relativePosition = new Vector3(last.relativePosition.x + last.width + 5, 5);
            randomIcon.tooltip = Translations.Translate("FIF_GR_RAN");
            randomIcon.isChecked = true;
            randomIcon.readOnly = true;
            randomIcon.checkedBoxObject.isInteractive = false;
            randomIcon.eventClicked += (c, p) =>
            {
                UISearchBox.instance.PickRandom();
            };

            all = SamsamTS.UIUtils.CreateButton(this);
            all.size = new Vector2(55, 35);
            all.text = Translations.Translate("FIF_SE_IA");
            all.relativePosition = new Vector3(randomIcon.relativePosition.x + last.width + 5, 5);

            all.eventClick += (c, p) =>
            {
                for (int i = 0; i < (int)Category.All; i++)
                {
                    toggles[i].isChecked = true;
                }
                eventFilteringChanged(this, 0);
            };

            width = parent.width;
        }
    }
}
