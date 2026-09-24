# Image creation

The Video Importer image workflow is independent from BackOffice and the web frontends. It supports provider-compatible generation and editing through the configured serverless endpoint.

Configure `ImageGeneration` in `appsettings.json`, environment variables, or user secrets. The API key is resolved only from Azure Key Vault configuration using `ImageGeneration:ApiKeySecretName` (for example, the Key Vault secret `ImageGenerationApiKey`); it is never saved to SQLite or output files. Configure `KeyVaultUrl` and `OutputDirectory` before using the feature.

`SupportedSizes` is an explicit requested-size to provider-size map. Only mapped sizes are sent, and unsupported ratios or dimensions are rejected without cropping or stretching. The default map is `1024x1024` to `1024x1024`; add provider-supported values explicitly.

Generation sends JSON with `prompt`, `size`, `quality`, `background`, `output_compression`, `output_format`, and `n` (plus the configured model). Editing sends one `image`, an optional `mask`, and `prompt` as multipart form data. Additional reference images are rejected because the provider contract does not define their semantics.

Images are written only to the configured output directory with collision-free names. The application previews the first result. Prompt templates are global to the local machine and stored in `%LOCALAPPDATA%/MorWalPiz.VideoImporter/image-prompt-templates.json`; no image history is persisted.

Live provider calls require a reachable endpoint and Key Vault credentials and are not verified by the automated tests.