# FrostyPlatformer — Roadmap
*Skapad: 2026-04-12 | Status: Aktiv*

---

## Bakgrund och vision

FrostyPlatformer är en direkt fortsättning på OlcSideScrollingConsoleGame, ett side-scrolling platform-spel med en pingvin som protagonist. Projektet startar från den välrefaktorerade kodbasen som OlcSideScrollingConsoleGame lämnar efter sig — med en ren Composition Root, 289 gröna enhetstester och alla spelkoncerner isolerade bakom väldefinierade interfaces.

**Varför ett nytt repo?**
- Bryta den tekniska skulden kring PixelEngine (C#-port, övergiven ~2019, 32-bit-låst)
- Uppgradera till modern .NET (mål: .NET 8)
- Byta spelmotor i kontrollerade steg
- Friare namnrymd — "Olc" och "Console" är inte längre beskrivande

**Startläge — vad vi tar med:**
Klon av OlcSideScrollingConsoleGame efter Fas 4b (2026-04-11). Inga kodytor av det gamla motorlagret följer med aktivt — de ersätts i Fas 1.

---

## Strategiska beslut

| Beslut | Val | Motivering |
|--------|-----|------------|
| Repo-namn | `FrostyPlatformer` | Beskriver spelet, neutral mot motor och ramverk |
| Solution-namn | `FrostyPlatformer.sln` | Matchar repo-namnet |
| Projektnamn | `FrostyPlatformer` | Ersätter `OlcSideScrollingConsoleGame` |
| .NET-version | .NET 8 | Modern LTS-version; bryter x86-låsningen |
| Första motor | Raylib (via `Raylib-cs`) | Snabb uppstart, välunderhållen C#-binding, god community |
| Slutmål motor | MonoGame | Fullständigt C#-ekosystem, XNA-kompatibelt, bred plattformssupport |
| Migreringsordning | RayLib → MonoGame (sekventiellt) | RayLib ger snabb "up and running"; MonoGame planeras ordentligt i sin egen fas |

---

## Repo-transition — engångssteg

Dessa steg utförs en gång för att etablera det nya repot. De är inte en del av det löpande arbetet.

- [ ] Skapa tomt GitHub-repo: `FrostyPlatformer`
- [ ] Klona `OlcSideScrollingConsoleGame` lokalt till en ny mapp
- [ ] Byt remote: `git remote set-url origin <ny-repo-url>`
- [ ] Pusha till det nya repot: `git push -u origin master`
- [ ] Sätt en ny baslinje-tag: `git tag v0-from-olc && git push origin v0-from-olc`
- [ ] Verifiera att historiken är intakt i det nya repot
- [ ] Byt namn på solution och projekt: `OlcSideScrollingConsoleGame` → `FrostyPlatformer`
- [ ] Uppdatera namespaces och assemblyname i `.csproj`
- [ ] Bekräfta att bygget är grönt och alla 289 tester är gröna efter namnbytet

---

## Fas 1 — .NET 8 + Raylib (Mål: spelet körs)

**Strategiskt mål:** Ersätta PixelEngine, SlimDX och Audio.Library med moderna alternativ och uppgradera till .NET 8. Spelet ska vara spelbart i slutet av denna fas — alla befintliga funktioner ska fungera.

**Nyckelinsikt:** Arkitekturen är redan förberedd. `IRenderContext` kapslar in all rendering, `IInputProvider` kapslar in all input, `IAudioSystem` kapslar in allt ljud. Fas 1 handlar om att skriva nya *implementationer* av dessa interfaces — inte om att röra spellogiken.

### 1a — Projektuppsättning och beroenden

- [ ] Uppgradera `.csproj` till `<TargetFramework>net8.0</TargetFramework>`
- [ ] Ta bort NuGet-paket: `SlimDX`, `OpenTKWithOpenAL`
- [ ] Lägg till NuGet-paket: `Raylib-cs` (senaste stabila)
- [ ] Ta bort DLL-referenser: `PixelEngine.dll`, `Audio.Library.dll`, `Gamepad.Library.dll`
- [ ] Ändra `<PlatformTarget>` från `x86` till `AnyCPU`
- [ ] Ta bort `x86.runsettings` — behövs inte längre
- [ ] Verifiera att projektet kompilerar (med förväntade fel för saknade motor-anrop)

### 1b — RaylibRenderContext

Implementera `IRenderContext` med Raylib som backend.

- [ ] Skapa `RaylibRenderContext : IRenderContext`
- [ ] Implementera `DrawSprite` (mappas till `Raylib.DrawTexturePro` eller liknande)
- [ ] Implementera `FillRect` (mappas till `Raylib.DrawRectangle`)
- [ ] Implementera `DrawText` / `DrawBigText`
- [ ] Hantera Raylib-fönsterinitieringen (ersätter `PixelEngine`-arvet i `Program.cs`)
- [ ] Spelloop via `Raylib.WindowShouldClose()` — ersätter `Start()`/`OnUserCreate()`/`OnUserUpdate()`
- [ ] Verifiera att rendering fungerar med ett enkelt testscen (en sprite på skärmen)

### 1c — RaylibInputProvider

Implementera `IInputProvider` med Raylib + gamepad-stöd.

- [ ] Skapa `RaylibInputProvider : IInputProvider`
- [ ] Implementera tangentbords-input (Raylib.IsKeyDown / IsKeyPressed)
- [ ] Implementera gamepad-input (Raylib.IsGamepadButtonDown — ersätter SlimDX)
- [ ] Verifiera mot `InputProviderTests` att kontraktet uppfylls

### 1d — RaylibAudioSystem

Implementera `IAudioSystem` med Raylib.

- [ ] Skapa `RaylibAudioSystem : IAudioSystem`
- [ ] Implementera WAV-laddning och uppspelning via `Raylib.LoadSound` / `Raylib.PlaySound`
- [ ] Implementera `Mute()` / `UnMute()`
- [ ] Verifiera att samtliga 11 WAV-filer laddas korrekt

### 1e — Integrationstest och speltest

- [ ] Bygget är grönt — `1 succeeded, 0 failed`
- [ ] Alla 289 enhetstester är gröna
- [ ] Spelet startar och menysystemet fungerar
- [ ] Gameplay-tester: rörelse, kollision, fiender, boss, poäng, save/load
- [ ] Ljud fungerar, gamepad fungerar
- [ ] Sätt tag: `git tag v1-raylib`

---

## Fas 2 — MonoGame (Planering sker separat)

**Strategiskt mål:** Byta från Raylib till MonoGame som spelmotor.

Denna fas planeras i ett separat dokument när Fas 1 är avslutad. Fas 1 ger erfarenhet av hur väl `IRenderContext`-abstraktionen håller i praktiken — den erfarenheten styr hur Fas 2 utformas.

**Preliminära frågor att besvara i planeringssteget:**
- Hur mappar MonoGames `SpriteBatch` mot `IRenderContext`? Behöver interfacet utökas?
- Hur hanterar MonoGame spelloop och fönsterlivstid — hur påverkar det `Program.cs`?
- Content Pipeline vs. runtime-laddning av assets — vad väljer vi?
- Stöd för gamepad och ljud — MonoGame.Framework.DesktopGL vs. Windows?
- Ska vi behålla Raylib som ett parallellt alternativ (feature flags) under transition?

**Fas 2 påbörjas inte förrän:**
- [ ] Fas 1 är avslutad och taggad (`v1-raylib`)
- [ ] Separata planeringsmötet för MonoGame-fasen är genomfört
- [ ] Ett nytt fasplaneringsdokument (`MONOGAME_PLAN.md`) är skapat

---

## Principer som gäller

Dessa bärs med från OlcSideScrollingConsoleGame och gäller fortsatt:

- `CODING_STANDARDS.md` och `ARCHITECTURE.md` är normerande dokument
- Alla nya system ska ha enhetstester innan integration
- Inga direkta motor-anrop utanför dedikerade adapter-klasser (`*RenderContext`, `*InputProvider`, `*AudioSystem`)
- `Program.cs` förblir en ren Composition Root — inga spelkoncerner läggs tillbaka dit
- Nullable reference types används i all ny kod
