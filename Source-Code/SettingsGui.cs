using UnityEngine;

namespace DistantObject
{
    // MOARdV TODO: Make this usable in space center and flight
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    partial class SettingsGui : MonoBehaviour
    {
        protected Rect windowPos = new Rect(Screen.width / 4, Screen.height / 4, 10f, 10f);

        private bool toolbarInstalled = false;
        private static bool activated = false;
        private bool isActivated = false;

        private bool flaresEnabled = false;
        private float flareSaturation = 0.65f;
        private float flareSize = 1.0f;
        private float flareBrightness = 1.0f;
        private bool ignoreDebrisFlare = false;
        private float debrisBrightness = 0.15f;
        private bool showNames = false;
        private bool renderVessels = false;
        private float maxDistance = 750000f;
        private int renderMode = 1;
        private bool ignoreDebris = false;
        private bool changeSkybox = true;
        private float maxBrightness = 0.25f;
        private bool debugMode = false;

        private void ApplySettings()
        {
            // Apply our local values to the settings file object, and then
            // save it.
            DistantObjectSettings.DistantFlare.flaresEnabled = flaresEnabled;
            DistantObjectSettings.DistantFlare.flareSaturation = flareSaturation;
            DistantObjectSettings.DistantFlare.flareSize = flareSize;
            DistantObjectSettings.DistantFlare.flareBrightness = flareBrightness;
            DistantObjectSettings.DistantFlare.ignoreDebrisFlare = ignoreDebrisFlare;
            DistantObjectSettings.DistantFlare.debrisBrightness = debrisBrightness;
            DistantObjectSettings.DistantFlare.showNames = showNames;

            DistantObjectSettings.DistantVessel.renderVessels = renderVessels;
            DistantObjectSettings.DistantVessel.maxDistance = maxDistance;
            DistantObjectSettings.DistantVessel.renderMode = renderMode;
            DistantObjectSettings.DistantVessel.ignoreDebris = ignoreDebris;

            DistantObjectSettings.SkyboxBrightness.changeSkybox = changeSkybox;
            DistantObjectSettings.SkyboxBrightness.maxBrightness = maxBrightness;

            DistantObjectSettings.debugMode = debugMode;

            DistantObjectSettings.SaveConfig();
        }

        private void ReadSettings()
        {
            DistantObjectSettings.LoadConfig();

            // Create local copies of the values, so we're not editing the
            // config file until the user presses "Apply"
            flaresEnabled = DistantObjectSettings.DistantFlare.flaresEnabled;
            flareSaturation = DistantObjectSettings.DistantFlare.flareSaturation;
            flareSize = DistantObjectSettings.DistantFlare.flareSize;
            flareBrightness = DistantObjectSettings.DistantFlare.flareBrightness;
            ignoreDebrisFlare = DistantObjectSettings.DistantFlare.ignoreDebrisFlare;
            debrisBrightness = DistantObjectSettings.DistantFlare.debrisBrightness;
            showNames = DistantObjectSettings.DistantFlare.showNames;

            renderVessels = DistantObjectSettings.DistantVessel.renderVessels;
            maxDistance = DistantObjectSettings.DistantVessel.maxDistance;
            renderMode = DistantObjectSettings.DistantVessel.renderMode;
            ignoreDebris = DistantObjectSettings.DistantVessel.ignoreDebris;

            changeSkybox = DistantObjectSettings.SkyboxBrightness.changeSkybox;
            maxBrightness = DistantObjectSettings.SkyboxBrightness.maxBrightness;

            debugMode = DistantObjectSettings.debugMode;
        }

        public void Awake()
        {
            // Load and configure once
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                print(Constants.DistantObject + " -- SettingsGUI initialized");
                foreach (AssemblyLoader.LoadedAssembly assembly in AssemblyLoader.loadedAssemblies)
                {
                    if (assembly.name == "Toolbar")
                    {
                        toolbarInstalled = true;
                    }
                }
                if (toolbarInstalled)
                {
                    toolbarButton();
                }

                RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI));

                //Load settings
                ReadSettings();
            }
        }

        private void mainGUI(int windowID)
        {
            GUIStyle styleWindow = new GUIStyle(GUI.skin.window);
            styleWindow.padding.left = 4;
            styleWindow.padding.top = 4;
            styleWindow.padding.bottom = 4;
            styleWindow.padding.right = 4;

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            GUILayout.Label("");
            GUILayout.EndHorizontal();

            //--- Flare Rendering --------------------------------------------
            GUILayout.BeginVertical("Flare Rendering", new GUIStyle(GUI.skin.window));
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            flaresEnabled = GUILayout.Toggle(flaresEnabled, "Enable Flares");
            GUILayout.EndHorizontal();

            if (flaresEnabled)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                showNames = GUILayout.Toggle(showNames, "Show names on mouseover");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                GUILayout.Label("Flare Saturation");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                flareSaturation = GUILayout.HorizontalSlider(flareSaturation, 0f, 1f, GUILayout.Width(240));
                GUILayout.Label(string.Format("{0:0}", 100 * flareSaturation) + "%");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                GUILayout.Label("Flare Size");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                flareSize = GUILayout.HorizontalSlider(flareSize, 0.5f, 1.5f, GUILayout.Width(240));
                GUILayout.Label(string.Format("{0:0}", 100 * flareSize) + "%");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                GUILayout.Label("Flare Brightness");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                flareBrightness = GUILayout.HorizontalSlider(flareBrightness, 0.0f, 1.0f, GUILayout.Width(240));
                GUILayout.Label(string.Format("{0:0}", 100 * flareBrightness) + "%");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                ignoreDebrisFlare = !GUILayout.Toggle(!ignoreDebrisFlare, "Show Debris Flares");
                GUILayout.EndHorizontal();

                if (!ignoreDebrisFlare)
                {
                    GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                    GUILayout.Label("Debris Brightness");
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                    debrisBrightness = GUILayout.HorizontalSlider(debrisBrightness, 0f, 1f, GUILayout.Width(240));
                    GUILayout.Label(string.Format("{0:0}", 100 * debrisBrightness) + "%");
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            GUILayout.Label("");
            GUILayout.EndHorizontal();
            
            //--- Vessel Rendering -------------------------------------------
            GUILayout.BeginVertical("Distant Vessel", new GUIStyle(GUI.skin.window));

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            renderVessels = GUILayout.Toggle(renderVessels, "Distant Vessel Rendering");
            GUILayout.EndHorizontal();

            if (renderVessels)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                GUILayout.Label("Max Distance to Render");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                maxDistance = GUILayout.HorizontalSlider(maxDistance, 2500f, 750000f, GUILayout.Width(200));
                GUILayout.Label(string.Format("{0:0}", maxDistance) + "m");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                if (GUILayout.Button(renderMode == 0 ? "Render All Unloaded Vessels" : "Render Targeted Vessel Only"))
                {
                    if (renderMode == 0)
                    {
                        renderMode = 1;
                    }
                    else
                    {
                        renderMode = 0;
                    }
                }
                GUILayout.EndHorizontal();

                if (renderMode == 1)
                {
                    GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                    ignoreDebris = GUILayout.Toggle(ignoreDebris, "Ignore Debris");
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            GUILayout.Label("");
            GUILayout.EndHorizontal();

            //--- Skybox Brightness ------------------------------------------
            GUILayout.BeginVertical("Skybox Dimming", new GUIStyle(GUI.skin.window));
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));

            changeSkybox = GUILayout.Toggle(changeSkybox, "Dynamic Sky Dimming");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            GUILayout.Label("Maximum Sky Brightness");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            maxBrightness = GUILayout.HorizontalSlider(maxBrightness, 0f, 1f, GUILayout.Width(240));
            GUILayout.Label(string.Format("{0:0}", 100 * maxBrightness) + "%");
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            GUILayout.Label("");
            GUILayout.EndHorizontal();

            //--- Misc. ------------------------------------------------------
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            debugMode = GUILayout.Toggle(debugMode, "Debug Mode");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
            if (GUILayout.Button("Reset To Default"))
            {
                Reset();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
            GUIStyle styleApply = new GUIStyle(GUI.skin.button);
            styleApply.fontSize = styleApply.fontSize + 2;
            if (GUILayout.Button("Apply", GUILayout.Height(50)))
            {
                ApplySettings();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void drawGUI()
        {
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                if (activated)
                {
                    if (!isActivated)
                    {
                        ReadSettings();
                    }
                    windowPos = GUILayout.Window(-5234628, windowPos, mainGUI, Constants.DistantObject + " Settings", GUILayout.Width(300), GUILayout.Height(200));
                }
                isActivated = activated;
            }
        }

        private void Reset()
        {
            flaresEnabled = true;
            flareSaturation = 0.65f;
            flareSize = 1.0f;
            flareBrightness = 1.0f;
            ignoreDebrisFlare = false;
            debrisBrightness = 0.15f;
            showNames = false;

            renderVessels = false;
            maxDistance = 750000f;
            renderMode = 1;
            ignoreDebris = false;

            changeSkybox = true;
            maxBrightness = 0.25f;

            debugMode = false;
        }

        public static void Toggle()
        {
            activated = !activated;
        }
    }
}
