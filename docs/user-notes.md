# User notes

user note while using the application, can be related to bug, new feature, fixes, improvements

## Frontend

### Backoffice admin SPA
- the login username/email input box should not have auto-capitalize
- when a request goes wrong the navigator shouldn't navigate back. This issue appears on customforms/create : i get 400 as response, but i don't see any alert about the request fail and the page navigates back.
- frontend\back-office-spa\src\routes\customForms\form\loader.ts and frontend\back-office-spa\src\routes\customForms\detail\loader.ts missing breadcrumbidentifier
- frontend\back-office-spa\src\routes\customForms\form\component.tsx has missing default input as question possibility. check backoffice contract if single input is possible. Options should be 'Text' / 'Text Area' / 'Single choice' / 'Multiple choice' / 'True / False'  

### morwalpizvideo.client

- there is an old implementation which make the UI fail on presenting information: from backoffice i can create forms, that forms can be used in all application, it is a structure of a form which will be presented in UI. But in the past that form were treated as a Survey and now, after create a new form for sponsor page, i see in morwalpizvideo.client a banner which indicate a new survey. if i click on "rispondi ora" it goes to 'custom-forms/sponsor' which is a 404 page. I want to keep the survey function but handled different: new backoffice object which represent a survey, it can contains multiple forms, it has a relation with channelId. If a survey is online then the banner appears and a user can navigate into the page to fill that.
- when submitting a form i see "form submitted successfully" even if in the network i have error 400 on submitting. the error is this {
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

### Form submit feedback [improvement]
server.API is responsable to save all responses coming from a UI form. I want to create an automatic procedure that every 10 minutes scan the responses, understand if the responses is new (maybe create a property 'processed' which goes true when the job analyze that) and if the response contains an email, coming from 'email' form question. i want to create an email like "we receive your submission, this are the data provided, we will get in touch soon". 


## Video importer

### Image creation [new feature]

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