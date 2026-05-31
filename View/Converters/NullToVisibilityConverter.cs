using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TCC_Assiduidade.View.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Se o valor for nulo, a gaveta se esconde (Collapsed). 
            // Se tiver um aluno selecionado, ela aparece (Visible).
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // O caminho de volta não é necessário para essa lógica, então jogamos a exceção padrão
            throw new NotImplementedException();
        }
    }
}