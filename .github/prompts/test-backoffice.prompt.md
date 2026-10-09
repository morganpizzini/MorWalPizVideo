---
description: "Run the BackOffice test suite"
agent: "MorWalPiz Delivery Handoff"
model: GPT-5.6 Luna (copilot)
---

From the repository root, run:

```powershell
dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --no-restore --filter "Category=TestGroup:BackOffice|Category=TestGroup:Shared|Category=TestGroup:CrossApi" --verbosity minimal