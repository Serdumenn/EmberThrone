# EmberThrone

A match-3 puzzle game for Android by **Ocean Forge**.

![Android Build](https://img.shields.io/github/actions/workflow/status/Serdumenn/EmberThrone/build-android.yml?branch=main&label=Android%20Build&logo=unity&logoColor=white&style=for-the-badge)
![Core Tests](https://img.shields.io/github/actions/workflow/status/Serdumenn/EmberThrone/core-tests.yml?branch=main&label=Core%20Tests&style=for-the-badge)

## Game

- **Match-3:** rockets, bombs, homing pieces and color bombs, with a combo for every pair of specials
- **Obstacles:** boxes, floor tiles, ice covers, collectibles and shaped boards
- **Levels:** goals, move counter, stars, win and lose flow; every level is tuned by a bot simulator
- **Look and feel:** 3D-rendered pieces and board, particles, screen shake, haptics, praise words and an end-of-level fireworks show
- **Status:** playable prototype, Phase 2 (look and feel) in progress. Plan: [ROADMAP.md](ROADMAP.md)

## Technology

| Area | Stack |
|---|---|
| Engine | Unity 6000.3.25f1, URP 2D, UI Toolkit built from code |
| Gameplay | Deterministic pure C# engine (`EmberThrone.Core`), testable without Unity |
| Input | Input System (touch and mouse) |
| Art | Blender 5.1 renders from scripts (`ArtSource/blender`) |
| Quality | EditMode and PlayMode tests, bot level simulator, GitHub Actions CI |

## Repository

| Path | Contents |
|---|---|
| `Assets/EmberThrone` | Game code, levels, tests |
| `Assets/_Game` | Older foundation code kept for later phases |
| `tools` | Engine test project, level simulator, helper scripts |
| `docs/design` | Design process and decision log |
| `docs/qa` | Quality audits |
| `docs/release` | Release runbook |
| `.github/workflows` | CI pipelines |

## Builds

- Every push to `main` runs the tests and builds a development APK.
- A `v*` tag (for example `v0.1.0`) runs the tests and builds the signed release AAB.

Setup: [docs/CI-ANDROID.md](docs/CI-ANDROID.md) and [docs/release/README.md](docs/release/README.md).
For AI agents and developers: [CLAUDE.md](CLAUDE.md).

## License

Proprietary. © 2026 Ocean Forge. All rights reserved. See [LICENSE](LICENSE).
