# Fonts Directory

Place your .ttf font files in this directory. The font name used in the API should match the filename without the .ttf extension.

For example:
- File: `arial.ttf` → FontStyle: `arial`
- File: `Obelix-pro.ttf` → FontStyle: `Obelix-pro`

## Usage

Call the API endpoint:
```
POST /api/Utility/generate-text-image
```

With JSON body:
```json
{
    "nome": "Mario",
    "cognome": "Rossi",
    "fontStyle": "arial",
    "fontSize": 24
}
```

The API will return a PNG image with:
- First line: Nome
- Second line: Cognome  
- 15px margin from image borders
- Image size calculated based on text and font size
