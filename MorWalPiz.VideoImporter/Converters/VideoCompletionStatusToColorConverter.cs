using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace MorWalPiz.VideoImporter.Converters
{
    public class VideoCompletionStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is VideoFile videoFile)
            {
                // Controlla se titolo e descrizione sono vuoti
                bool titleEmpty = string.IsNullOrWhiteSpace(videoFile.Title);
                bool descriptionEmpty = string.IsNullOrWhiteSpace(videoFile.Description);
                
                // Controlla lo stato delle traduzioni
                bool hasTranslations = videoFile.Translations != null && videoFile.Translations.Any();
                bool allTranslationsComplete = false;
                bool someTranslationsComplete = false;
                
                if (hasTranslations)
                {
                    var translationItems = videoFile.Translations.Values.ToList();
                    allTranslationsComplete = translationItems.All(t => 
                        !string.IsNullOrWhiteSpace(t.Title) && !string.IsNullOrWhiteSpace(t.Description));
                    someTranslationsComplete = translationItems.Any(t => 
                        !string.IsNullOrWhiteSpace(t.Title) && !string.IsNullOrWhiteSpace(t.Description));
                }
                
                // Applica la logica dei colori:
                // Rosso: se titolo, descrizione e tutte le traduzioni sono vuote
                if (titleEmpty && descriptionEmpty && (!hasTranslations || !someTranslationsComplete))
                {
                    return new SolidColorBrush(Colors.Red);
                }
                // Verde: se titolo, descrizione e traduzioni sono tutte popolate
                else if (!titleEmpty && !descriptionEmpty && (!hasTranslations || allTranslationsComplete))
                {
                    return new SolidColorBrush(Colors.Green);
                }
                // Giallo: se titolo e descrizione sono presenti ma alcune traduzioni sono vuote
                else if (!titleEmpty && !descriptionEmpty && hasTranslations && !allTranslationsComplete)
                {
                    return new SolidColorBrush(Colors.Gold);
                }
                // Giallo anche per casi intermedi (es. solo titolo o solo descrizione popolati)
                else
                {
                    return new SolidColorBrush(Colors.Gold);
                }
            }
            
            // Default: trasparente se non è un VideoFile
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
