v1.4 (November 2, 2014) Distant Object Enhancement bis

- 0.25 compatibility
- Potential NULL reference exceptions when vessels are destroyed fixed (Anatid)
- Ability to show labels on flares (planets and visible vessels) (Anatid)
- Moved flares to a different rendering layer so they're not affected by lighting illuminating the vessel, such as the PlanetShine mod (Valerian)
- Changed GUI (MOARdV)
- Enabled GUI in Flight in addition to Space Center view (MOARdV)

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
