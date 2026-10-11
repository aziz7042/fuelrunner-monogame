# FuelRunner

A compact 3D endless runner built with **C#**, **.NET 9** and **MonoGame DesktopGL**. The player drives a car along a three-lane road, avoids obstacles and collects fuel while the speed steadily increases.

## Features

- Smooth three-lane movement
- Procedural obstacle and fuel-canister spawning
- Continuously draining fuel resource
- Increasing game speed and score over time
- Bounding-box collision detection
- 3D models and simple primitive-based world rendering
- Fuel bar, low-fuel warning, score and game-over UI
- Keyboard and mouse start/restart flow

## Controls

- `Left` / `Right` — change lane
- `Enter` or the on-screen button — start/restart

## Build and run

```bash
dotnet restore
dotnet build
dotnet run
```

Current build status: **0 warnings and 0 errors**.

## Design notes

The core loop stays in one small codebase. Speed progression, resource pressure and collision risk reinforce one another: staying alive increases the difficulty while fuel pickups force lane decisions.

## 3D model credits

The two 3D models in `Content/`—`player_vehicle.fbx` and `fuel_canister.fbx`—were designed and modelled by **Aziz Zarrouki** specifically for FuelRunner. They are project assets, not downloaded stock models.

## Included files

The repository includes the source and assets required for the current build; generated output is ignored. The project-specific 3D models are not offered as reusable stock assets.
