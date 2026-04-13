# FrostyPlatformer
Penguin After All.
A side-scrolling platformer with a penguin protagonist.
Private project — published to GitHub as version control and backup.

## Origin
Started as OlcSideScrollingConsoleGame (.NET Framework 4.7.2, PixelEngine).
Extensively refactored (SOLID principles, 289 unit tests, clean architecture).
This repo is the next chapter: .NET 8 migration and engine replacement.

See `ROADMAP.md` for the migration plan.

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
Sprites, maps, and audio are excluded from version control (.gitignore).
Add them locally to run the game.
