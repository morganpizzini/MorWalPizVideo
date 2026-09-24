# Frontend Custom Forms

Custom form definitions are loaded from the anonymous ServerAPI custom-form endpoints. The reusable `CustomFormRenderer` is exported by `@morwalpiz/layout` and supports open text, single choice, multiple choice, required validation, loading, errors, success, and an injectable reCAPTCHA token provider.

Form answer requests are serialized by `@morwalpizvideo/services`. Each polymorphic question or answer includes the backend-compatible `_t` discriminator (`OpenQuestion`, `MultipleChoiceQuestion`, `SingleChoiceQuestion`, `BooleanQuestion`, `EmailQuestion`, `OpenAnswer`, `MultipleChoiceAnswer`, `SingleChoiceAnswer`, `BooleanAnswer`, or `EmailAnswer`).

Custom-form response submission uses `POST api/customforms/{id}/responses` and rejects non-success HTTP responses before the renderer displays success. ASP.NET validation fields are retained in the client error so they remain visible to the form UI.

Forms have an explicit lifecycle (`Draft`, `Online`, `Disabled`, `Archived`, or `Deleted`) and access mode (`Direct` or `SurveyOnly`). Legacy documents without these fields remain compatible through the model's effective values. BackOffice deletion is a soft delete and is rejected while a Survey references the form. Survey-only submissions must include the active Survey identifier.

When an Email question has a valid answer, the BackOffice recurring response job sends an acknowledgement using the existing newsletter SMTP configuration. The job claims pending, failed, or expired leases atomically, permanently skips missing or invalid email answers, and backfills legacy embedded responses before processing them. Legacy embedded writes and public submission routes remain unchanged.

Surveys are channel-scoped BackOffice resources with a URL, lifecycle, time window, and ordered form IDs. The public home banner links to `/survey/{url}`; the Survey page presents one form at a time and advances only after the current response succeeds.

The public sponsor page keeps its dedicated `SponsorRequest` endpoint at `POST api/sponsors` and reCAPTCHA action. Its dynamic form must contain exactly one open-text question identified by the configured labels `Name`/`Nome`, `Email`, and `Description`/`Descrizione`; answers are matched by question identity, never by position. Missing identity or reCAPTCHA causes a visible client error before submission.