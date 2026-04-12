# Engine/Raylib — Motoradapter för Raylib

Denna mapp innehåller alla Raylib-specifika implementationer av spelmotorns interfaces.
Det är den **enda** platsen i projektet där `Raylib_cs` importeras.

---

## Filer

| Fil | Interface | Ansvar |
|-----|-----------|--------|
| `RaylibRenderContext.cs` | `IRenderContext` | Rendering via Raylib (texturer, geometri, text) |
| `RaylibInputProvider.cs` | `IInputProvider` | Tangentbord + Xbox-gamepad via Raylib |
| `RaylibAudioSystem.cs`   | `IAudioSystem`  | WAV-uppspelning via Raylib |

---

## Hur Raylib-cs fungerar

### NuGet-paketet
```
Raylib-cs 7.0.2  →  wrapprar Raylib C-biblioteket 5.5 (native DLL)
```
Paketet innehåller förbyggda native-bibliotek för Windows, Linux och macOS.
Inga extra installationer krävs — NuGet kopierar rätt `.dll`/`.so`/`.dylib` automatiskt.

### Spelloop
```csharp
Raylib.InitWindow(width, height, title);
Raylib.InitAudioDevice();
Raylib.SetTargetFPS(60);              // -1 = okappat (GameConstants.FrameRate)

while (!Raylib.WindowShouldClose())
{
    float elapsed = Raylib.GetFrameTime();
    Raylib.BeginDrawing();
    // — all rendering och spellogik här —
    Raylib.EndDrawing();
}

Raylib.CloseAudioDevice();
Raylib.CloseWindow();
```

### Strängparametrar (unsafe)
Raylib-cs P/Invoke-bindningarna tar `sbyte*` för strängar. Råa namngivna P/Invoke-metoder
exponeras med den unsafe-signaturen. Wrappar det med:
```csharp
unsafe Sound LoadSoundFromPath(string path)
{
    byte[] bytes = Encoding.UTF8.GetBytes(path + '\0');
    fixed (byte* ptr = bytes)
        return Raylib.LoadSound((sbyte*)ptr);
}
```
`AllowUnsafeBlocks = true` är satt i `FrostyPlatformer.csproj`.

### Koordinatsystem och skalning
Spelet renderas i ett logiskt utrymme på **256×224 pixlar** (`GameConstants.ScreenWidth/Height`).
Varje spelpixel mappas till **4×4 faktiska pixlar** (`GameConstants.PixelWidth/Height`).
Fönstret är alltså **1024×896 pixlar**.

`RaylibRenderContext` skalas alla koordinater internt:
```
screenX * scaleX,  screenY * scaleY
```
Spelkoden arbetar alltid i spel-koordinater (0–255, 0–223).

### Texturer
```csharp
// Ladda:
Texture2D tex = Raylib.LoadTexture((sbyte*)ptr);

// Rita urklipp ur sprite-ark (DrawPartialSprite):
Raylib.DrawTexturePro(tex,
    source: new Rectangle(srcX, srcY, width, height),   // texturkoordinater
    dest:   new Rectangle(screenX*4, screenY*4, w*4, h*4), // fönsterkoordinater
    origin: Vector2.Zero,
    rotation: 0f,
    tint: Color.White);

// Avregistrera vid avslut:
Raylib.UnloadTexture(tex);
```

### Ljud
```csharp
Raylib.InitAudioDevice();          // måste anropas före LoadSound
Sound s = Raylib.LoadSound(ptr);   // laddar WAV från disk
Raylib.PlaySound(s);
Raylib.StopSound(s);
Raylib.IsSoundPlaying(s);          // bool
Raylib.UnloadSound(s);             // frigör GPU/CPU-minne
Raylib.CloseAudioDevice();         // stänger ljudenheten
```

### Input
```csharp
// Tangentbord:
Raylib.IsKeyDown(KeyboardKey.Right)     // hålls nere
Raylib.IsKeyPressed(KeyboardKey.Space)  // tryckt ned denna frame
Raylib.IsKeyReleased(KeyboardKey.Up)    // släppt denna frame
Raylib.GetKeyPressed()                  // returnerar 0 om ingen tangent tryckts

// Gamepad (Xbox-layout, port 0):
Raylib.IsGamepadAvailable(0)
Raylib.IsGamepadButtonDown(0, GamepadButton.RightFaceDown)    // A
Raylib.IsGamepadButtonPressed(0, GamepadButton.RightFaceRight) // B (pressed)

// Gamepad-knappmappning (Xbox → Raylib):
//   D-Pad Up/Down/Left/Right → GamepadButton.LeftFaceUp/Down/Left/Right
//   A (hoppa/bekräfta)       → GamepadButton.RightFaceDown
//   B (springa)              → GamepadButton.RightFaceRight
//   Select                   → GamepadButton.MiddleLeft
//   Start (paus)             → GamepadButton.MiddleRight

// Fönsterfokus:
Raylib.IsWindowFocused()
```

---

## Motorbyte (framtida MonoGame)

För att byta motor: skapa `Engine/MonoGame/` med tre nya klasser som implementerar
samma tre interfaces. Ändra sedan tre rader i `Program.cs` (Composition Root).
Ingen spellogik behöver röras.
