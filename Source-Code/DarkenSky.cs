using System;
using UnityEngine;

namespace DistantObject
{
    //Peachoftree: It was EveryScene so the sky would darken in places like the starting menu and the tracking center, not just flight and map veiw 
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class DarkenSky : MonoBehaviour
    {
        private Color galaxyColor = Color.black;
        private float glareFadeLimit = 0.0f;
        private bool restorableGalaxyCube = false;

        public void Awake()
        {
            restorableGalaxyCube = false;

            DistantObjectSettings.LoadConfig();

            if (GalaxyCubeControl.Instance != null)
            {
                restorableGalaxyCube = true;
                galaxyColor = GalaxyCubeControl.Instance.maxGalaxyColor;
                glareFadeLimit = GalaxyCubeControl.Instance.glareFadeLimit;

                if (DistantObjectSettings.SkyboxBrightness.changeSkybox)
                {
                    GalaxyCubeControl.Instance.maxGalaxyColor = new Color(DistantObjectSettings.SkyboxBrightness.maxBrightness, DistantObjectSettings.SkyboxBrightness.maxBrightness, DistantObjectSettings.SkyboxBrightness.maxBrightness);
                    GalaxyCubeControl.Instance.glareFadeLimit = 1f;
                }
            }
        }

        public void OnDestroy()
        {
            if (GalaxyCubeControl.Instance != null && restorableGalaxyCube)
            {
                GalaxyCubeControl.Instance.maxGalaxyColor = galaxyColor;
                GalaxyCubeControl.Instance.glareFadeLimit = glareFadeLimit;
                restorableGalaxyCube = false;
            }
        }

        public void Update()
        {
            if (GalaxyCubeControl.Instance != null)
            {
                if (DistantObjectSettings.SkyboxBrightness.changeSkybox)
                {
                    Color color = new Color(DistantObjectSettings.SkyboxBrightness.maxBrightness, DistantObjectSettings.SkyboxBrightness.maxBrightness, DistantObjectSettings.SkyboxBrightness.maxBrightness);

                    if (HighLogic.LoadedSceneIsFlight && !MapView.MapIsEnabled)
                    {
                        Vector3d camPos = FlightCamera.fetch.mainCamera.transform.position;
                        float camFov = FlightCamera.fetch.mainCamera.fieldOfView;
                        Vector3d camAngle = FlightCamera.fetch.mainCamera.transform.forward;

                        for (int i = 0; i < FlightGlobals.Bodies.Count; ++i)
                        {
                            double bodyRadius = FlightGlobals.Bodies[i].Radius;
                            double bodyDist = FlightGlobals.Bodies[i].GetAltitude(camPos) + bodyRadius;
                            float bodySize = Mathf.Acos((float)(Math.Sqrt(bodyDist * bodyDist - bodyRadius * bodyRadius) / bodyDist)) * Mathf.Rad2Deg;

                            if (bodySize > 1.0f)
                            {
                                Vector3d bodyPosition = FlightGlobals.Bodies[i].position;
                                Vector3d targetVectorToSun = FlightGlobals.Bodies[0].position - bodyPosition;
                                Vector3d targetVectorToCam = camPos - bodyPosition;

                                float targetRelAngle = (float)Vector3d.Angle(targetVectorToSun, targetVectorToCam);
                                targetRelAngle = Mathf.Max(targetRelAngle, bodySize);
                                targetRelAngle = Mathf.Min(targetRelAngle, 100.0f);
                                targetRelAngle = 1.0f - ((targetRelAngle - bodySize) / (100.0f - bodySize));

                                float CBAngle = Mathf.Max(0.0f, Vector3.Angle((bodyPosition - camPos).normalized, camAngle) - bodySize);
                                CBAngle = 1.0f - Mathf.Min(1.0f, Math.Max(0.0f, (CBAngle - (camFov / 2.0f)) - 5.0f) / (camFov / 4.0f));
                                bodySize = Mathf.Min(bodySize, 60.0f);

                                float colorScalar = 1.0f - (targetRelAngle * (Mathf.Sqrt(bodySize / 60.0f)) * CBAngle);
                                color.r *= colorScalar;
                                color.g *= colorScalar;
                                color.b *= colorScalar;
                            }
                        }
                    }

                    GalaxyCubeControl.Instance.maxGalaxyColor = color;
                }
                else if (restorableGalaxyCube)
                {
                    GalaxyCubeControl.Instance.maxGalaxyColor = galaxyColor;
                    GalaxyCubeControl.Instance.glareFadeLimit = glareFadeLimit;
                }
            }
        }
    }
}
