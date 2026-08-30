# Architecture

Tomk Engine should stay modular so each system can grow separately.

## Runtime Flow

1. `EngineHost` starts.
2. Project settings and scene files load.
3. Systems initialize: renderer, input, physics, animation, scripting.
4. Main loop updates systems every frame.
5. Editor uses the same engine core but adds UI panels and viewport tools.

## Module Roles

- `engine/core`: entity/component model, scene state, main loop.
- `engine/renderer`: graphics API wrapper, render graph, viewport rendering.
- `engine/physics`: collisions, raycasts, rigid bodies, character movement.
- `engine/animation`: rigs, bones, clips, retargeting.
- `engine/fps`: ready-made FPS gameplay package.
- `editor`: visual tools built on top of engine modules.
- `scripting`: parser, compiler/interpreter, and bindings.

## Suggested Tech Path

- Phase 1: C# prototype for engine loop and editor concept.
- Phase 2: Add a graphics layer through OpenTK, Silk.NET, or another rendering library.
- Phase 3: Add Tomk Script parser and bind it to engine components.
- Phase 4: Build asset importer for GLB/GLTF/FBX-style workflows.
