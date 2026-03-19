namespace Utilities
{
    /// <summary>
    /// Formats large numbers into compact human-readable strings.
    /// 999 → "999", 1000 → "1K", 1500 → "1.5K", 999999 → "999.9K",
    /// 1000000 → "1M", 1500000 → "1.5M", 1000000000 → "1B", etc.
    /// </summary>
    public static class NumberFormatter
    {
        private static readonly (long threshold, long divisor, string suffix)[] Tiers =
        {
            (1_000_000_000L, 1_000_000_000L, "B"),
            (1_000_000L,     1_000_000L,     "M"),
            (1_000L,         1_000L,         "K"),
        };

        /// <summary>
        /// Format an integer into a compact string.
        /// Values below 1000 are returned as-is. Above that: 1K, 1.5K, 1M, 1.5M, 1B, etc.
        /// </summary>
        public static string Format(long value)
        {
            bool negative = value < 0;
            long abs = negative ? -value : value;

            foreach (var (threshold, divisor, suffix) in Tiers)
            {
                if (abs >= threshold)
                {
                    float divided = abs / (float)divisor;
                    // Show one decimal if it's not a whole number, otherwise show whole
                    string num = (divided % 1f < 0.05f)
                        ? ((int)divided).ToString()
                        : divided.ToString("F1");
                    return negative ? $"-{num}{suffix}" : $"{num}{suffix}";
                }
            }

            return value.ToString();
        }

        /// <summary>
        /// Format a float value (e.g. distance in meters).
        /// Below 1000: "845.2m". Above: "1.5Km", "1M m", etc.
        /// </summary>
        public static string FormatDistance(float meters)
        {
            if (meters < 1_000f)
                return $"{meters:F1}m";
            if (meters < 1_000_000f)
                return $"{meters / 1_000f:F1}km";
            return $"{meters / 1_000_000f:F1}Mm";
        }

        /// <summary>
        /// Overload for int values.
        /// </summary>
        public static string Format(int value) => Format((long)value);
    }
}
