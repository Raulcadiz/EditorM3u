using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace EditorM3u
{
    public static class ResourceDictionaryExtensions
    {
        public static void SetDarkThemeResources(this ResourceDictionary resources)
        {
            // Colores principales
            resources["BackgroundColor"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            resources["BackgroundColorLight"] = new SolidColorBrush(Color.FromRgb(37, 37, 38));
            resources["ForegroundColor"] = new SolidColorBrush(Color.FromRgb(230, 230, 230));
            resources["AccentColor"] = new SolidColorBrush(Color.FromRgb(0, 122, 204));
            resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(63, 63, 70));

            // Plantilla para categorías en TreeView
            var categoryTemplate = new HierarchicalDataTemplate();
            categoryTemplate.ItemsSource = new Binding("Entries");

            var stackPanel = new FrameworkElementFactory(typeof(StackPanel));
            stackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            var checkBox = new FrameworkElementFactory(typeof(CheckBox));
            checkBox.SetBinding(CheckBox.IsCheckedProperty, new Binding("IsSelected") { Mode = BindingMode.TwoWay });
            // Asignar eventos mediante código

            var textBlock = new FrameworkElementFactory(typeof(TextBlock));
            textBlock.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            textBlock.SetValue(TextBlock.MarginProperty, new Thickness(5, 0, 0, 0));
            textBlock.SetValue(TextBlock.ForegroundProperty, resources["ForegroundColor"]);

            stackPanel.AppendChild(checkBox);
            stackPanel.AppendChild(textBlock);

            categoryTemplate.VisualTree = stackPanel;

            resources["CategoryTemplate"] = categoryTemplate;
        }
    }
}
