# MonoGame API — Implementationsguide

*Skapad: 2026-04-13 | Gäller: FrostyPlatformer (Fas 2)*

Den här guiden beskriver hur MonoGame används i FrostyPlatformer och hur
de tre adapter-klasserna (`MonoGameRenderContext`, `MonoGameInputProvider`,
`MonoGameAudioSystem`) mappas mot spelets egna interface.

---

## Innehåll

1. [Spelloop och Game-klassen](#1-spelloop-och-game-klassen)
2. [Koordinatsystem och skalning](#2-koordinatsystem-och-skalning)
3. [Rendering — SpriteBatch och RenderTarget](#3-rendering--spritebatch-och-rendertarget)
4. [Texturladdning](#4-texturladdning)
5. [Input — tangentbord och gamepad](#5-input--tangentbord-och-gamepad)
6. [Ljud — SoundEffect och SoundEffectInstance](#6-ljud--soundeffect-och-soundeffectinstance)
7. [Byta motor i framtiden](#7-byta-motor-i-framtiden)

---

## 1. Spelloop och Game-klassen

MonoGame är ett spelramverk, inte en fullfjädrad motor. Startpunkten är att
ärva från `Microsoft.Xna.Framework.Game` och overrida dessa metoder:

| Metod | Anropas | Vad som görs i FrostyPlatformer |
|-------|---------|----------------------------------|
| `Initialize()` | En gång, innan LoadContent | Skapar spelsystem som inte behöver GPU (input, kamera, dialog, världskarta, sparning) |
| `LoadContent()` | En gång, efter Initialize | Skapar SpriteBatch, RenderTarget, RenderContext, AudioSystem; registrerar sprites och ljud; sätter SplashState |
| `Update(GameTime)` | 60×/sekund | Spårar förfluten tid; hanterar F11 helskärm |
| `Draw(GameTime)` | 60×/sekund | Kör spellogik + rendering (tvåstegs — se avsnitt 3) |
| `UnloadContent()` | Vid programavslut | Frigar texturer, ljud och render target |

`static void Main()` instansierar `Program` och anropar `Run()` — MonoGames
inbyggda metod som startar spelloop, hanterar fönsterlivstid och kallar metoderna ovan.

```csharp
static void Main()
{
    using var game = new Program();
    game.Run();
}
```

**Timing:** `IsFixedTimeStep = true` och `TargetElapsedTime = 1/60s` ger en
stabil 60 fps-loop med MonoGames inbyggda catch-up-logik. Förfluten tid per
frame läses via `gameTime.ElapsedGameTime.TotalSeconds`.

---

## 2. Koordinatsystem och skalning

Spellogiken opererar i **spelpixelkoordinater** — ett fast 256×224-rutnät
oberoende av fönsterstorlek.

```
Spelpixlar (256×224)
  └─ RenderTarget2D (256×224, scale 1×1)
       └─ Skalas upp till fönstret i Draw pass 2
```

`MonoGameRenderContext` skapas med `scaleX: 1, scaleY: 1` — alla ritanrop
arbetar i spelpixelkoordinater direkt mot render target:

```
DrawPartialSprite(sheet, screenX=128, screenY=112, ...)
  → SpriteBatch.Draw vid pixel (128, 112) i render target
```

**PointClamp** används i `SpriteBatch.Begin()` för att bevara pixelart-skärpan
vid uppskala — linjär interpolation (Bilinear/Anisotropic) ger suddiga kanter.

`CameraSystem.Calculate()` tar `ScreenWidth=256` och `ScreenHeight=224` och
returnerar tile-offsettar i spelpixelkoordinater. `TileSize=16` ger
`256/16 = 16` synliga tiles i bredd och `224/16 = 14` i höjd.

---

## 3. Rendering — SpriteBatch och RenderTarget

Rendering sker i **två pass** per frame i `Program.Draw()`:

### Pass 1 — Spel → RenderTarget (256×224)

```csharp
GraphicsDevice.SetRenderTarget(_renderTarget);   // rita till offscreen buffer
GraphicsDevice.Clear(Color.Black);
_spriteBatch.Begin(SpriteSortMode.Deferred,
                   BlendState.AlphaBlend,
                   SamplerState.PointClamp, ...);
_stateManager.Update(_context, _elapsed);        // spellogik + rendering
_spriteBatch.End();
```

`SpriteSortMode.Deferred` + `BlendState.AlphaBlend` är standardkombinationen
för 2D-sprites med transparens. Sprites dras i den ordning de skickas in.

### Pass 2 — RenderTarget → Skärm (skalat)

```csharp
GraphicsDevice.SetRenderTarget(null);            // rita till backbuffer
GraphicsDevice.Clear(Color.Black);               // svart pillarbox/letterbox
_spriteBatch.Begin(SpriteSortMode.Deferred,
                   BlendState.Opaque,
                   SamplerState.PointClamp, ...);
_spriteBatch.Draw(_renderTarget, destRect, Color.White);
_spriteBatch.End();
```

`destRect` beräknas av `CalculateDestRect()` i `Program.cs` och bevarar
256:224-aspektkvoten. Svarta kanter (pillarbox/letterbox) visas om fönstrets
aspekt inte matchar.

> **Stretch till full bredd** (utan aspektbevarelse): ersätt `CalculateDestRect()`
> med `new Rectangle(0, 0, Viewport.Width, Viewport.Height)`. Se kommentaren
> i metoden för exakt kodfragment.

**Viktig regel:** `GraphicsDevice.Clear()` måste anropas *före* `SpriteBatch.Begin()`.
`MonoGameRenderContext.Clear()` är därför ett no-op — rensningen sker i `Program.Draw()`.

---

## 4. Texturladdning

Texturer laddas runtime från disk — ingen MonoGame Content Pipeline används.
Det är ett medvetet val för att stödja user-generated content och moddning.

```csharp
// Registrering vid LoadContent (Program.cs → RegisterSprites)
_renderContext.RegisterSprite(SpriteId.Hero, path + "\\hero.png");

// Internt i MonoGameRenderContext
public void RegisterSprite(SpriteId id, string filePath)
    => _textures[id] = Texture2D.FromFile(_gd, filePath);
```

`_textures` är en `Dictionary<SpriteId, Texture2D>` som lever för spelets
hela livstid. `UnloadAll()` anropar `Dispose()` på varje textur vid avslut.

**Kartbyte** kräver att tile-spritesheet laddas om — `MapRepository.Load()`
anropar `_rc.RegisterSprite(SpriteId.MapTileSheet, newPath)` vilket ersätter
den befintliga texturen i dictionary.

---

## 5. Input — tangentbord och gamepad

MonoGame saknar ett inbyggt "just tryckt"-API. `MonoGameInputProvider`
håller föregående och nuvarande `KeyboardState` och `GamePadState` för att
beräkna pressed (nedtryckt nu men inte förra framen):

```csharp
// Snapshot i Poll() — anropas en gång per frame (i varje IGameState.Update)
_previous = _current;
_current  = Keyboard.GetState();
_prevPad  = _pad;
_pad      = GamePad.GetState(PlayerIndex.One);

// Pressed-logik
bool KeyPressed(Keys key)
    => _current.IsKeyDown(key) && _previous.IsKeyUp(key);
```

`GamePad.GetState(PlayerIndex.One)` returnerar `GamePadState.IsConnected = false`
om ingen kontroller är ansluten — alla `PadDown`/`PadPressed`-anrop returnerar
`false` utan kraschar.

### Knappkartläggning (Xbox-layout)

| Spelaction | Tangentbord | Xbox-kontroller |
|-----------|-------------|-----------------|
| Rörelse | Piltangenter | D-Pad |
| Hoppa | Upp / Space | A |
| Springa | Z / B | X |
| Bekräfta | Space / X / S | A |
| Paus/Avbryt | Escape / P | Start |

D-Pad Upp är *inte* mappat till hopp — det är avsiktligt för att undvika
att hoppa vid navigering i menyer och på världskartan.

### F11 helskärm

F11 hanteras direkt i `Program.Update()` utanför `IInputProvider`, eftersom
det är ett fönsterhanteringsbeslut snarare än spellogik:

```csharp
var kb = Keyboard.GetState();
if (kb.IsKeyDown(Keys.F11) && _prevKeyboard.IsKeyUp(Keys.F11))
    _graphics.ToggleFullScreen();
_prevKeyboard = kb;
```

---

## 6. Ljud — SoundEffect och SoundEffectInstance

`MonoGameAudioSystem` laddar WAV-filer med `SoundEffect.FromStream()` och
skapar en `SoundEffectInstance` per ljud. Instansen hålls vid liv i en
dictionary för hela spelets livstid.

```csharp
using var stream = File.OpenRead(filePath);
var effect   = SoundEffect.FromStream(stream);
var instance = effect.CreateInstance();
instance.IsLooped = isLooped;   // true för bakgrundsmusik
```

**Varför hålla SoundEffect vid liv?** `SoundEffectInstance` är bunden till
sin förälder-`SoundEffect`. Om `SoundEffect` töms av GC slutar instansen
fungera. Därför lagras båda i separata dictionaries.

### Loopning

Bakgrundsmusik registreras med `isLooped: true`:

```csharp
RegisterSound(SoundRef.BGSoundGame, path, isLooped: true);
```

`SoundEffectInstance.IsLooped = true` loopas automatiskt av MonoGame.
States behöver bara anropa `Play()` en gång — ingen "kolla IsPlaying och
spela igen"-logik krävs (till skillnad från Raylib-implementationen).

### Mute/UnMute

```csharp
public void Mute()   => SoundEffect.MasterVolume = 0f;
public void UnMute() => SoundEffect.MasterVolume = 1f;
```

`SoundEffect.MasterVolume` är statisk och global — alla instanser tystas
omedelbart. Instanserna stannar på sin position och återupptas vid UnMute
utan att states behöver trigga dem på nytt.

### WAV-format

MonoGame kräver PCM WAV (8 eller 16 bit, standard samplingshastigheter).
ADPCM-komprimerade WAV-filer orsakar `InvalidOperationException` vid laddning.
Konvertera med Audacity (Export → WAV → PCM 16-bit) vid behov.

---

## 7. Byta motor i framtiden

Arkitekturen är förberedd för ett eventuellt framtida motorbyte. Alla
motor-specifika anrop är isolerade i de tre adapterklasserna i denna mapp:

| Interface | MonoGame-implementation | Byt mot |
|-----------|------------------------|---------|
| `IRenderContext` | `MonoGameRenderContext` | Ny `XyzRenderContext` |
| `IInputProvider` | `MonoGameInputProvider` | Ny `XyzInputProvider` |
| `IAudioSystem` | `MonoGameAudioSystem` | Ny `XyzAudioSystem` |

Composition Root (`Program.cs`) kopplar samman implementationerna — att byta
motor är bokstavligen att instansiera tre andra klasser och uppdatera
`LoadContent()`. All spellogik, alla states och alla system förblir oförändrade.

**Render target-mönstret** (`RenderTarget2D` i pass 1) är MonoGame-specifikt
och behöver sin motsvarighet i det nya motorlagret. Principen — rendera
spelet till en offscreen-buffer i nativ upplösning och stretcha till skärmen
— är dock motor-agnostisk och ska behållas.
