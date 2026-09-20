using System.Globalization;

namespace RedBlackTree.Comparers;

public sealed class NumericAwareStringComparer : IComparer<string>
{
    public static readonly NumericAwareStringComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var xIsNumber = double.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out var xNumber);
        var yIsNumber = double.TryParse(y, NumberStyles.Float, CultureInfo.InvariantCulture, out var yNumber);

        if (xIsNumber && yIsNumber)
        {
            return xNumber.CompareTo(yNumber);
        }

        return string.CompareOrdinal(x, y);
    }
}
