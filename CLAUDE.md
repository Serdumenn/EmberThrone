# EmberThrone — working agreement for AI agents

Commercial match-3 mobile game (Unity 6.3 LTS `6000.3.11f1`, URP 2D, Android first, portrait).
The owner does not write code: AI agents build everything, so keep the repo self-explanatory
and keep `ROADMAP.md` up to date (tick items, add what you learned).

## Layout

| Path | What |
|---|---|
| `Assets/EmberThrone/Scripts/Core/` | Pure C# engine, asmdef `EmberThrone.Core` (`noEngineReferences`). Board, matching, specials & combos, obstacles, gravity, goals, boosters, level JSON, bot simulator. |
| `Assets/EmberThrone/Tests/EditMode/` | NUnit tests. Run in Unity Test Runner **and** with `dotnet test`. |
| `Assets/EmberThrone/Resources/Levels/` | `level_NNNN.json` (format: header comment in `Core/Levels/LevelParser.cs`). |
| `tools/CoreLib`, `tools/CoreTests` | dotnet projects that compile the Unity sources (netstandard2.1, C# 9 = Unity's limits). |
| `tools/LevelSim` | Bot simulator: win rates, invariant fuzzing, move-count tuning, game traces. |
| `tools/gen_meta.py` | Creates missing `.meta` files. |
| `.github/workflows/core-tests.yml` | Engine CI (no Unity license needed). |
| `.github/workflows/build-android.yml` | APK build via game-ci (needs Unity license secrets; `main` + manual). |

## Rules

1. **Core never touches UnityEngine.** C# 9 max, .NET Standard 2.1 APIs only — the csproj enforces both.
2. **Determinism.** Randomness in Core comes only from `Pcg32`. No `System.Random`, clocks, or hash-order-dependent logic. Same level + seed + inputs ⇒ same game (replays, server-side validation, bots rely on this).
3. **Presentation is a viewer.** Unity code calls `GameSession` and animates the returned `MoveOutcome.Steps`; it never mutates `Board`.
4. **Balance lives in data.** `GameRules` and level JSON, never magic numbers in code.
5. **`.meta` files.** No editor runs here: after adding anything under `Assets/`, run `python3 tools/gen_meta.py`. Never change an existing GUID.
6. **Tests.** NUnit 3 *classic* asserts only (Unity ships an NUnit 3.5 fork): `Assert.AreEqual/IsTrue/Throws…`, `CollectionAssert`. No `Assert.Multiple`, no NUnit 4 APIs. Board-text helpers are in `TestUtil.cs` (rows top-first, y=0 bottom).
7. **Original IP.** Names, characters, art, music, levels and UI are EmberThrone's own. Only generic genre mechanics are shared with other games.
8. Turkish is the owner's language: reply to them in short, plain Turkish.

## Before every push

```bash
python3 tools/gen_meta.py --check
dotnet test tools/CoreTests/EmberThrone.Core.Tests.csproj -c Release
dotnet run --project tools/LevelSim -c Release -- --runs 100 --validate --fail-on-error Assets/EmberThrone/Resources/Levels
```

## Where you are running

- **Owner's computer (preferred for anything Unity-side: scenes, UI, animation, builds).**
  Verify Unity code yourself instead of guessing: open the project in the editor and read the console, or run
  EditMode tests headless (close the editor first — batchmode cannot open a project that is already open):
  - Windows: `"C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml`
  - macOS: `/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity` with the same arguments.
  The editor writes `.meta` files itself; commit them.
- **Cloud container.** No Unity, no phone. `apt-get install -y dotnet-sdk-8.0` (Ubuntu 24.04 repo) for the dotnet checks;
  Unity-side code is verified only by the Android CI build (needs license secrets). Prefer pure C# work here.

## Level tools

```bash
# difficulty table (greedy bot, 35% random moves ~ casual player)
dotnet run --project tools/LevelSim -c Release -- --runs 300 --mistakes 0.35 Assets/EmberThrone/Resources/Levels
# find the move count for a target win rate and write it into the file
dotnet run --project tools/LevelSim -c Release -- --runs 300 --mistakes 0.35 --tune 0.8 --write Assets/EmberThrone/Resources/Levels/level_0007.json
# print one game move by move
dotnet run --project tools/LevelSim -c Release -- --trace 3 Assets/EmberThrone/Resources/Levels/level_0001.json
```

## Engine cheat sheet

- Specials are colorless. 4 in a line → rocket (perpendicular, `GameRules.RocketPerpendicularToMatch`),
  2x2 → homing, L/T → bomb (5x5), 5 in a line → color bomb. Tap or swap fires them; two specials swapped = combo (`ComboKind`).
- Cell layers: floor (HP) → piece → cover (HP). Cover locks the piece and absorbs hits. Boxes take damage from adjacent matches and blasts. Collectibles are blast-proof and are collected on the lowest cell of their column.
- Resolution: swap/tap → clear waves (chained specials fire in the next wave) → tick-based gravity (straight, then diagonal around blockers; spawners refill) → cascades → win/lose → auto-shuffle if no moves.
