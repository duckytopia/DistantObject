using UnityEngine;
using Toolbar;

namespace DistantObject
{
    public partial class SettingsGui : MonoBehaviour
    {
        public static IButton buttonDOSettings;

        private void toolbarButton()
        {
            print(Constants.DistantObject + " -- Drawing toolbar icon...");
            buttonDOSettings = ToolbarManager.Instance.add("test", "buttonDOSettings");
            buttonDOSettings.Visibility = new GameScenesVisibility(GameScenes.SPACECENTER, GameScenes.FLIGHT);
            if (activated)
            {
                buttonDOSettings.TexturePath = "DistantObject/Icons/toolbar_enabled";
            }
            else
            {
                buttonDOSettings.TexturePath = "DistantObject/Icons/toolbar_disabled";
            }
            buttonDOSettings.ToolTip = "Distant Object Enhancement Settings";
            buttonDOSettings.OnClick += (e) => Toggle();
            buttonDOSettings.OnClick += (e) => ToggleIcon();
        }

        private void ToggleIcon()
        {
            if (buttonDOSettings.TexturePath == "DistantObject/Icons/toolbar_disabled")
            {
                buttonDOSettings.TexturePath = "DistantObject/Icons/toolbar_enabled";
            }
            else
            {
                buttonDOSettings.TexturePath = "DistantObject/Icons/toolbar_disabled";
            }
        }

        private void OnDestroy()
        {
            buttonDOSettings.Destroy();
        }
    }
}
