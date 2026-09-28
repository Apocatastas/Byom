using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Byom.WPF.Converters;

public sealed class AmountToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Negative = new(Color.FromRgb(0xC0, 0x39, 0x2B));
    private static readonly SolidColorBrush Positive = new(Color.FromRgb(0x27, 0xAE, 0x60));
    private static readonly SolidColorBrush Neutral = new(Color.FromRgb(0x7F, 0x8C, 0x8D));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not decimal amount)
        {
            return Neutral;
        }

        if (amount < 0) return Negative;
        if (amount > 0) return Positive;
        return Neutral;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
