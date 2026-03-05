using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DuplicateFinder.Models;

namespace DuplicateFinder.Helpers
{
    /// <summary>
    /// Konvertor DuplicateStatus -> Brush (barevné odlišení stavů)
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DuplicateStatus status)
            {
                return status switch
                {
                    DuplicateStatus.ExactDuplicate => new SolidColorBrush(Color.FromRgb(253, 231, 233)), // #FDE7E9
                    DuplicateStatus.ProbableDuplicate => new SolidColorBrush(Color.FromRgb(255, 244, 206)), // #FFF4CE
                    _ => new SolidColorBrush(Color.FromRgb(223, 246, 221)) // #DFF6DD
                };
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// Invertuje boolean hodnotu (pro IsEnabled binding při skenování)
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }
    }
}