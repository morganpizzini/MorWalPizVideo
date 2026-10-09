---
description: "Run the ServerAPI test suite"
agent: "MorWalPiz Delivery Handoff"
model: GPT-5.6 Luna (copilot)
---

From the repository root, run:

```powershell
dotnet test MorWalPizVideo.ServerAPI.Tests/MorWalPizVideo.ServerAPI.Tests.csproj --configuration Release --no-restore --filter "Category=TestGroup:ServerAPI|Category=TestGroup:Shared|Category=TestGroup:CrossApi" --verbosity minimal
```