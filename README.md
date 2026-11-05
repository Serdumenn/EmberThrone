# EmberThrone
Match-3 casual mobile game — Unity 6.3 LTS

- Plan & status: [ROADMAP.md](ROADMAP.md)
- How the project is built (for AI agents and developers): [CLAUDE.md](CLAUDE.md)

```bash
dotnet test tools/CoreTests/EmberThrone.Core.Tests.csproj -c Release      # engine tests
dotnet run --project tools/LevelSim -c Release -- Assets/EmberThrone/Resources/Levels   # level difficulty
```
