# FrostyPlatformer
Penguin After All.
A side-scrolling platformer with a penguin protagonist.
Private project — published to GitHub as version control and backup.

## Origin
Started as OlcSideScrollingConsoleGame (.NET Framework 4.7.2, PixelEngine).
Extensively refactored (SOLID principles, 500+ unit tests, clean architecture).
Now on .NET 8 with MonoGame (DesktopGL) as the engine, assets loaded at runtime.

## Controls

| Action | Keyboard | Xbox controller |
|--------|----------|-----------------|
| Move   | Arrow keys | D-Pad |
| Jump   | Up / Space | A |
| Run (power) | Z / B | X |
| Confirm | Space / X / S | A |
| Pause / Cancel | Escape / P | Start |

| Shortcut | Action |
|----------|--------|
| F11 | Toggle fullscreen |

The game window is resizable. The 256×224 native resolution is always preserved
with pillarbox/letterbox — no stretching.

## Assets
Sprites, maps, and audio live under `FrostyPlatformer/Resources/Assets/` and are
version-controlled. Runtime-generated files (settings, high scores) stay local.

## Map format
Maps are stored as Tiled JSON (`.json`) in `Resources/Assets/MapData/Tiled/`.
Each map has two layers: `Tiles` (visual) and `Collision` (solid/non-solid).

To regenerate maps from the original format:
```
cd Tools/ConvertMaps
dotnet run
```
