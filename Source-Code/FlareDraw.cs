using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DistantObject
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class FlareDraw : MonoBehaviour
    {
        private static Dictionary<Vessel, GameObject> vesselMeshLookup = new Dictionary<Vessel, GameObject>();
        private static Dictionary<GameObject, Vessel> meshVesselLookup = new Dictionary<GameObject, Vessel>();
        private static Dictionary<Vessel, double> vesselLuminosityLookup = new Dictionary<Vessel, double>();
        private static Dictionary<CelestialBody, GameObject> bodyMeshLookup = new Dictionary<CelestialBody, GameObject>();
        private static Dictionary<CelestialBody, Color> bodyColorLookup = new Dictionary<CelestialBody, Color>();
        private static Dictionary<GameObject, int[]> meshBlockedLookup = new Dictionary<GameObject, int[]>();
        private static Dictionary<GameObject, MeshRenderer> meshRendererLookup = new Dictionary<GameObject, MeshRenderer>();

        private static Dictionary<CelestialBody, double> bodyDist = new Dictionary<CelestialBody, double>();
        private static Dictionary<CelestialBody, double> bodySize = new Dictionary<CelestialBody, double>();
        private static Dictionary<CelestialBody, Vector3d> bodyAngle = new Dictionary<CelestialBody, Vector3d>();
        private static Vector3d camPos;
        private static float camFOV;
        private static float atmosphereFactor = 1.0f;
        private static float dimFactor = 1.0f;

        private List<string> situations = new List<string>();

        private string showNameString = null;
        private Transform showNameTransform = null;
        private Color showNameColor;

        public static Dictionary<GameObject, Vector3d> debugDeltaPos = new Dictionary<GameObject, Vector3d>();

        private static void DrawVesselFlare(Vessel referenceShip)
        {
            if (DistantObjectSettings.debugMode)
            {
                print("DistObj: Drawing effect for vessel " + referenceShip.vesselName);
            }

            GameObject flare = GameDatabase.Instance.GetModel("DistantObject/Flare/model");
            GameObject flareMesh = Mesh.Instantiate(flare) as GameObject;
            DestroyObject(flare);

            flareMesh.name = referenceShip.vesselName;
            flareMesh.SetActive(true);

            double luminosity = 0;
            foreach (ProtoPartSnapshot part in referenceShip.protoVessel.protoPartSnapshots)
            {
                luminosity += Math.Pow(part.mass, 2);
            }

            MeshRenderer flareMR = flareMesh.GetComponentInChildren<MeshRenderer>();
            flareMR.gameObject.layer = 10;
            flareMR.material.shader = Shader.Find("KSP/Alpha/Unlit Transparent");
            flareMR.material.color = Color.white;
            flareMR.castShadows = false;
            flareMR.receiveShadows = false;

            //update definitions
            vesselMeshLookup.Add(referenceShip, flareMesh);
            meshVesselLookup.Add(flareMesh, referenceShip);
            vesselLuminosityLookup.Add(referenceShip, luminosity);
            meshRendererLookup.Add(flareMesh, flareMR);

            //position, rotate, and scale mesh
            flareMesh.transform.SetParent(referenceShip.transform);
            UpdateVesselFlare(flareMesh);
        }

        private static void DrawBodyFlare(CelestialBody referenceBody)
        {
            if (DistantObjectSettings.debugMode)
            {
                print("DistObj: Drawing effect for body " + referenceBody.bodyName);
            }

            GameObject flare = GameDatabase.Instance.GetModel("DistantObject/Flare/model");
            GameObject flareMesh = Mesh.Instantiate(flare) as GameObject;
            DestroyObject(flare);

            flareMesh.name = referenceBody.bodyName;
            flareMesh.SetActive(true);

            MeshRenderer flareMR = flareMesh.GetComponentInChildren<MeshRenderer>();
            flareMR.gameObject.layer = 10;
            flareMR.material.shader = Shader.Find("KSP/Alpha/Unlit Transparent");
            Color color = Color.white;
            if (bodyColorLookup.ContainsKey(referenceBody))
            {
                color = bodyColorLookup[referenceBody];
            }
            flareMR.material.color = color;
            flareMR.castShadows = false;
            flareMR.receiveShadows = false;

            //update definitions
            bodyMeshLookup.Add(referenceBody, flareMesh);
            meshRendererLookup.Add(flareMesh, flareMR);

            UpdateBodyFlare(referenceBody);

        }

        private static void UpdateVesselFlare(GameObject flareMesh)
        {
            double targetDist = Vector3d.Distance(flareMesh.transform.position, camPos);
            if (targetDist > 750000 && flareMesh.activeSelf)
            {
                flareMesh.SetActive(false);
            }
            if (targetDist < 750000 && !flareMesh.activeSelf)
            {
                flareMesh.SetActive(true);
            }
            double luminosity = vesselLuminosityLookup[meshVesselLookup[flareMesh]];
            double brightness = Math.Log10(luminosity) * (1 - Math.Pow(targetDist / 750000, 1.25));

            CheckDraw(flareMesh, flareMesh.transform.position, meshVesselLookup[flareMesh].mainBody, 5, true);

            flareMesh.transform.LookAt(camPos);
            Vector3d resizeVector = new Vector3d(1, 1, 1);
            resizeVector *= (0.002 * Vector3d.Distance(flareMesh.transform.position, camPos) * brightness * (0.7 + .99 * camFOV) / 70) * DistantObjectSettings.DistantFlare.flareSize;
            flareMesh.transform.localScale = resizeVector;
        }

        private static void UpdateBodyFlare(CelestialBody targetBody)
        {
            double targetSunDist = Vector3d.Distance(targetBody.position, FlightGlobals.Bodies[0].position);
            double camSunDist = Vector3d.Distance(camPos, FlightGlobals.Bodies[0].position);
            Vector3d targetVectorToSun = FlightGlobals.Bodies[0].position - targetBody.position;
            Vector3d targetVectorToCam = camPos - targetBody.position;
            double targetRelAngle = Vector3d.Angle(targetVectorToSun, targetVectorToCam);

            double luminosity = (1 / Math.Pow(targetSunDist / FlightGlobals.Bodies[1].orbit.semiMajorAxis, 2)) * Math.Pow(targetBody.Radius / FlightGlobals.Bodies[1].Radius, 2);
            luminosity *= (0.5 + (32400 - Math.Pow(targetRelAngle, 2)) / 64800);
            luminosity = (Math.Log10(luminosity) + 1.5) * (-2);

            double brightness = luminosity + Math.Log10(bodyDist[targetBody] / FlightGlobals.Bodies[1].orbit.semiMajorAxis);

            GameObject flareMesh = bodyMeshLookup[targetBody];

            CheckDraw(flareMesh, targetBody.transform.position, targetBody.referenceBody, targetBody.Radius, false);

            //position, rotate, and scale mesh
            targetVectorToCam = (750000 * targetVectorToCam.normalized);
            //targetVectorToCam *= (740000 / targetVectorToCam.magnitude);
            flareMesh.transform.position = camPos - targetVectorToCam;
            flareMesh.transform.LookAt(camPos);
            Vector3d resizeVector = new Vector3d(1, 1, 1);
            resizeVector *= (-750 * (brightness - 5) * (0.7 + .99 * camFOV) / 70) * DistantObjectSettings.DistantFlare.flareSize;
            flareMesh.transform.localScale = resizeVector;
        }

        private static void CheckDraw(GameObject flareMesh, Vector3d position, CelestialBody referenceBody, double objRadius, bool isVessel)
        {
            Vector3d targetVectorToSun = FlightGlobals.Bodies[0].position - position;
            Vector3d targetVectorToRef = referenceBody.position - position;
            double targetRelAngle = Vector3d.Angle(targetVectorToSun, targetVectorToRef);
            double targetDist = Vector3d.Distance(position, camPos);
            double targetSize;
            if (isVessel)
            {
                targetSize = Math.Atan2(objRadius, targetDist) * (180 / Math.PI);
            }
            else
            {
                targetSize = bodySize[FlightGlobals.Bodies.Find(n => n.name == flareMesh.name)];
            }
            double targetRefDist = Vector3d.Distance(position, referenceBody.position);
            double targetRefSize = Math.Acos(Math.Sqrt(Math.Pow(targetRefDist, 2) - Math.Pow(referenceBody.Radius, 2)) / targetRefDist) * (180 / Math.PI);

            bool inShadow = false;
            if (referenceBody != FlightGlobals.Bodies[0] && targetRelAngle < targetRefSize)
            {
                inShadow = true;
            }

            bool isVisible = true;
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body.bodyName != flareMesh.name && bodyDist[body] < targetDist && bodySize[body] > targetSize && Vector3d.Angle(bodyAngle[body], position - camPos) < bodySize[body])
                {
                    isVisible = false;
                }
            }

            MeshRenderer flareMR = meshRendererLookup[flareMesh];
            Color color = flareMR.material.color;

            if (targetSize < (camFOV / 500) && !inShadow && isVisible)
            {
                color.a = atmosphereFactor * dimFactor;
                if (targetSize > (camFOV / 1000))
                {
                    color.a *= (float)(((camFOV / targetSize) / 500) - 1);
                }
                if (meshVesselLookup.ContainsKey(flareMesh))
                {
                    if (meshVesselLookup[flareMesh].vesselType == VesselType.Debris)
                    {
                        color.a *= DistantObjectSettings.DistantFlare.debrisBrightness;
                    }
                }
            }
            else
            {
                color.a = 0;
            }

            flareMR.material.color = color;
        }

        private void UpdateVar()
        {
            camPos = FlightCamera.fetch.mainCamera.transform.position;
            camFOV = FlightCamera.fetch.mainCamera.fieldOfView;

            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (bodyDist.ContainsKey(body))
                {
                    bodyDist[body] = body.GetAltitude(camPos) + body.Radius;
                }
                else
                {
                    bodyDist.Add(body, body.GetAltitude(camPos) + body.Radius);
                    //bodyDist.Add(body, Vector3d.Distance(body.position, camPos));
                }

                if (bodySize.ContainsKey(body))
                {
                    bodySize[body] = Math.Acos(Math.Sqrt(Math.Pow(bodyDist[body], 2) - Math.Pow(body.Radius, 2)) / bodyDist[body]) * (180 / Math.PI);
                }
                else
                {
                    bodySize.Add(body, Math.Acos(Math.Sqrt(Math.Pow(bodyDist[body], 2) - Math.Pow(body.Radius, 2)) / bodyDist[body]) * (180 / Math.PI));
                }

                if (bodyAngle.ContainsKey(body))
                {
                    bodyAngle[body] = (body.position - camPos).normalized;
                }
                else
                {
                    bodyAngle.Add(body, (body.position - camPos).normalized);
                }
            }

            atmosphereFactor = 1;
            if (FlightGlobals.currentMainBody != null && FlightGlobals.currentMainBody.atmosphere)
            {
                double camAltitude = FlightGlobals.currentMainBody.GetAltitude(camPos);
                double atmAltitude = FlightGlobals.currentMainBody.maxAtmosphereAltitude;
                double atmCurrentBrightness = (Vector3d.Distance(camPos, FlightGlobals.Bodies[0].position) - Vector3d.Distance(FlightGlobals.currentMainBody.position, FlightGlobals.Bodies[0].position)) / (FlightGlobals.currentMainBody.Radius);

                if (camAltitude > (atmAltitude / 2) || atmCurrentBrightness > 0.15)
                {
                    atmosphereFactor = 1;
                }
                else if (camAltitude < (atmAltitude / 10) && atmCurrentBrightness < 0.05)
                {
                    atmosphereFactor = 0;
                }
                else
                {
                    if (camAltitude < (atmAltitude / 2) && camAltitude > (atmAltitude / 10) && atmCurrentBrightness < 0.15)
                    {
                        atmosphereFactor *= (float)((camAltitude - (atmAltitude / 10)) / (atmAltitude - (atmAltitude / 10)));
                    }
                    if (atmCurrentBrightness < 0.15 && atmCurrentBrightness > 0.05 && camAltitude < (atmAltitude / 2))
                    {
                        atmosphereFactor *= (float)((atmCurrentBrightness - 0.05) / (0.10));
                    }
                    if (atmosphereFactor > 1)
                    {
                        atmosphereFactor = 1;
                    }
                }
                float atmThickness = (float)Math.Min(Math.Sqrt(FlightGlobals.currentMainBody.atmosphereMultiplier), 1);
                atmosphereFactor = (atmThickness)*(atmosphereFactor) + (1 - atmThickness);
            }

            dimFactor = (GalaxyCubeControl.Instance.maxGalaxyColor.r / DistantObjectSettings.SkyboxBrightness.maxBrightness);
            double angCamToSun = Vector3d.Angle(FlightCamera.fetch.mainCamera.transform.forward, bodyAngle[FlightGlobals.Bodies[0]]);
            if (angCamToSun < (camFOV / 2))
            {
                bool isVisible = true;
                foreach (CelestialBody body in FlightGlobals.Bodies)
                {
                    if (body.bodyName != FlightGlobals.Bodies[0].bodyName && bodyDist[body] < bodyDist[FlightGlobals.Bodies[0]] && bodySize[body] > bodySize[FlightGlobals.Bodies[0]] && Vector3d.Angle(bodyAngle[body], FlightGlobals.Bodies[0].position - camPos) < bodySize[body])
                    {
                        isVisible = false;
                    }
                }
                if (isVisible)
                {
                    dimFactor *= (float)Math.Pow(angCamToSun / (camFOV / 2), 4);
                }
            }
            dimFactor = Math.Max(0.5f, dimFactor);
            dimFactor *= DistantObjectSettings.DistantFlare.flareBrightness;
        }

        private void UpdateNameShown()
        {
            if (DistantObjectSettings.DistantFlare.showNames && !MapView.MapIsEnabled)
            {
                showNameTransform = null;
                Ray mouseRay = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);

                // Detect CelestialBody mouseovers
                double bestRadius = -1;
                foreach (CelestialBody body in bodyMeshLookup.Keys)
                {
                    if (body == FlightGlobals.ActiveVessel.mainBody)
                    {
                        continue;
                    }
                    Vector3d vectorToBody = body.position - mouseRay.origin;
                    double mouseBodyAngle = Vector3d.Angle(vectorToBody, mouseRay.direction);
                    if (mouseBodyAngle < 1.0)
                    {
                        if (body.Radius > bestRadius)
                        {
                            double distance = Vector3d.Distance(FlightCamera.fetch.mainCamera.transform.position, body.position);
                            double angularSize = (180 / Math.PI) * body.Radius / distance;
                            if (angularSize < 0.2)
                            {
                                bestRadius = body.Radius;
                                showNameTransform = body.transform;
                                showNameString = body.bodyName;
                                if (bodyColorLookup.ContainsKey(body))
                                {
                                    showNameColor = bodyColorLookup[body];
                                }
                                else
                                {
                                    showNameColor = Color.white;
                                }
                            }
                        }
                    }
                }

                if (showNameTransform == null)
                {
                    // Detect Vessel mouseovers
                    double bestBrightness = 0.25; // min luminosity to show vessel name
                    foreach (Vessel v in vesselMeshLookup.Keys)
                    {
                        GameObject mesh = vesselMeshLookup[v];
                        MeshRenderer flareMR = meshRendererLookup[mesh];
                        if (flareMR.material.color.a > 0)
                        {
                            Vector3d vectorToVessel = v.transform.position - mouseRay.origin;
                            double mouseVesselAngle = Vector3d.Angle(vectorToVessel, mouseRay.direction);
                            if (mouseVesselAngle < 1.0)
                            {
                                double luminosity = vesselLuminosityLookup[v];
                                double distance = Vector3d.Distance(FlightCamera.fetch.mainCamera.transform.position, v.transform.position);
                                double brightness = Math.Log10(luminosity) * (1 - Math.Pow(distance / 750000, 1.25));
                                // MOARdV TODO: Distance as a configurable parameter?
                                if (brightness > bestBrightness && distance < 750000.0)
                                {
                                    bestBrightness = brightness;
                                    showNameTransform = v.transform;
                                    showNameString = v.vesselName;
                                    showNameColor = Color.white;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Awake()
        {
            meshVesselLookup.Clear();
            vesselMeshLookup.Clear();
            vesselLuminosityLookup.Clear();
            bodyMeshLookup.Clear();
            bodyColorLookup.Clear();
            meshBlockedLookup.Clear();
            meshRendererLookup.Clear();
            bodyDist.Clear();
            bodySize.Clear();
            bodyAngle.Clear();

            DistantObjectSettings.LoadConfig();
            situations = DistantObjectSettings.DistantFlare.situations.Split(',').ToList();

            foreach (UrlDir.UrlConfig node in GameDatabase.Instance.GetConfigs("CelestialBodyColor"))
            {
                CelestialBody body = FlightGlobals.Bodies.Find(n => n.name == node.config.GetValue("name"));
                if (FlightGlobals.Bodies.Contains(body))
                {
                    Color color = ConfigNode.ParseColor(node.config.GetValue("color"));
                    color.r = 1 - (DistantObjectSettings.DistantFlare.flareSaturation * (1 - (color.r / 255)));
                    color.g = 1 - (DistantObjectSettings.DistantFlare.flareSaturation * (1 - (color.g / 255)));
                    color.b = 1 - (DistantObjectSettings.DistantFlare.flareSaturation * (1 - (color.b / 255)));
                    color.a = 1;
                    if (!bodyColorLookup.ContainsKey(body))
                    {
                        bodyColorLookup.Add(body, color);
                    }
                }
            }
            if (DistantObjectSettings.DistantFlare.flaresEnabled)
            {
                print(Constants.DistantObject + " -- FlareDraw initialized");
            }
            else
            {
                print(Constants.DistantObject + " -- FlareDraw disabled");
            }

            if (DistantObjectSettings.DistantFlare.flaresEnabled)
            {
                StartCoroutine("StartUp");
            }

            // Remove Vessels from our dictionaries just before they are destroyed.
            // After they are destroyed they are == null and this confuses Dictionary.
            GameEvents.onVesselWillDestroy.Add(RemoveVesselFlare);
        }

        private void RemoveVesselFlare(Vessel v)
        {
            if (vesselMeshLookup.ContainsKey(v))
            {
                if (DistantObjectSettings.debugMode)
                {
                    print("DistObj: Erasing flare for vessel " + v.name);
                }
                GameObject flareMesh = vesselMeshLookup[v];
                meshRendererLookup.Remove(flareMesh);
                meshVesselLookup.Remove(flareMesh);
                vesselMeshLookup.Remove(v);
                vesselLuminosityLookup.Remove(v);
                DestroyObject(flareMesh);
            }
        }

        private void Update()
        {
            if (DistantObjectSettings.DistantFlare.flaresEnabled)
            {
                UpdateVar();

                foreach (Vessel vessel in FlightGlobals.Vessels)
                {
                    if (vessel.vesselType != VesselType.Flag && vessel.vesselType != VesselType.EVA && !vessel.loaded && !vesselMeshLookup.ContainsKey(vessel) && (vessel.vesselType != VesselType.Debris || !DistantObjectSettings.DistantFlare.ignoreDebrisFlare) && situations.Contains(vessel.situation.ToString()))
                    {
                        DrawVesselFlare(vessel);
                    }
                }

                restart:
                foreach (GameObject flareMesh in meshVesselLookup.Keys)
                {
                    Vessel v = meshVesselLookup[flareMesh];
                    if (v == null || v.loaded || !situations.Contains(v.situation.ToString()))
                    {
                        RemoveVesselFlare(v);
                        goto restart;
                    }

                    UpdateVesselFlare(flareMesh);
                }

                foreach (CelestialBody targetBody in bodyMeshLookup.Keys)
                {
                    UpdateBodyFlare(targetBody);
                }

                UpdateNameShown();
            }
        }

        private void OnGUI()
        {
            if (DistantObjectSettings.DistantFlare.flaresEnabled && DistantObjectSettings.DistantFlare.showNames && !MapView.MapIsEnabled && showNameTransform != null)
            {
                Vector3 screenPos = FlightCamera.fetch.mainCamera.WorldToScreenPoint(showNameTransform.position);
                Rect screenRect = new Rect(screenPos.x, Screen.height - screenPos.y - 20, 100, 20);
                GUIStyle s = new GUIStyle();
                s.normal.textColor = showNameColor;
                GUI.Label(screenRect, showNameString, s);
            }
        }

        private System.Collections.IEnumerator StartUp()
        {
            yield return new WaitForSeconds(1f);

            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body != FlightGlobals.Bodies[0])
                {
                    DrawBodyFlare(body);
                }
            }
        }
    }
}
