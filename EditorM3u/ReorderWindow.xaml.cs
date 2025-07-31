using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EditorM3u
{
    /// <summary>
    /// Lógica de interacción para ReorderWindow.xaml
    /// </summary>
    // Ventana para reorganizar las categorías
    public partial class ReorderWindow : Window
    {
        public ObservableCollection<Category> OrderedCategories { get; private set; }
        private ListView categoriesListView;

        public ReorderWindow(ObservableCollection<Category> categories)
        {
            Title = "Reorganizar Categorías";
            Width = 400;
            Height = 500;
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Crear una copia de las categorías para no modificar la colección original
            OrderedCategories = new ObservableCollection<Category>(categories);

            // Crear la interfaz de usuario
            var mainGrid = new Grid();
            mainGrid.Margin = new Thickness(10);
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Lista de categorías
            categoriesListView = new ListView();
            categoriesListView.Background = new SolidColorBrush(Color.FromRgb(37, 37, 38));
            categoriesListView.Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230));
            categoriesListView.BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70));
            categoriesListView.BorderThickness = new Thickness(1);
            categoriesListView.ItemsSource = OrderedCategories;
            categoriesListView.DisplayMemberPath = "Name";
            categoriesListView.SelectionMode = SelectionMode.Single;
            Grid.SetRow(categoriesListView, 0);

            // Panel de botones
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };

            var upButton = new Button { Content = "↑", Width = 60, Height = 30, Margin = new Thickness(5, 0, 5, 0), Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)), Foreground = Brushes.White };
            upButton.Click += UpButton_Click;

            var downButton = new Button { Content = "↓", Width = 60, Height = 30, Margin = new Thickness(5, 0, 5, 0), Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)), Foreground = Brushes.White };
            downButton.Click += DownButton_Click;

            var okButton = new Button { Content = "Aceptar", Width = 100, Height = 30, Margin = new Thickness(5, 0, 5, 0), Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)), Foreground = Brushes.White };
            okButton.Click += OkButton_Click;

            var cancelButton = new Button { Content = "Cancelar", Width = 100, Height = 30, Margin = new Thickness(5, 0, 5, 0), Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)), Foreground = Brushes.White };
            cancelButton.Click += CancelButton_Click;

            buttonPanel.Children.Add(upButton);
            buttonPanel.Children.Add(downButton);
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 1);

            mainGrid.Children.Add(categoriesListView);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = categoriesListView.SelectedIndex;
            if (selectedIndex > 0)
            {
                var item = OrderedCategories[selectedIndex];
                OrderedCategories.RemoveAt(selectedIndex);
                OrderedCategories.Insert(selectedIndex - 1, item);
                categoriesListView.SelectedIndex = selectedIndex - 1;
            }
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = categoriesListView.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < OrderedCategories.Count - 1)
            {
                var item = OrderedCategories[selectedIndex];
                OrderedCategories.RemoveAt(selectedIndex);
                OrderedCategories.Insert(selectedIndex + 1, item);
                categoriesListView.SelectedIndex = selectedIndex + 1;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

}
