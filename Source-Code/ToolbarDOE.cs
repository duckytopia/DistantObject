using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Toolbar;

namespace DistantObject
{
    public partial class SettingsGui : MonoBehaviour
    {
        public static IButton buttonDOSettings;

        private void toolbarButton()
        {
            print("Distant Object Enhancement v1.3 -- Drawing toolbar icon...");
            buttonDOSettings = ToolbarManager.Instance.add("test", "buttonDOSettings");
            buttonDOSettings.Visibility = new GameScenesVisibility(GameScenes.SPACECENTER);
            if(activated)
                buttonDOSettings.TexturePath = "DistantObject/Icons/toolbar_enabled";
            else
                buttonDOSettings.TexturePath = "DistantObject/Icons/toolbar_disabled";
            buttonDOSettings.ToolTip = "Distant Object Enhancement Settings";
            buttonDOSettings.OnClick += (e) => Toggle();
            buttonDOSettings.OnClick += (e) => ToggleIcon();
        }

        private void ToggleIcon()
        {
            if (buttonDOSettings.TexturePath == "DistantObject/Icons/toolbar_disabled")
                buttonDOSettings.TexturePath = "DistantObject/Icons/toolbar_enabled";
            else
                buttonDOSettings.TexturePath = "DistantObject/Icons/toolbar_disabled";
        }

        private void OnDestroy()
        {
            buttonDOSettings.Destroy();
        }
    }
}
