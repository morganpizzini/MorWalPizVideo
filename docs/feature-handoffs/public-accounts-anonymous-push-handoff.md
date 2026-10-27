
# Public Accounts and Anonymous Push Handoff

## Implemented

- Public registration now requires Terms and Privacy Notice acknowledgements in the shared contract, frontend form, controller, and domain service.
- The domain service records server-owned legal versions and UTC acceptance timestamps.
- Public account and session IDs use `ObjectId.GenerateNewId().ToString()` for compatibility with the existing BSON `ObjectId` representation.
- Authenticated public users see their display name in the header, with username fallback and bounded ellipsis behavior.
- Added `/privacy` and `/terms`, preserved `/cookie-policy` as a compatibility route to Privacy, and linked both documents from the footer.
- Profile updates refresh the public account session context.

## Validation

- Public client production build command: `npm --prefix frontend/morwalpizvideo.client run build`.
- Focused backend test command: `dotnet test MorWalPizVideo.BackOffice.Tests/MorWalPizVideo.BackOffice.Tests.csproj --configuration Release --filter FullyQualifiedName~PublicAccountEndpointTests --verbosity normal`.
- The execution wrapper returned no output for the extended runs, so final pass counts require confirmation in the local terminal/test runner.

## Residual risks

- The legal page text is a concise product-facing baseline and should receive final legal-owner review before production rollout.
- Existing package vulnerability warnings were unrelated to this change and remain outstanding.
