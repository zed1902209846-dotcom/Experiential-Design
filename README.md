# AR Solar System

An Android augmented reality learning app built with Unity and Vuforia. Scan physical planet cards to explore interactive 3D planets, read short profiles, and compare gravity through a shared jumping experiment. The project is being developed for hands-on learning in a museum setting.

## Requirements

| Component | Version |
| --- | --- |
| Unity | 6.3 LTS, `6000.3.23f1` |
| Render pipeline | Universal Render Pipeline 17.3.0 |
| AR SDK | Vuforia Engine 11.4.4, installed from the local `.tgz` package |
| Input | Input System 1.20 |
| Build target | Android |

Install Git LFS before cloning so that binary assets and the Vuforia package download correctly:

```sh
git lfs install
git clone https://github.com/zed1902209846-dotcom/Experiential-Design.git
cd Experiential-Design
git lfs pull
```

Open the cloned folder in Unity `6000.3.23f1` and allow package resolution and asset import to finish. Android builds require the Android Build Support module, SDK, NDK, and OpenJDK supplied through Unity Hub.

## Run the app

1. Open `Assets/Scenes/App/MainMenuScene.unity`, the first scene in Build Settings.
2. Tap the scan icon near the bottom of the home screen to enter `ScanScene`. No account or sign-in is required.
3. Point the camera at a supported physical planet card.
4. Scan a second planet card to activate the Gravity Lab.

The home screen uses the text-free illustration in `Assets/UI/HomeArtwork.png`. The artwork retains its aspect ratio, and the scan control adapts to the screen's safe area.

AR recognition requires a compatible Android device or a configured webcam in Unity Play Mode. Editor tracking simulations can exercise the interaction logic, but they do not replace testing physical cards on a phone.

## Planet interactions

`ScanScene` supports up to five different tracked cards at once. Each planet appears and disappears independently as its card is found or lost.

| Action | Result |
| --- | --- |
| Tap a planet or its name | Open its English information panel |
| Drag a planet with one finger | Rotate it; automatic rotation pauses during the drag |
| Release the drag | Resume automatic rotation |
| Pinch with two fingers | Resize the visible planet nearest the gesture center |
| Scroll an information panel | Read the rest of its profile |
| Tap a panel's close icon | Close that panel independently |

Dragging does not open the information panel. After a pinch, lift and press again to start a new rotation gesture.

### Readable information panels

The eight planets, Sun, and Solar System each have an Art-generated panel skin, stored in `Assets/Resources/UI/PlanetInfoPanels.png`. Titles and descriptions remain native Unity text so they can be laid out and scrolled independently of the artwork.

Panels remain front-facing in screen space. Their layout avoids visible planets, jump trajectories, other panels, and the bottom interaction hint. A single panel is capped at 320 × 400 Canvas units; multiple panels are capped at 340 units in height. When space is limited, panels become smaller or temporarily collapse to a name entry. They return when space becomes available, preserving their content and scroll position. Tapping a collapsed panel's name prioritizes it for reading.

Names, panel connector lines, and gravity figures share a stable center supplied by `ScanPlanetContent`. Imported visual geometry is centered before rotation, and a rotation-independent envelope controls placement. Rotating a planet or Saturn's rings therefore does not move the UI anchor. Moving a card and resizing a planet still update the anchor and its reserved space.

## Gravity Lab: guess, then jump

When two planet cards are tracked, a stick figure stands on each selected planet.

1. **Guess:** choose a planet name, or **=** for the same height.
2. **Jump:** tap **JUMP** to launch both figures simultaneously.
3. **Reveal:** after both figures land, see whether the prediction was correct, along with the peak heights and comparison.
4. **Repeat:** tap **AGAIN** to make a fresh prediction.

The interface uses short prompts and symbols. Heights and answers stay hidden before the reveal. Predictions and planet switching are locked during a jump. Changing the compared pair or losing a selected card resets the round; repeated tracking notifications for the same pair preserve it.

When three or more planet cards are tracked, use the arrows above the choices to change the compared pair. The layout reserves space for the planets and their full jump trajectories. A compact layout keeps all three prediction choices and the jump/retry control available when space is limited. The full panel is up to 280 Canvas units high; compact mode is 160 units high.

### Physics model

Both figures use the same initial upward speed, approximately **3.13 m/s**, with constant gravity and no air resistance:

```text
height at time t = max(0, v*t - 0.5*g*t²)
peak height      = v² / (2*g)
total flight time = 2*v / g
```

Playback runs at **0.45× speed** to make the difference easier to observe. Both jumpers use a shared visual height scale; the displayed planet sizes are not proportional to their real diameters.

For example, this launch produces a peak height of approximately **0.50 m on Earth** and **1.32 m on Mars**. Lower gravity produces a higher jump and a longer flight at the same launch speed.

Gravity values come from the [NASA Planetary Fact Sheet](https://nssdc.gsfc.nasa.gov/planetary/factsheet/index.html?level=1). They use equatorial gravity including rotation, with a 1-bar atmospheric reference level for giant planets; see the [NASA fact sheet notes](https://nssdc.gsfc.nasa.gov/planetary/factsheet/planetfact_notes.html).

Jupiter, Saturn, Uranus, and Neptune have no solid ground to stand on. Their jump comparisons are hypothetical demonstrations at the stated reference level, which the interface notes. The Sun and Solar System overview cards do not participate in the planet gravity comparison. This idealized experiment illustrates gravity rather than predicting a person's real jumping ability.

## Android build

Switch the active platform to Android in Unity's Build Profiles and confirm that the required scenes are enabled. Use Unity's Build / Build and Run workflow, or the project menu:

```text
Solar System > Setup > Step6 Build Android APK
```

The build includes two scenes: `MainMenuScene` and `ScanScene`. The shared `ScanScene` handles every card and the multi-card activities.

The individual planet scenes in `Assets/Scenes/App` (Earth, Venus, Mercury, Mars, Jupiter, Saturn, Uranus, Neptune, Sun, and Solar System; the Earth scene's filename is `Eearth.unity`) are kept in the project but are not part of the build, because no screen in the app navigates to them. Add them back in Build Profiles together with a menu entry if they are needed.

`ScanScene` keeps the screen awake while it is open and restores the device's sleep setting when the visitor leaves it.

## Project layout

```text
Assets/
  Scenes/App/          Home, scanning, and individual planet scenes
  Scripts/AR/          Tracking, gestures, annotations, layout, and Gravity Lab
  Scripts/App/         Scene navigation
  Scripts/Data/        Planet descriptions, statistics, and gravity data
  Scripts/UI/          Home screen and other UI controllers
  Scripts/Editor/      Scene generation, repair, and Android build tools
  Tests/EditMode/      EditMode tests for the gameplay and layout logic
  Data/                Celestial-body ScriptableObject assets
  UI/                  Home artwork and UI textures
  Resources/           Runtime panel atlas, figure material, and Vuforia configuration
  StreamingAssets/     Vuforia tracking databases (.dat / .xml)
  Settings/            URP and quality settings
  Prefabs/             Reusable models, including Saturn's rings
  Inguz Media Studio/The Amazing Solar System/
                       Third-party planet meshes, textures, and materials
```

Main interaction entry points:

- `ScanController` routes tracking updates and activates the activities.
- `ScanPlanetContent` creates and centers models and provides stable display bounds.
- `PlanetAnnotations` and `PlanetPanelLayout` place readable names and profiles.
- `PlanetTapTarget` and `PlanetPinchScale` handle rotation, taps, and resizing.
- `GravityJumpComparison` coordinates the figures, prediction UI, and animation.
- `GravityGuessRound` manages the guess/jump/reveal state.
- `PlanetGravity` contains the gravity values and jump equations.

## Validation and limitations

EditMode tests in `Assets/Tests/EditMode` cover the gravity values and jump equations, the guess/jump/reveal state machine (including ties and replay), target-name lookup, and panel and label placement. Run them from **Window > General > Test Runner > EditMode**.

Tracking-loss resets, pair selection, stable anchors during rotation, and home-screen navigation are not covered by automated tests. Final museum deployment still requires device testing for camera tracking, printed-card quality, touch usability, screen sizes, and performance under exhibition lighting.

## Repository notes

- Large binary packages and assets such as `.tgz`, `.fbx`, and `.psd` use Git LFS.
- Unity-generated folders, build output, debug symbols, and local build backups are excluded from version control and regenerated as needed.
- `Assets/Resources/VuforiaConfiguration.asset` contains a client-visible Vuforia license key. Use your own license for deployment and review its exposure and usage limits before distributing the project.
- The project references [MCP for Unity](https://github.com/CoplayDev/unity-mcp) through `Packages/manifest.json` for editor tooling. It is optional for app development and is not required to run the built Android app.
- Third-party assets retain their original ownership and licensing requirements.
