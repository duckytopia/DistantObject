using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DistantObject
{
    class BodyFlare
    {
        public CelestialBody body;
        public GameObject bodyMesh;
        public MeshRenderer meshRenderer;
        public Color color;
        public Vector3d bodyAngle;
        public double bodyDist;
        public double bodySize;

        public void Update(Vector3d camPos, float camFOV)
        {
            // Update Body Flare
            double targetSunDist = Vector3d.Distance(body.position, FlightGlobals.Bodies[0].position);
            double camSunDist = Vector3d.Distance(camPos, FlightGlobals.Bodies[0].position);
            Vector3d targetVectorToSun = FlightGlobals.Bodies[0].position - body.position;
            Vector3d targetVectorToCam = camPos - body.position;
            double targetSunRelAngle = Vector3d.Angle(targetVectorToSun, targetVectorToCam);

            double luminosity = (1.0 / Math.Pow(targetSunDist / FlightGlobals.Bodies[1].orbit.semiMajorAxis, 2.0)) * Math.Pow(body.Radius / FlightGlobals.Bodies[1].Radius, 2.0);
            luminosity *= (0.5 + (32400.0 - Math.Pow(targetSunRelAngle, 2.0)) / 64800.0);
            luminosity = (Math.Log10(luminosity) + 1.5) * (-2);

            double brightness = luminosity + Math.Log10(bodyDist / FlightGlobals.Bodies[1].orbit.semiMajorAxis);

            //position, rotate, and scale mesh
            targetVectorToCam = (750000.0 * targetVectorToCam.normalized);
            bodyMesh.transform.position = camPos - targetVectorToCam;
            bodyMesh.transform.LookAt(camPos);

            Vector3d resizeVector = Vector3d.one;
            resizeVector *= (-750.0 * (brightness - 5.0) * (0.7 + .99 * camFOV) / 70.0) * DistantObjectSettings.DistantFlare.flareSize;
            bodyMesh.transform.localScale = resizeVector;

            bodyDist = body.GetAltitude(camPos) + body.Radius;
            bodySize = Math.Acos(Math.Sqrt(Math.Pow(bodyDist, 2.0) - Math.Pow(body.Radius, 2.0)) / bodyDist) * (180.0 / Math.PI);
            bodyAngle = (body.position - camPos).normalized;
        }
    }

    class VesselFlare
    {
        public Vessel referenceShip;
        public GameObject flareMesh;
        public MeshRenderer meshRenderer;
        public double luminosity;
        public double brightness;

        public void Update(Vector3d camPos, float camFOV)
        {
            double targetDist = Vector3d.Distance(flareMesh.transform.position, camPos);
            if (targetDist > 750000.0 && flareMesh.activeSelf)
            {
                flareMesh.SetActive(false);
            }
            if (targetDist < 750000.0 && !flareMesh.activeSelf)
            {
                flareMesh.SetActive(true);
            }

            if (flareMesh.activeSelf)
            {
                brightness = Math.Log10(luminosity) * (1.0 - Math.Pow(targetDist / 750000.0, 1.25));

                flareMesh.transform.LookAt(camPos);
                Vector3d resizeVector = Vector3d.one;
                resizeVector *= (0.002 * Vector3d.Distance(flareMesh.transform.position, camPos) * brightness * (0.7 + .99 * camFOV) / 70.0) * DistantObjectSettings.DistantFlare.flareSize;

                flareMesh.transform.localScale = resizeVector;
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlareDraw : MonoBehaviour
    {
        enum FlareType
        {
            Celestial,
            Vessel,
            Debris
        }

        private List<BodyFlare> bodyFlares = new List<BodyFlare>();
        private Dictionary<Vessel, VesselFlare> vesselFlares = new Dictionary<Vessel, VesselFlare>();

        private static float camFOV;
        private Vector3d camPos;
        private float atmosphereFactor = 1.0f;
        private float dimFactor = 1.0f;

        private static bool ExternalControl = false;

        private List<Vessel.Situations> situations = new List<Vessel.Situations>();

        private string showNameString = null;
        private Transform showNameTransform = null;
        private Color showNameColor;

        private List<Transform> scaledTransforms = new List<Transform>();

        //--------------------------------------------------------------------
        // AddVesselFlare
        // Add a new vessel flare to our library
        private void AddVesselFlare(Vessel referenceShip)
        {
            GameObject flare = GameDatabase.Instance.GetModel("DistantObject/Flare/model");
            GameObject flareMesh = Mesh.Instantiate(flare) as GameObject;
            DestroyObject(flare);

            flareMesh.name = referenceShip.vesselName;
            flareMesh.SetActive(true);

            double luminosity = 0.0;
            foreach (ProtoPartSnapshot part in referenceShip.protoVessel.protoPartSnapshots)
            {
                luminosity += Math.Pow(part.mass, 2.0);
            }
            // MOARdV: Luminosity can be < 1 for small / light craft, which leads to a
            // negative localScale for a flare mesh.  Clamp to 1.0
            luminosity = Math.Max(luminosity, 1.0);

            MeshRenderer flareMR = flareMesh.GetComponentInChildren<MeshRenderer>();
            // MOARdV: valerian recommended moving vessel and body flares to
            // layer 10, but that behaves poorly for nearby / co-orbital objects.
            // Move vessels back to layer 0 until I can find a better place to
            // put it.
            //flareMR.gameObject.layer = 10;
            flareMR.material.shader = Shader.Find("KSP/Alpha/Unlit Transparent");
            flareMR.material.color = Color.white;
            flareMR.castShadows = false;
            flareMR.receiveShadows = false;

            flareMesh.transform.SetParent(referenceShip.transform);

            VesselFlare vesselFlare = new VesselFlare();
            vesselFlare.flareMesh = flareMesh;
            vesselFlare.meshRenderer = flareMR;
            vesselFlare.referenceShip = referenceShip;
            vesselFlare.luminosity = luminosity;
            vesselFlare.brightness = 0.0;

            vesselFlares.Add(referenceShip, vesselFlare);
        }

        //--------------------------------------------------------------------
        // GenerateBodyFlares
        // Iterate over the celestial bodies and generate flares for each of
        // them.  Add the flare info to the dictionary.
        private void GenerateBodyFlares()
        {
            bodyFlares.Clear();

            Dictionary<CelestialBody, Color> bodyColors = new Dictionary<CelestialBody, Color>();
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
                    if (!bodyColors.ContainsKey(body))
                    {
                        bodyColors.Add(body, color);
                    }
                }
            }

            GameObject flare = GameDatabase.Instance.GetModel("DistantObject/Flare/model");

            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body != FlightGlobals.Bodies[0])
                {
                    BodyFlare bf = new BodyFlare();

                    GameObject flareMesh = Mesh.Instantiate(flare) as GameObject;

                    flareMesh.name = body.bodyName;
                    flareMesh.SetActive(true);

                    MeshRenderer flareMR = flareMesh.GetComponentInChildren<MeshRenderer>();
                    flareMR.gameObject.layer = 10;
                    flareMR.material.shader = Shader.Find("KSP/Alpha/Unlit Transparent");
                    Color color = Color.white;
                    if (bodyColors.ContainsKey(body))
                    {
                        color = bodyColors[body];
                    }
                    flareMR.material.color = color;
                    flareMR.castShadows = false;
                    flareMR.receiveShadows = false;

                    bf.body = body;
                    bf.bodyMesh = flareMesh;
                    bf.meshRenderer = flareMR;
                    bf.color = color;

                    bodyFlares.Add(bf);
                }
            }

            DestroyObject(flare);
        }

        //--------------------------------------------------------------------
        // GenerateVesselFlares
        // Iterate over the vessels, adding and removing flares as appropriate
        private void GenerateVesselFlares()
        {
            // See if there are vessels that need to be removed from our live
            // list
            List<Vessel> deadVessels = new List<Vessel>();
            foreach(Vessel v in vesselFlares.Keys)
            {
                if(v.loaded == true || !situations.Contains(v.situation))
                {
                    deadVessels.Add(v);
                }
            }

            foreach(Vessel v in deadVessels)
            {
                RemoveVesselFlare(v);
            }

            // See which vessels we should add
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (!vesselFlares.ContainsKey(vessel) && RenderableVesselType(vessel.vesselType) && !vessel.loaded && situations.Contains(vessel.situation))
                {
                    AddVesselFlare(vessel);
                }
            }
        }

        //--------------------------------------------------------------------
        // CheckDraw
        // Checks if the given mesh should be drawn.
        private void CheckDraw(GameObject flareMesh, MeshRenderer flareMR, Vector3d position, CelestialBody referenceBody, double objRadius, FlareType flareType)
        {
            Vector3d targetVectorToSun = FlightGlobals.Bodies[0].position - position;
            Vector3d targetVectorToRef = referenceBody.position - position;
            double targetRelAngle = Vector3d.Angle(targetVectorToSun, targetVectorToRef);
            double targetDist = Vector3d.Distance(position, camPos);
            double targetSize;
            if (flareType == FlareType.Celestial)
            {
                targetSize = objRadius;
            }
            else
            {
                targetSize = Math.Atan2(objRadius, targetDist) * (180 / Math.PI);
            }
            double targetRefDist = Vector3d.Distance(position, referenceBody.position);
            double targetRefSize = Math.Acos(Math.Sqrt(Math.Pow(targetRefDist, 2) - Math.Pow(referenceBody.Radius, 2)) / targetRefDist) * (180 / Math.PI);

            bool inShadow = false;
            if (referenceBody != FlightGlobals.Bodies[0] && targetRelAngle < targetRefSize)
            {
                inShadow = true;
            }

            bool isVisible;
            if (inShadow)
            {
                isVisible = false;
            }
            else
            {
                isVisible = true;

                foreach (BodyFlare bodyFlare in bodyFlares)
                {
                    if (bodyFlare.body.bodyName != flareMesh.name && bodyFlare.bodyDist < targetDist && bodyFlare.bodySize > targetSize && Vector3d.Angle(bodyFlare.bodyAngle, position - camPos) < bodyFlare.bodySize)
                    {
                        isVisible = false;
                    }
                }
            }

            Color color = flareMR.material.color;

            if (targetSize < (camFOV / 500.0f) && isVisible && !MapView.MapIsEnabled)
            {
                color.a = atmosphereFactor * dimFactor;
                if (targetSize > (camFOV / 1000.0f))
                {
                    color.a *= (float)(((camFOV / targetSize) / 500.0) - 1.0);
                }
                if (flareType == FlareType.Debris)
                {
                    color.a *= DistantObjectSettings.DistantFlare.debrisBrightness;
                }
            }
            else
            {
                color.a = 0.0f;
            }

            flareMR.material.color = color;
        }

        //--------------------------------------------------------------------
        // RenderableVesselType
        // Indicates whether the specified vessel type is one we will render
        private bool RenderableVesselType(VesselType vesselType)
        {
            return !(vesselType == VesselType.Flag || vesselType == VesselType.EVA || (vesselType == VesselType.Debris && DistantObjectSettings.DistantFlare.ignoreDebrisFlare));
        }

        //--------------------------------------------------------------------
        // UpdateVar()
        // Update atmosphereFactor and dimFactor
        private void UpdateVar()
        {
            Vector3d sunBodyAngle = (FlightGlobals.Bodies[0].position - camPos).normalized;
            double sunBodyDist = FlightGlobals.Bodies[0].GetAltitude(camPos) + FlightGlobals.Bodies[0].Radius;
            double sunBodySize = Math.Acos(Math.Sqrt(Math.Pow(sunBodyDist, 2) - Math.Pow(FlightGlobals.Bodies[0].Radius, 2)) / sunBodyDist) * (180 / Math.PI);

            atmosphereFactor = 1.0f;

            if (FlightGlobals.currentMainBody != null && FlightGlobals.currentMainBody.atmosphere)
            {
                double camAltitude = FlightGlobals.currentMainBody.GetAltitude(camPos);
                double atmAltitude = FlightGlobals.currentMainBody.maxAtmosphereAltitude;
                double atmCurrentBrightness = (Vector3d.Distance(camPos, FlightGlobals.Bodies[0].position) - Vector3d.Distance(FlightGlobals.currentMainBody.position, FlightGlobals.Bodies[0].position)) / (FlightGlobals.currentMainBody.Radius);

                if (camAltitude > (atmAltitude / 2.0) || atmCurrentBrightness > 0.15)
                {
                    atmosphereFactor = 1.0f;
                }
                else if (camAltitude < (atmAltitude / 10.0) && atmCurrentBrightness < 0.05)
                {
                    atmosphereFactor = 0.0f;
                }
                else
                {
                    if (camAltitude < (atmAltitude / 2.0) && camAltitude > (atmAltitude / 10.0) && atmCurrentBrightness < 0.15)
                    {
                        atmosphereFactor *= (float)((camAltitude - (atmAltitude / 10.0)) / (atmAltitude - (atmAltitude / 10.0)));
                    }
                    if (atmCurrentBrightness < 0.15 && atmCurrentBrightness > 0.05 && camAltitude < (atmAltitude / 2.0))
                    {
                        atmosphereFactor *= (float)((atmCurrentBrightness - 0.05) / (0.10));
                    }
                    if (atmosphereFactor > 1.0f)
                    {
                        atmosphereFactor = 1.0f;
                    }
                }
                float atmThickness = (float)Math.Min(Math.Sqrt(FlightGlobals.currentMainBody.atmosphereMultiplier), 1);
                atmosphereFactor = (atmThickness)*(atmosphereFactor) + (1 - atmThickness);
            }

            dimFactor = Mathf.Min(1.0f, GalaxyCubeControl.Instance.maxGalaxyColor.r / DistantObjectSettings.SkyboxBrightness.maxBrightness);

            double angCamToSun = Vector3d.Angle(FlightCamera.fetch.mainCamera.transform.forward, sunBodyAngle);
            if (angCamToSun < (camFOV / 2.0f))
            {
                bool isVisible = true;
                foreach (BodyFlare bodyFlare in bodyFlares)
                {
                    if (bodyFlare.bodyDist < sunBodyDist && bodyFlare.bodySize > sunBodySize && Vector3d.Angle(bodyFlare.bodyAngle, FlightGlobals.Bodies[0].position - camPos) < bodyFlare.bodySize)
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

        //--------------------------------------------------------------------
        // UpdateNameShown
        // Update the mousever name (if applicable)
        private void UpdateNameShown()
        {
            showNameTransform = null;
            if (DistantObjectSettings.DistantFlare.showNames && !MapView.MapIsEnabled)
            {
                Ray mouseRay = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);

                // Detect CelestialBody mouseovers
                double bestRadius = -1.0;
                foreach (BodyFlare bodyFlare in bodyFlares)
                {
                    if (bodyFlare.body == FlightGlobals.ActiveVessel.mainBody)
                    {
                        continue;
                    }
                    Vector3d vectorToBody = bodyFlare.body.position - mouseRay.origin;
                    double mouseBodyAngle = Vector3d.Angle(vectorToBody, mouseRay.direction);
                    if (mouseBodyAngle < 1.0)
                    {
                        if (bodyFlare.body.Radius > bestRadius)
                        {
                            double distance = Vector3d.Distance(FlightCamera.fetch.mainCamera.transform.position, bodyFlare.body.position);
                            double angularSize = (180 / Math.PI) * bodyFlare.body.Radius / distance;
                            if (angularSize < 0.2)
                            {
                                bestRadius = bodyFlare.body.Radius;
                                showNameTransform = bodyFlare.body.transform;
                                showNameString = bodyFlare.body.bodyName;
                                showNameColor = bodyFlare.color;
                            }
                        }
                    }
                }

                if (showNameTransform == null)
                {
                    // Detect Vessel mouseovers
                    double bestBrightness = 0.01; // min luminosity to show vessel name
                    foreach (VesselFlare vesselFlare in vesselFlares.Values)
                    {
                        //MeshRenderer flareMR = vesselFlare.meshRenderer;
                        if (vesselFlare.flareMesh.activeSelf && vesselFlare.meshRenderer.material.color.a > 0.0f)
                        {
                            Vector3d vectorToVessel = vesselFlare.referenceShip.transform.position - mouseRay.origin;
                            double mouseVesselAngle = Vector3d.Angle(vectorToVessel, mouseRay.direction);
                            if (mouseVesselAngle < 1.0)
                            {
                                //double distance = Vector3d.Distance(FlightCamera.fetch.mainCamera.transform.position, vesselFlare.referenceShip.transform.position);
                                //double brightness = Math.Log10(vesselFlare.luminosity) * (1 - Math.Pow(distance / 750000, 1.25));
                                double brightness = vesselFlare.brightness;
                                // MOARdV TODO: Distance as a configurable parameter?
                                if (brightness > bestBrightness /*&& distance < 750000.0*/)
                                {
                                    bestBrightness = brightness;
                                    showNameTransform = vesselFlare.referenceShip.transform;
                                    showNameString = vesselFlare.referenceShip.vesselName;
                                    showNameColor = Color.white;
                                }
                            }
                        }
                    }
                }
            }
        }

        //--------------------------------------------------------------------
        // Awake()
        // Load configs, set up the callback, 
        private void Awake()
        {
            DistantObjectSettings.LoadConfig();

            Dictionary<string, Vessel.Situations> namedSituations = new Dictionary<string, Vessel.Situations> {
                { Vessel.Situations.LANDED.ToString(), Vessel.Situations.LANDED},
                { Vessel.Situations.SPLASHED.ToString(), Vessel.Situations.SPLASHED},
                { Vessel.Situations.PRELAUNCH.ToString(), Vessel.Situations.PRELAUNCH},
                { Vessel.Situations.FLYING.ToString(), Vessel.Situations.FLYING},
                { Vessel.Situations.SUB_ORBITAL.ToString(), Vessel.Situations.SUB_ORBITAL},
                { Vessel.Situations.ORBITING.ToString(), Vessel.Situations.ORBITING},
                { Vessel.Situations.ESCAPING.ToString(), Vessel.Situations.ESCAPING},
                { Vessel.Situations.DOCKED.ToString(), Vessel.Situations.DOCKED},
            };

            List<string> situationStrings = DistantObjectSettings.DistantFlare.situations.Split(',').ToList();

            foreach(string sit in situationStrings)
            {
                if(namedSituations.ContainsKey(sit))
                {
                    situations.Add(namedSituations[sit]);
                }
                else
                {
                    Debug.LogWarning(Constants.DistantObject + " -- Unable to find situation '" + sit + "' in my known situations atlas");
                }
            }

            if (DistantObjectSettings.DistantFlare.flaresEnabled)
            {
                Debug.Log(Constants.DistantObject + " -- FlareDraw initialized");
            }
            else
            {
                Debug.Log(Constants.DistantObject + " -- FlareDraw disabled");
            }

            GenerateBodyFlares();

            // Remove Vessels from our dictionaries just before they are destroyed.
            // After they are destroyed they are == null and this confuses Dictionary.
            GameEvents.onVesselWillDestroy.Add(RemoveVesselFlare);

            // Cache a list of the scaledTransforms so we know which worlds
            // are being rendered.
            scaledTransforms =
                ScaledSpace.Instance.scaledSpaceTransforms
                .Where(ss => ss.GetComponent<ScaledSpaceFader>() != null)
                .ToList();
        }

        //--------------------------------------------------------------------
        // RemoveVesselFlare
        // Removes a flare (either because a vessel was destroyed, or it's no
        // longer supposed to be part of the draw list).
        private void RemoveVesselFlare(Vessel v)
        {
            if(vesselFlares.ContainsKey(v))
            {
                GameObject flareMesh = vesselFlares[v].flareMesh;
                DestroyObject(flareMesh);

                vesselFlares.Remove(v);
            }
        }

        //--------------------------------------------------------------------
        // Update
        // Update flare positions and visibility
        private void Update()
        {
            if (DistantObjectSettings.DistantFlare.flaresEnabled)
            {
                camPos = FlightCamera.fetch.mainCamera.transform.position;

                if (!ExternalControl)
                {
                    camFOV = FlightCamera.fetch.mainCamera.fieldOfView;
                }

                foreach (BodyFlare flare in bodyFlares)
                {
                    flare.Update(camPos, camFOV);
                    CheckDraw(flare.bodyMesh, flare.meshRenderer, flare.body.transform.position, flare.body.referenceBody, flare.bodySize, FlareType.Celestial);

                    try
                    {
                        Renderer scaledRenderer = scaledTransforms.Find(x => x.name == flare.body.name).renderer;

                        flare.bodyMesh.SetActive(!(scaledRenderer.enabled && scaledRenderer.isVisible));
                    }
                    catch(Exception e)
                    {
                        flare.bodyMesh.SetActive(true);
                        Debug.LogException(e);
                    }
                }

                UpdateVar();

                GenerateVesselFlares();
                foreach(VesselFlare vesselFlare in vesselFlares.Values)
                {
                    vesselFlare.Update(camPos, camFOV);

                    if (vesselFlare.flareMesh.activeSelf)
                    {
                        CheckDraw(vesselFlare.flareMesh, vesselFlare.meshRenderer, vesselFlare.flareMesh.transform.position, vesselFlare.referenceShip.mainBody, 5, (vesselFlare.referenceShip.vesselType == VesselType.Debris) ? FlareType.Debris : FlareType.Vessel);
                    }
                }

                UpdateNameShown();
            }
        }

        //--------------------------------------------------------------------
        // OnGUI
        // Draws flare names when enabled
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

        //--------------------------------------------------------------------
        // SetFOV
        // Provides an external plugin the opportunity to set the FoV.
        public static void SetFOV(float FOV)
        {
            if (ExternalControl)
            {
                camFOV = FOV;
            }
        }

        //--------------------------------------------------------------------
        // SetExternalFOVControl
        // Used to indicate whether an external plugin wants to control the
        // field of view.
        public static void SetExternalFOVControl(bool Control)
        {
            ExternalControl = Control;
        }
    }
}
