namespace MorWalPizVideo.Server.Utils
{
    public static class StringUtils
    {
        public static string TrimDescription(this string description, int maxLength = 100)
        {
            if (description.Length <= maxLength)
            {
                return description;
            }

            // Trova la posizione dell'ultimo spazio prima del limite
            int lastSpaceIndex = description.LastIndexOf(' ', maxLength);

            // Se non ci sono spazi, taglia semplicemente al limite e aggiungi " [...]"
            if (lastSpaceIndex == -1)
            {
                return description.Substring(0, maxLength) + " [...]";
            }

            // Ritorna la stringa fino all'ultimo spazio con " [...]" aggiunto
            return description.Substring(0, lastSpaceIndex) + " [...]";
        }
    }

}
