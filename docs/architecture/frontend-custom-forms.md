# Frontend Custom Forms

Custom form definitions are loaded from the anonymous ServerAPI custom-form endpoints. The reusable `CustomFormRenderer` is exported by `@morwalpiz/layout` and supports open text, single choice, multiple choice, required validation, loading, errors, success, and an injectable reCAPTCHA token provider.

Form answer requests are serialized by `@morwalpizvideo/services`. Each polymorphic question or answer includes the backend-compatible `_t` discriminator (`OpenQuestion`, `MultipleChoiceQuestion`, `SingleChoiceQuestion`, `OpenAnswer`, `MultipleChoiceAnswer`, or `SingleChoiceAnswer`).

The public sponsor page keeps its existing sponsor application endpoint and reCAPTCHA action, while obtaining its form definition from the active public custom-form endpoint using the local `formUrl` constant.