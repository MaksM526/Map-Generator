# Map Generator

Unity scripts for edit-mode procedural world-map generation aimed at survival games.

## Features

- `ProceduralWorldMapGenerator` runs with `[ExecuteAlways]` and refreshes previews from `OnValidate`.
- Continental masks combine Voronoi cellular noise with fractal coherent noise.
- Terrain uses ridged multifractal mountains, FBM hills, latitude-based temperature, moisture with coastal and river influence, downhill river tracing, and biome classification.
- `CubeSphereWorld` builds a subdivided cube and projects each face to a sphere for spherical world previews.

## Usage

1. Copy the `Assets/Scripts/ProceduralWorld` folder into a Unity project.
2. In Unity, create a shared procedural-world settings asset from **Assets > Create > Map Generator > Procedural World Settings**.
3. Add `ProceduralWorldMapGenerator` to a plane or quad with a material to preview generated map textures in edit mode.
4. Assign the settings asset to the generator's `Settings` field. If no asset is assigned, the generator uses its fallback serialized values.
5. Add `CubeSphereWorld` to an empty GameObject with a mesh renderer to generate a spherical world mesh.
6. Change values in the settings asset or fallback Inspector fields; generation refreshes automatically when `Auto Regenerate` is enabled.
