using UnityEngine;

public static class NumberShortener
{
    public static string Format(long value)
    {
        // Milyar (Billion)
        if (value >= 1_000_000_000)
        {
            float billions = value / 1_000_000_000f;
            return FormatValue(billions, "B");
        }

        // Milyon (Million)
        if (value >= 1_000_000)
        {
            float millions = value / 1_000_000f;
            return FormatValue(millions, "M");
        }

        // Bin (Thousand)
        if (value >= 1_000)
        {
            float thousands = value / 1_000f;
            return FormatValue(thousands, "K");
        }

        return value.ToString();
    }

    private static string FormatValue(float value, string suffix)
    {
        // 100 ve üzeri: tam sayý (örn: 1.93M yerine 1.9M? hayýr, 1.93M yapacak)
        // Ýstediðin gibi 2 ondalýk basamak gösterelim

        // 10'dan büyükse 1 ondalýk basamak, deðilse 2 ondalýk basamak
        if (value >= 100)
        {
            // 100+ için tam sayý yaz (örn: 150M ? 150M)
            return Mathf.FloorToInt(value).ToString() + suffix;
        }
        else if (value >= 10)
        {
            // 10-99.9 arasý: 1 ondalýk basamak (örn: 15.3M)
            return value.ToString("0.#") + suffix;
        }
        else
        {
            // 0-9.99 arasý: 2 ondalýk basamak (örn: 1.93M)
            return value.ToString("0.##") + suffix;
        }
    }
}