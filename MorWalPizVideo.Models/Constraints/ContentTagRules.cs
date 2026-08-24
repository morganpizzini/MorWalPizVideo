namespace MorWalPizVideo.Models.Constraints
{
    /// <summary>
    /// Server-side rules for free-form content tags on the YouTubeContent aggregate.
    /// Normalization trims whitespace, rejects empty values and de-duplicates
    /// case-insensitively while preserving the first display casing supplied by the caller.
    /// </summary>
    public static class ContentTagRules
    {
        /// <summary>Maximum number of distinct tags stored on a single content aggregate.</summary>
        public const int MaxTagsPerContent = 20;

        /// <summary>Maximum length of a single tag after trimming.</summary>
        public const int MaxTagLength = 32;

        /// <summary>Maximum number of suggestions returned by the tag suggestions endpoint.</summary>
        public const int MaxSuggestions = 50;

        /// <summary>Default number of suggestions returned when the caller does not specify a limit.</summary>
        public const int DefaultSuggestions = 20;

        /// <summary>
        /// Normalizes a caller-supplied tag collection. Returns false with a human readable
        /// <paramref name="error"/> when a value is empty/whitespace or a bound is exceeded.
        /// </summary>
        public static bool TryNormalize(IEnumerable<string?>? tags, out string[] normalized, out string? error)
        {
            normalized = [];
            error = null;

            if (tags is null)
            {
                return true;
            }

            var accepted = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var tag in tags)
            {
                var trimmed = tag?.Trim() ?? string.Empty;
                if (trimmed.Length == 0)
                {
                    error = "Tags cannot be empty or whitespace";
                    normalized = [];
                    return false;
                }

                if (trimmed.Length > MaxTagLength)
                {
                    error = $"Tags cannot exceed {MaxTagLength} characters";
                    normalized = [];
                    return false;
                }

                // Case-insensitive de-duplication keeps the first display casing.
                if (seen.Add(trimmed))
                {
                    accepted.Add(trimmed);
                }
            }

            if (accepted.Count > MaxTagsPerContent)
            {
                error = $"A maximum of {MaxTagsPerContent} tags is allowed";
                normalized = [];
                return false;
            }

            normalized = [.. accepted];
            return true;
        }
    }
}
