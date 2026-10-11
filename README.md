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

Verified locally: **build succeeded with 0 warnings and 0 errors**.

## Design notes

The project deliberately keeps its core loop in one small codebase. Speed progression, resource pressure and collision risk reinforce one another: staying alive increases the difficulty while fuel pickups force lane decisions.

## Asset provenance

The two 3D models in `Content/`—`player_vehicle.fbx` and `fuel_canister.fbx`—were designed and modelled by **Aziz Zarrouki** specifically for FuelRunner. They are project assets, not downloaded stock models.

## Repository scope

Source code and the assets required for the current build are included. Generated build output is excluded. No blanket license is granted for the project-specific 3D models.
