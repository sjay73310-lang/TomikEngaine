# Tomk Engine

Tomk Engine is a concept base for a 3D game engine with a real-time editor, FPS tooling, rig/animation workflow, and a custom C#-like scripting language called Tomk Script.

GitHub repository name: `TomikEngaine`.

## Goal

- Create and edit 3D scenes inside an editor viewport.
- Import and manage 3D models, rigs, textures, materials, shaders, and scenes.
- Build FPS-style gameplay systems such as player movement, camera, weapons, and input.
- Support animation and rig editing for characters.
- Add Tomk Script as a personal coding language that can call engine APIs, similar to how C# works in game engines.

## Project Map

```text
Tomk Engine/
  engine/                 Core engine modules
    core/                 Engine loop, entities, components, project state
    renderer/             3D rendering pipeline and viewport drawing
    physics/              Collision, rigid bodies, character controller
    input/                Keyboard, mouse, gamepad input
    animation/            Rig, skeleton, animation clips
    fps/                  FPS camera, movement, weapons, interaction
  editor/                 Visual editor
    ui/                   Panels, inspector, hierarchy, asset browser
    viewport/             3D scene viewport controls
    tools/                Move/rotate/scale, terrain, rig tools
  runtime/                Launcher/runtime entry point
  scripting/              Tomk Script language and bindings
  assets/                 Shared engine assets
  projects/sample-fps/    First playable test project
  tools/                  Importer and builder tools
  docs/                   Planning and architecture notes
```

## First Milestone

1. Open a window with a 3D viewport.
2. Render a cube/model with camera controls.
3. Add scene hierarchy and transform editing.
4. Add FPS controller and input.
5. Load a simple Tomk Script file and connect it to an entity.

## Current Runnable Editor

The first Windows editor app is now available in `editor/Tomk.Editor`.

It includes:

- Scene View with orbit/zoom camera controls.
- Left-click object selection inside the 3D viewport.
- Camera controls work on blank Scene View space too.
- Right-drag orbit camera controls, middle-drag pan, and mouse-wheel zoom.
- Hold right mouse and use `W/A/S/D/Q/Z` for Scene View fly movement.
- Game View with play/pause preview.
- Hierarchy panel for scene objects.
- Inspector panel for name, type, position, rotation, and scale.
- Transform tools: Select, Move, Rotate, Scale.
- Clickable XYZ transform gizmo lines on the selected object.
- Gizmo axis behavior for Move, Rotate, and Scale, including Y-axis movement.
- Keyboard shortcuts: `W/E/R` for Move/Rotate/Scale, `F` to frame selected, `Delete` to remove, arrow keys to nudge.
- Add cube, sphere, and plane buttons.
- Duplicate/delete selected object.
- File menu with Create New Project, New Object, New Script, Save Scene.
- File menu can create shader and material assets.
- Project folders are created under `projects/<ProjectName>/` with `assets`, `objects`, `scenes`, and `scripts`.
- Asset Browser works like a project file explorer with category folders, search, preview, drag-drop, and right-click actions.
- Asset categories include Models, Materials, Shaders, Textures, Scripts, Scenes, Objects, Audio, and Imports.
- Right-click assets to add supported models/objects to the scene, import model/texture files, create shaders/materials/scripts/object files, reveal in Explorer, or refresh.
- Asset Browser now has an Explorer-style tile grid, preview/details area, category sidebar, search, and zoom slider.
- Asset Browser tree/tiles/preview areas are resizable for small or zoomed-out windows.
- Inspector is organized into Object, Components, Render, and Lighting tabs.
- Components tab supports Add Component, Remove Component, and Attach Script.
- Play mode no longer rotates every object automatically; only objects with the `Preview Spin` component rotate during preview.
- Camera tab supports Camera enabled state, Field Of View, Near Clip, Far Clip, Aspect Ratio, and frustum visibility.
- Selecting a camera shows a yellow camera frustum in the Scene View so you can see what the camera renders.
- Game View now uses the selected hierarchy camera object's rotation, FOV, near clip, and far clip.
- New scene objects get smarter default components based on object type, such as Camera, Point Light, Directional Light, Mesh Renderer, Collider, and Sky Settings.
- Render tab shows material/shader and Mesh Renderer enabled state for the selected object.
- Lighting tab has Enable Scene Lighting, Enable Sky, Classic Skybox, and Volumetric Sky settings.
- Numeric transform fields can be adjusted by dragging left/right; hold Shift while dragging for faster changes.
- Window layout supports resizable panels with splitters.
- Hierarchy, Inspector, Asset Browser, Console, and Tomk Script panels can float in separate movable/resizable windows.
- Window menu can save a personal layout, load it again, or reset the editor layout.
- Visual theme has a darker editor palette for menus, asset browser, controls, and floating panels.
- Selected tabs, combo boxes, menus, lists, and popups use dark editor styling instead of default white WPF styling.
- Asset Browser has a resizable tree and preview/details area.
- Gizmo can be toggled on/off from the top toolbar.
- Active gizmo axis highlights yellow while dragging.
- Rotate tool shows XYZ ring gizmos instead of straight move handles.
- Project shader files are saved under `projects/<ProjectName>/assets/shaders`.
- Project material files are saved under `projects/<ProjectName>/assets/materials`.
- Materials can choose a shader and can be assigned to selected objects.
- Default shaders include `DefaultLit`, `UnlitColor`, and `VolumetricSkyCloud`.
- Hierarchy includes camera, directional light, point light, and game settings objects.
- Scene lighting uses hierarchy light objects.
- Sky supports `Classic Skybox` and `Volumetric Sky` modes.
- Scene View supports drag-and-drop import for `.glb`, `.gltf`, `.fbx`, `.obj`, and `.tomkobj` files.
- Asset, console, and Tomk Script panels.
- Save Scene action that writes `projects/<ProjectName>/scenes/editor.scene.tomk`.

## Run

```powershell
dotnet run --project "editor/Tomk.Editor/Tomk.Editor.csproj"
```

## Build EXE

```powershell
dotnet publish "editor/Tomk.Editor/Tomk.Editor.csproj" -c Release -r win-x64 --self-contained false -o "build/TomkEngineEditor"
```

The generated editor executable is:

```text
build/TomkEngineEditor/Tomk.Editor.exe
```

## Example Tomk Script

```tomk
class PlayerController : Component {
    speed: float = 7.5;

    fn update(delta: float) {
        let move = Input.axis("Horizontal", "Vertical");
        entity.move(move * speed * delta);
    }
}
```
