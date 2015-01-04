using System;
using UnityEngine;

namespace DistantObject
{
    //Peachoftree: It was EveryScene so the sky would darken in places like the starting menu and the tracking center, not just flight and map veiw 
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class DarkenSky : MonoBehaviour
    {
        private Color maxColor = Color.black;

        public void Awake()
        {
            DistantObjectSettings.LoadConfig();
            float maxBrightness = DistantObjectSettings.SkyboxBrightness.maxBrightness;
            maxColor.r = maxBrightness;
            maxColor.b = maxBrightness;
            maxColor.g = maxBrightness;
            if (GalaxyCubeControl.Instance != null)
            {
                GalaxyCubeControl.Instance.maxGalaxyColor = maxColor;

                if (DistantObjectSettings.SkyboxBrightness.changeSkybox)
                {
                    GalaxyCubeControl.Instance.glareFadeLimit = 1f;
                }
            }
        }

        public void Update()
        {
            if (DistantObjectSettings.SkyboxBrightness.changeSkybox && HighLogic.LoadedSceneIsFlight)
            {
                Color color = maxColor;

                if (!MapView.MapIsEnabled)
                {
                    Vector3d camPos = FlightCamera.fetch.mainCamera.transform.position;
                    float camFov = FlightCamera.fetch.mainCamera.fieldOfView;
                    Vector3d camAngle = FlightCamera.fetch.mainCamera.transform.forward;

                    foreach (CelestialBody body in FlightGlobals.Bodies)
                    {
                        double bodyDist = body.GetAltitude(camPos) + body.Radius;
                        double bodySize = Math.Acos(Math.Sqrt(Math.Pow(bodyDist, 2) - Math.Pow(body.Radius, 2)) / bodyDist) * (180 / Math.PI);

                        if(bodySize > 1)
                        {
                            Vector3d targetVectorToSun = FlightGlobals.Bodies[0].position - body.position;
                            Vector3d targetVectorToCam = camPos - body.position;
                            double targetRelAngle = Vector3d.Angle(targetVectorToSun, targetVectorToCam);
                            targetRelAngle = Math.Max(targetRelAngle, bodySize);
                            targetRelAngle = Math.Min(targetRelAngle, 100);
                            targetRelAngle = 1 - ((targetRelAngle - bodySize) / (100 - bodySize));
                            double CBAngle = Math.Max(0, Vector3d.Angle((body.position - camPos).normalized, camAngle) - bodySize);
                            CBAngle = 1 - Math.Min(1, Math.Max(0, (CBAngle - (camFov / 2)) - 5) / (camFov / 4));
                            bodySize = Math.Min(bodySize, 60);

                            color.r *= (float)(1 - ((targetRelAngle) * (Math.Sqrt(bodySize / 60)) * CBAngle));
                            color.g *= (float)(1 - ((targetRelAngle) * (Math.Sqrt(bodySize / 60)) * CBAngle));
                            color.b *= (float)(1 - ((targetRelAngle) * (Math.Sqrt(bodySize / 60)) * CBAngle));
                        }
                    }
                }

                GalaxyCubeControl.Instance.maxGalaxyColor = color;
            }
        }
    }
}
