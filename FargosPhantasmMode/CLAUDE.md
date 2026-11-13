# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

FargosPhantasmMode is a tModLoader (1.4.4, net8.0) addon for Fargo's Souls Mod. It adds a new difficulty, Phantasm Mode (境妄模式), layered on top of Masochist/Eternity Mode: it forces Master Mode, keeps all Masochist behavior, makes debuff damage non-lethal, reworks selected bosses, and rebalances Eternity drops, Enchantments, Souls and Forces. The toggle lives in Fargo's Souls' own difficulty-selection UI. The mod is experimental and changes often. English localization is AI-translated; Chinese (`zh-Hans`) is the source language, and most code comments are in Chinese.

## Repository layout

The git root is the parent `ModSources/` directory, not this folder. Three projects build together (`../PhantasmMode.slnx`):

- `FargosPhantasmMode/`: this mod.
- `../Monochrome/`: an in-house library mod (hard `modReferences` dependency, `sortAfter` it). It provides the core runtime (scheduler, tween, timeline, pooling, RNG, logging), networking (`MonoNet`), graphics (render targets, shaders, primitives, post-processing, particles) and the AI state machine (`MonoFsm`). Its docs are authoritative: `../Monochrome/README.md`, `../Monochrome/Core/README.md`, `../Monochrome/Core/Graphics/README.md`, `../Monochrome/docs/构建蓝图.md`.
- `../FargosPhantasmMode.SourceGenerator/`: Roslyn incremental generators (referenced as an analyzer).
- `../Monochrome.CoreTests/`: a plain console project that checks Monochrome's game-independent core (pools, RNG, scheduler, coroutines, timeline, tween).

Other dependencies are DLLs in `lib/` (`FargowiltasSouls.dll`, `Fargowiltas.dll`, `Luminance.dll`), referenced with `Private=false`. Both mods use tml-build 2.1.2 with `TmlVersion=stable`: NuGet imports the cached release's targets. The legacy `../tModLoader.targets` also resolves that cached stable version. Machine-specific .NET host overrides belong in the ignored `../tModLoader.Local.props`; they must not redirect the tML assembly to Steam.

## Commands

After switching tML versions, restore before building and reload the VS projects:

```bash
dotnet restore FargosPhantasmMode.csproj --disable-parallel -m:1
```

```bash
dotnet build FargosPhantasmMode.csproj -p:BuildMod=false -p:TmlBuildPackageMod=false
```

Use `-p:BuildMod=false -p:TmlBuildPackageMod=false` to skip packaging into `Mods/*.tmod` with the tml-build SDK. That step writes outside the repo and fails in a sandbox; compilation itself still succeeds. Drop the flag (or use tModLoader's in-game "Build + Reload") to produce a playable `.tmod`.

```bash
dotnet run --project ../Monochrome.CoreTests
```

This runs the offline Monochrome core checks. There are no tests for this mod itself; gameplay, frame hooks and per-frame allocation can only be verified in-game (Monochrome exposes `/mono core ...` chat commands for self-tests).

## Architecture

### Difficulty gate and boss reworks

`Core/Systems/WorldSavingSystem.cs` (`PModeWorldSavingSystem.PhantasmMode`) is the world-level flag everything checks.

Boss and vanilla-NPC reworks derive from `Global/PModeNPCBehaviour`, a copy of Fargo's `EModeNPCBehaviour` pattern:
- Each subclass sets `NPCType`. `NewInstance` returns null unless Phantasm Mode is on, so the behavior simply does not exist otherwise.
- `PreAI`/`PostAI` are sealed. Override `SafePreAI`, `SafePostAI` and `OnFirstTick` instead. `RunPmodeAI = false` disables the rework.
- To replace Fargo's Eternity AI, `OnFirstTick` sets the Fargo GlobalNPC's `RunEmodeAI = false` (see `Content/Bosses/VanillaEternity/EyeOfCthulhu/EyeOfCthulhu.cs`).
- Extra AI state is synced with Monochrome's `MonoNetFields<NPC>` tables. `WithLocalAI` adds `localAI[0..3]`, but must not be used where the underlying ModNPC already syncs those slots (AbomBoss and CursedSpirit write `[0..3]`, MutantBoss writes `[0..2]`); see the XML comment there.

Reworks of Fargo's own bosses live under `Content/Bosses/{Abom,Mutant,Trojan,Coffin}`; vanilla bosses under `Content/Bosses/VanillaEternity/`. Newer bosses use `MonoFsm<State>` from `Monochrome.Content.Ai` as a state machine split across partial classes (e.g. `Coffin/CoffinStateManagement.cs`: `Fsm.To(from, to, condition, onEnter)` transitions plus phase-change hijacks). Older ones use hand-rolled `AIState` switches. A Luminance `PushdownAutomata` extension also exists in `Common/PhanUtil.cs`, and the `luminance-state-machine` skill covers that API.

### Networking

Client-to-server requests are declared in `Core/Systems/PModeNet.cs` as `MonoNetRequest<T>` (`MonoNet.Request("fpm.xxx", ...)`) and registered from `FargosPhantasmMode.Load()`. The server validates and drops bad requests. Any client action that changes world state must go through this table rather than hand-built packets. Use `FargoSoulsUtil.HostCheck` before spawning NPCs or projectiles.

### Generated ID tables

The source generator emits static classes with lazy `int` properties for every public, non-abstract mod type:
- `PProjID` / `PBuffID`: this mod's projectiles and buffs (e.g. `PProjID.AbomBlast`).
- `FProjID` / `FBuffID`: FargowiltasSouls' projectiles and buffs, scanned from the referenced assembly.

Use these instead of `ModContent.ProjectileType<T>()`. Output is written to `obj/Generated/` for inspection only (`EmitCompilerGeneratedFiles`); those `.cs` files are excluded from compilation, so never edit them.

### Rendering

- Shaders: `.fx`/`.fxc` live in `Assets/Effects/`, built by tml-build 2.1.2. Generated accessors are in `obj/Generated`; never edit generated output. `PModeShaders.Library` explicitly imports own resources and a whitelist of FSS resources; external effects use independent clones. `MonoShaderManager` only contains Monochrome built-ins. No MonoShaderScope, custom shader compiler, or Luminance watcher suppression remains. Missing optional shaders should be skipped.
- `FargosPhantasmMode.cs` hooks `On_FilterManager.EndCapture` to draw a screen-sized render target (`MonoRt.ScreenSized()`) with particle layers and composite it through the `BigTentacle` shader.
- Monochrome's invariants apply to code built on it. Hold cross-frame references with `MonoHandle`, never raw indices. Gameplay logic uses `MonoTime.Tick`; purely visual code uses `MonoTime.RealTime`. Per-frame paths allocate nothing (no `new`, LINQ, dictionary lookups or string building).

### Localization

Edit both `Localization/zh-Hans/*.hjson` and `Localization/en-US/*.hjson` when adding content.

## Gotchas

- Some older source files carry mojibake from a non-UTF-8 encoding (e.g. `#region` comments in `FargosPhantasmMode.cs`). Don't "fix" them wholesale; keep the edit scoped and save as UTF-8.
- Some code under `Content/` was recovered by decompilation (see commit history), so naming and style vary between files.
- `build.txt` lists required mod versions (`FargowiltasSouls@1.7.3.9`, `Fargowiltas@3.3.6`, `Monochrome@1.0.0`). Update it and `lib/` together when bumping Fargo.
