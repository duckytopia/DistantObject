v1.5.2 (February 15, 2015)
- Fixed flares rendering when their world is rendered (eg, Minmus and its flare rendering at the same time).
- Internal reorganization of the flare management code to make it less costly to execute, and easier to change.

---

v1.5.1 (December 21, 2014)
- Removed requirement for blizzy's Toolbar being installed.

---

v1.5.0 (December 21, 2014)
- Disable flare rendering in MapView, since flare positions are completely wrong.
- Add support for stock KSP Toolbar (AppLauncher).

---

v1.4.2 (December 11, 2014)
- Move vessel flares back to camera layer 0 to reduce their displacement as a temporary workaround for Issue #3 (MOARdV)
- Fix flare drawing so it works with the CactEye telescope mod (Raven45)

---

v1.4.1
- Fix skybox max brightness not being read (Issue #1)

----

v1.4 (November 2, 2014) Distant Object Enhancement bis

- 0.25 compatibility
- Fix potential NULL reference exceptions when vessels are destroyed fixed (Anatid)
- Ability to show labels on flares (planets and visible vessels) (Anatid)
- Moved flares to a different rendering layer so they're not affected by lighting illuminating the vessel, such as the PlanetShine mod (Valerian)
- Tweaked GUI (MOARdV)
- Enabled GUI in Flight in addition to Space Center view (some changes still going back to the Space Center before they take effect) (MOARdV)
- Improved handling of missing config file or missing config file entries (MOARdV)


----

v1.3.1 (July 29, 2014)

Patch by MOARdV

- 0.24 compatibility
- Two null reference exceptions fixed
- Removed System.Threading.Tasks

----

v1.3 (March 3, 2014)

- Dynamic skybox fading
- Added settings GUI
- Vessel rendering overall should be stable now
- Vessel rendering now creates a database of part models and draws from there, instead of cloning the part reference object
- Vessel rendering no longer attempts to draw incompatible parts in many cases
- Probably some other minor things

----

v1.2 (February 18, 2014)

- Planet color definitions added for Real Solar System
- Planet color definitions added for Real Solar System (metaphor's reconfiguration)
- Planet color definitions added for PlanetFactory default planets
- Planet color definitions added for Alternis Kerbol
- Fixed issue with plugin trying to render launch clamps at large distances and causing ships to explode
- Fixed issue with plugin incorrectly loading custom planet color definitions
- Added some more information to print to the console for easier debugging
- Added setting to easily toggle vessel rendering
- Vessel rendering is now disabled by default

----

v1.1 (February 17, 2014)

- Fixed issue with plugin trying to render flags and EVA Kerbals

----

v1.0 (February 16, 2014)

- Initial Release
