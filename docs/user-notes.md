# User notes

user note while using the application, can be related to bug, new feature, fixes, improvements

## Frontend

### Backoffice admin SPA

#### UI enhancement

Completed: the login username/email input disables automatic capitalization.

#### submit and callback error feedback

Completed for custom form create/edit: failed saves now show a danger toast and do not navigate.

#### form pages ui breadcrumbs

Completed: custom form create/edit and detail loaders now provide a breadcrumb identifier.

#### form creation question type 

The contract currently supports only one text question type (`OpenText`), rendered as a text area. Adding a separate single-line `Text` option requires a new contract type and renderer support.


Implemented: generic form submissions now keep the `api/customforms/{id}/responses` contract and show non-success responses as errors. The sponsor page keeps its dedicated `SponsorRequest` endpoint, obtains reCAPTCHA at submit time, validates named questions before mapping, and displays missing-token or server validation failures.
### morwalpizvideo.client

#### Survey workflow

Implemented: surveys are separate channel-scoped BackOffice objects containing ordered forms. Online surveys produce a home banner and open at `/survey/{url}`. Forms are presented sequentially; the next form is shown only after a successful response, and SurveyOnly forms require the active survey context when submitted.

#### form implementation legacy workflow

there is an old implementation which make the UI fail on presenting information: from backoffice i can create forms, that forms can be used in all application, it is a structure of a form which will be presented in UI. But in the past that form were treated as a Survey and now, after create a new form for sponsor page, i see in morwalpizvideo.client a banner which indicate a new survey. if i click on "rispondi ora" it goes to 'custom-forms/sponsor' which is a 404 page. I want to keep the survey function but handled different: new backoffice object which represent a survey, it can contains multiple forms, it has a relation with channelId. If a survey is online then the banner appears and a user can navigate into the page to fill that.

#### client form submitting error

when submitting a form i see "form submitted successfully" even if in the network i have error 400 on submitting. the error is this {
    "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
    "title": "One or more validation errors occurred.",
    "status": 400,
    "errors": {
        "Email": [
            "The Email field is not a valid e-mail address."
        ],
        "Token": [
            "The Token field is required."
        ],
        "Description": [
            "The Description field is required."
        ]
    },
    "traceId": "00-66f929759f1816a358b9dd34fc228be0-66c64ac24bdf76b3-01"
}
the form does not contains any Email/Description/Token request, understand why he runs evaluation on that field. Could token depends on reCaptcha? But still the api does not work.


## Backend

## Backoffice

### Form submit feedback [implemented]
Custom forms now support an explicit Email question. Standalone response documents are processed by the BackOffice Hangfire job every 10 minutes using the existing SMTP/newsletter email service. Responses use additive pending/claimed/processed/skipped/failed state with an atomic lease claim, deterministic provider key, HTML-encoded acknowledgement data, and retry-safe failure handling. Historical documents in the response collection are eligible; missing or invalid email answers are marked skipped. Legacy embedded responses and public routes remain compatible.


## Video importer

### Image creation [new feature]

Implemented in `MorWalPiz.VideoImporter`: generation and one-image-plus-optional-mask editing use the configured serverless endpoint and model. The API key is resolved from Azure Key Vault only. Supported sizes are an explicit configuration map; unsupported ratios/dimensions are rejected without cropping or stretching. Results are saved collision-free only in the configured output directory and can be previewed in-app. Global prompt templates are stored locally in a JSON file, with no image-history persistence. Additional reference images are intentionally rejected because provider semantics are undefined. Live-provider verification still requires a configured endpoint and credentials.

The functionality is completly unrelated from backoffice admin API.
The user will create a prompt, add images, set parameters that will be send to azure foundry model gpt-image2.5-sunburst

this are the requirements to interact with the model
Attività iniziali
1. Autenticazione con la chiave API
Per gli endpoint API serverless, distribuire il modello per generare l'URL dell'endpoint e una chiave API per l'autenticazione nel servizio. In questo esempio, l'endpoint e la chiave sono stringhe che contengono l'URL dell'endpoint e la chiave API. L'URL dell'endpoint API e la chiave API possono essere trovati nella pagina Distribuzioni ed endpoint dopo la distribuzione del modello.

Se si usa Bash:

export AZURE_API_KEY="<your-api-key>"

Se si è in PowerShell:

$Env:AZURE_API_KEY = "<your-api-key>"

Se si usa il prompt dei comandi di Windows:

set AZURE_API_KEY = <your-api-key>

2. Eseguire un esempio di codice di base
Per generare un'immagine, incolla quanto segue in una shell

curl -X POST "<your-endpoint-url>" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $AZURE_API_KEY" \
  -d '{
     "prompt": "A photograph of a red fox in an autumn forest",
     "size": "1024x1024",
     "quality": "low",
     "background": "auto",
     "output_compression": 100,
     "output_format": "png",
     "n": 1
    }' | jq -r '.data[0].b64_json' | base64 --decode > generated_image.png

Per modificare un'immagine, incolla quanto segue in una shell

curl -X POST "<your-endpoint-url>" \
  -H "Authorization: Bearer $AZURE_API_KEY" \
  -F "image=@image_to_edit.png" \
  -F "mask=@mask.png" \
  -F "prompt=Make this black and white"  | jq -r '.data[0].b64_json' | base64 --decode > edited_image.png

in case some parameters are needed add them to a setting configuration in application menu.
The scope of this functionality is mainly create portrait for reel thumbnail: i'll provide reference images like place, objects, subjects, a specific pixel size (i see probably the model force a specific pixel ratio, investigate on that) which will be then traduce in ratio like 1:1 16:9 9:16 4:5. then the model will elaborate the image, and the image is then download into a specific directory specify in setting (with API key and other parameters)
THe prompt can be saved as template in order to not re-type the same text multiple times. The template has a name property and the prompt.