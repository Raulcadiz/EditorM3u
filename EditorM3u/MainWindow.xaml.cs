using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Path = System.IO.Path;
using System;
using System.Linq;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Sharing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;


namespace EditorM3u
{
    public partial class MainWindow : Window
    {
        // Estructura para almacenar múltiples listas
        private class M3UList
        {
            public string Name { get; set; }
            public ObservableCollection<M3UEntry> Entries { get; set; }
            public ObservableCollection<Category> Categories { get; set; }
            public TabItem TabItem { get; set; }
            public TreeView CategoryTreeView { get; set; }
            public ListView M3UListView { get; set; }
            public ListView GroupsListView { get; set; }
        }

        // Lista de listas M3U
        private List<M3UList> m3uLists = new List<M3UList>();

        // Lista actual
        private M3UList currentList;

        // Constantes para Dropbox
        private const string DropboxAppKey = "5kzh0hhto6wcw6s";
        private const string DropboxAppSecret = "b9fa4mztzc0ta5c";
        private string DropboxAccessToken;

        public MainWindow()
        {
            InitializeComponent();
            // Registrar estilos oscuros personalizados
            Application.Current.Resources.SetDarkThemeResources();

            // Inicializar la lista principal
            InitializeNewList("Lista Principal", 0);
        }

        private void InitializeNewList(string name, int index)
        {
            var newList = new M3UList
            {
                Name = name,
                Entries = new ObservableCollection<M3UEntry>(),
                Categories = new ObservableCollection<Category>()
            };

            // Crear una copia de las vistas para esta lista
            if (index > 0)
            {
                // Si no es la lista principal, crear una nueva pestaña
                var newTab = new TabItem
                {
                    Header = name,
                    Content = CreateTabContent(newList)
                };

                newList.TabItem = newTab;
                MainTabControl.Items.Add(newTab);
            }
            else
            {
                // Para la lista principal, usar los controles existentes
                currentList = newList;
                CategoryTreeView.ItemsSource = newList.Categories;
                M3UListView.ItemsSource = newList.Entries;
            }

            m3uLists.Add(newList);

            // Si es una lista nueva (no la principal), seleccionarla
            if (index > 0)
            {
                MainTabControl.SelectedIndex = index;
                SetCurrentList(index);
            }
        }

        private Grid CreateTabContent(M3UList list)
        {
            // Este método crea una copia de la estructura de la pestaña principal
            // para cada nueva lista que se cargue
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Sección izquierda - Categorías
            var leftGrid = new Grid();
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(leftGrid, 0);
            Grid.SetColumn(leftGrid, 0);

            var selectAllCheckBox = new CheckBox
            {
                Content = "Seleccionar Todo",
                IsChecked = false,
                Margin = new Thickness(0, 0, 0, 10)
            };
            selectAllCheckBox.Checked += (s, e) => SelectAll(list, true);
            selectAllCheckBox.Unchecked += (s, e) => SelectAll(list, false);
            Grid.SetRow(selectAllCheckBox, 0);

            var treeView = new TreeView
            {
                ItemsSource = list.Categories,
                Margin = new Thickness(0, 0, 5, 0)
            };

            // En lugar de usar una referencia a un estilo existente, crea una plantilla directamente
            var itemTemplate = new HierarchicalDataTemplate();
            itemTemplate.ItemsSource = new Binding("Entries");

            var panel = new FrameworkElementFactory(typeof(StackPanel));
            panel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            var checkBox = new FrameworkElementFactory(typeof(CheckBox));
            checkBox.SetBinding(CheckBox.IsCheckedProperty, new Binding("IsSelected") { Mode = BindingMode.TwoWay });
            // Añadir eventos usando un nombre que podamos manejar después
            checkBox.AddHandler(CheckBox.CheckedEvent, new RoutedEventHandler(CheckBox_Checked_Handler));
            checkBox.AddHandler(CheckBox.UncheckedEvent, new RoutedEventHandler(CheckBox_Unchecked_Handler));
            // Guardar referencia a la lista actual para usarla en los manejadores de eventos
            checkBox.SetValue(FrameworkElement.TagProperty, list);

            var textBlock = new FrameworkElementFactory(typeof(TextBlock));
            textBlock.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            textBlock.SetValue(TextBlock.MarginProperty, new Thickness(5, 0, 0, 0));

            panel.AppendChild(checkBox);
            panel.AppendChild(textBlock);

            itemTemplate.VisualTree = panel;

            treeView.ItemTemplate = itemTemplate;
            Grid.SetRow(treeView, 1);

            leftGrid.Children.Add(selectAllCheckBox);
            leftGrid.Children.Add(treeView);

            // Sección derecha - Grupos seleccionados y entradas
            var rightGrid = new Grid();
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(rightGrid, 0);
            Grid.SetColumn(rightGrid, 1);

            var titleBlock = new TextBlock
            {
                Text = "Grupos Seleccionados",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5, 0, 0, 10)
            };
            Grid.SetRow(titleBlock, 0);

            var tabControl = new TabControl();
            Grid.SetRow(tabControl, 1);

            var channelsTab = new TabItem { Header = "Canales" };
            var listView = new ListView
            {
                BorderBrush = (SolidColorBrush)FindResource("BorderBrush"),
                BorderThickness = new Thickness(1)
            };
            var gridView = new GridView();
            gridView.Columns.Add(new GridViewColumn { Header = "Categoría", DisplayMemberBinding = new Binding("Categoria"), Width = 100 });
            gridView.Columns.Add(new GridViewColumn { Header = "Nombre", DisplayMemberBinding = new Binding("Nombre"), Width = 200 });
            gridView.Columns.Add(new GridViewColumn { Header = "URL", DisplayMemberBinding = new Binding("URL"), Width = 300 });
            listView.View = gridView;
            channelsTab.Content = listView;

            var categoriesTab = new TabItem { Header = "Categorías" };
            var groupsListView = new ListView
            {
                BorderBrush = (SolidColorBrush)FindResource("BorderBrush"),
                BorderThickness = new Thickness(1)
            };
            var groupsGridView = new GridView();
            groupsGridView.Columns.Add(new GridViewColumn { Header = "Categoría", DisplayMemberBinding = new Binding("Name"), Width = 200 });
            groupsGridView.Columns.Add(new GridViewColumn { Header = "Cantidad", DisplayMemberBinding = new Binding("EntryCount"), Width = 100 });
            groupsListView.View = groupsGridView;
            categoriesTab.Content = groupsListView;

            tabControl.Items.Add(channelsTab);
            tabControl.Items.Add(categoriesTab);

            rightGrid.Children.Add(titleBlock);
            rightGrid.Children.Add(tabControl);

            // Botones inferiores
            var bottomGrid = new Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(bottomGrid, 1);
            Grid.SetColumnSpan(bottomGrid, 2);
            bottomGrid.Margin = new Thickness(0, 10, 0, 0);

            var errorTextBlock = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(255, 82, 82)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(errorTextBlock, 0);

            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(buttonStack, 1);

            var loadButton = new Button
            {
                Content = "Cargar .m3u",
                Width = 120,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            loadButton.Click += (s, e) => LoadM3UFile(list);

            var saveButton = new Button
            {
                Content = "Guardar .m3u",
                Width = 120,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            saveButton.Click += (s, e) => SaveM3UFile(list);

            var uploadButton = new Button
            {
                Content = "Subir a Dropbox",
                Width = 120,
                Height = 30
            };
            uploadButton.Click += UploadToDropboxButton_Click;

            buttonStack.Children.Add(loadButton);
            buttonStack.Children.Add(saveButton);
            buttonStack.Children.Add(uploadButton);

            bottomGrid.Children.Add(errorTextBlock);
            bottomGrid.Children.Add(buttonStack);

            // Agregar todo al grid principal
            grid.Children.Add(leftGrid);
            grid.Children.Add(rightGrid);
            grid.Children.Add(bottomGrid);

            // Guardar referencia a los controles
            list.CategoryTreeView = treeView;
            list.M3UListView = listView;
            list.GroupsListView = groupsListView;

            return grid;
        }
        private void CheckBox_Checked_Handler(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                var list = checkBox.Tag as M3UList;
                if (list != null)
                {
                    UpdateViewsForList(list);
                }
            }
        }

        private void CheckBox_Unchecked_Handler(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                var list = checkBox.Tag as M3UList;
                if (list != null)
                {
                    UpdateViewsForList(list);
                }
            }
        }

        private void UpdateViewsForList(M3UList list)
        {
            // Actualiza la vista M3UListView para esta lista
            var selectedEntries = list.Categories
                .Where(c => c.IsSelected)
                .SelectMany(c => c.Entries)
                .ToList();

            list.M3UListView.ItemsSource = selectedEntries;

            // Actualiza la vista GroupsListView para esta lista
            list.GroupsListView.ItemsSource = list.Categories
                .Where(c => c.IsSelected)
                .Select(c => new {
                    Name = c.Name,
                    EntryCount = c.Entries.Count
                })
                .ToList();
        }
       
        private void SetCurrentList(int index)
        {
            if (index >= 0 && index < m3uLists.Count)
            {
                currentList = m3uLists[index];

                // Actualizar los controles para mostrar la lista actual
                if (index == 0)
                {
                    // Para la lista principal
                    CategoryTreeView.ItemsSource = currentList.Categories;
                    M3UListView.ItemsSource = currentList.Entries;
                    UpdateGroupsListView();
                }
                else
                {
                    // Para las pestañas adicionales, los controles ya están vinculados
                }
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = MainTabControl.SelectedIndex;
            SetCurrentList(selectedIndex);
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PlaceholderTextBlock != null)
                PlaceholderTextBlock.Visibility = System.Windows.Visibility.Visible;

            if (ListNamePlaceholderTextBlock != null)
                PlaceholderTextBlock.Visibility = System.Windows.Visibility.Visible;
        }

        private async void ProcessServerButton_Click(object sender, RoutedEventArgs e)
        {
            string url = UrlTextBox.Text;
            string listName = ListNameTextBox.Text;

            if (string.IsNullOrEmpty(listName))
                listName = "iptv";

            try
            {
                string content = await DownloadM3UList(url);
                ParseM3UContent(content);
                UpdateCategories();
                UpdateGroupsListView();
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = $"Error al procesar la lista: {ex.Message}";
            }
        }

        private void AddListButton_Click(object sender, RoutedEventArgs e)
        {
            string listName = ListNameTextBox.Text;
            if (string.IsNullOrWhiteSpace(listName))
            {
                MessageBox.Show("Por favor, ingrese un nombre para la lista.");
                return;
            }

            InitializeNewList(listName, m3uLists.Count);
        }

        private async Task<string> DownloadM3UList(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("No se pudo descargar la lista M3U.");
                }

                return await response.Content.ReadAsStringAsync();
            }
        }

        private void ParseM3UContent(string content)
        {
            currentList.Entries.Clear();
            var regex = new Regex(@"#EXTINF:-1\s+(?<attrs>.*?)\s*,(?<nombre>.+)", RegexOptions.Compiled);
            var attrRegex = new Regex(@"(?<key>[\w-]+)=""(?<value>[^""]*)""", RegexOptions.Compiled);

            using (StringReader reader = new StringReader(content))
            {
                string line;
                string lastCategoria = "";
                string lastNombre = "";
                string lastTvgId = "";
                string lastTvgName = "";
                string lastTvgLogo = "";

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("#EXTINF"))
                    {
                        Match match = regex.Match(line);
                        if (match.Success)
                        {
                            string attrs = match.Groups["attrs"].Value;
                            lastNombre = match.Groups["nombre"].Value.Trim();
                            lastCategoria = "";
                            lastTvgId = "";
                            lastTvgName = "";
                            lastTvgLogo = "";

                            foreach (Match attr in attrRegex.Matches(attrs))
                            {
                                string key = attr.Groups["key"].Value.ToLower();
                                string value = attr.Groups["value"].Value;

                                switch (key)
                                {
                                    case "group-title":
                                        lastCategoria = value;
                                        break;
                                    case "tvg-id":
                                        lastTvgId = value;
                                        break;
                                    case "tvg-name":
                                        lastTvgName = value;
                                        break;
                                    case "tvg-logo":
                                        lastTvgLogo = value;
                                        break;
                                }
                            }
                        }
                    }
                    else if (line.StartsWith("http"))
                    {
                        // Aplicar el proxy si está configurado
                        string url = line;
                        string proxy = ProxyTextBox.Text.Trim();
                        if (!string.IsNullOrEmpty(proxy))
                        {
                            url = proxy + url;
                        }

                        currentList.Entries.Add(new M3UEntry
                        {
                            Categoria = lastCategoria,
                            Nombre = lastNombre,
                            URL = url,
                            OriginalURL = line, // Guardar la URL original
                            TvgId = lastTvgId,
                            TvgName = lastTvgName,
                            TvgLogo = lastTvgLogo
                        });
                    }
                }
            }
        }

        private void UpdateCategories()
        {
            currentList.Categories.Clear();
            var groupedEntries = currentList.Entries.GroupBy(e => e.Categoria);

            foreach (var group in groupedEntries)
            {
                currentList.Categories.Add(new Category
                {
                    Name = group.Key,
                    IsSelected = false,
                    Entries = new ObservableCollection<M3UEntry>(group),
                    EntryCount = group.Count()
                });
            }
            UpdateSelectAllCheckBox();
        }

        private void UpdateGroupsListView()
        {
            // Actualiza la vista de grupos seleccionados
            GroupsListView.ItemsSource = currentList.Categories
                .Where(c => c.IsSelected)
                .Select(c => new {
                    Name = c.Name,
                    EntryCount = c.Entries.Count
                })
                .ToList();
        }

        private void UpdateSelectAllCheckBox()
        {
            SelectAllCheckBox.IsChecked = currentList.Categories.Count > 0 &&
                                        currentList.Categories.All(c => c.IsSelected);
        }

        private void SelectAll(M3UList list, bool selected)
        {
            foreach (var category in list.Categories)
            {
                category.IsSelected = selected;
            }
            UpdateM3UListView();
            UpdateGroupsListView();
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var category in currentList.Categories)
            {
                category.IsSelected = true;
            }
            UpdateM3UListView();
            UpdateGroupsListView();
        }

        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var category in currentList.Categories)
            {
                category.IsSelected = false;
            }
            UpdateM3UListView();
            UpdateGroupsListView();
        }

        private void LoadM3UButton_Click(object sender, RoutedEventArgs e)
        {
            LoadM3UFile(currentList);
        }

        private void LoadM3UFile(M3UList list)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "M3U Files (*.m3u)|*.m3u",
                Title = "Seleccionar archivo M3U"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string content = File.ReadAllText(openFileDialog.FileName);
                ParseM3UContent(content);
                UpdateCategories();
                UpdateM3UListView();
                UpdateGroupsListView();
            }
        }

        private void SaveM3UButton_Click(object sender, RoutedEventArgs e)
        {
            SaveM3UFile(currentList);
        }

        private void SaveM3UFile(M3UList list)
        {
            string listName = list.Name;
            if (string.IsNullOrWhiteSpace(listName))
            {
                MessageBox.Show("Por favor, ingrese un nombre para la lista.");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "M3U Files (*.m3u)|*.m3u",
                Title = "Guardar archivo M3U",
                FileName = $"{listName}.m3u",
                DefaultExt = ".m3u"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                {
                    writer.WriteLine("#EXTM3U");
                    foreach (var category in list.Categories.Where(c => c.IsSelected))
                    {
                        foreach (var entry in category.Entries)
                        {
                            writer.WriteLine($"#EXTINF:-1 tvg-id=\"{entry.TvgId}\" tvg-name=\"{entry.TvgName}\" tvg-logo=\"{entry.TvgLogo}\" group-title=\"{entry.Categoria}\",{entry.Nombre}");
                            // Usar la URL original al guardar
                            writer.WriteLine(entry.OriginalURL);
                        }
                    }
                }

                MessageBox.Show($"Lista M3U guardada exitosamente en: {saveFileDialog.FileName}");
            }
        }
        private void SaveM3UFile2(M3UList list)
        {
            string listName = list.Name;
            if (string.IsNullOrWhiteSpace(listName))
            {
                MessageBox.Show("Por favor, ingrese un nombre para la lista.");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "M3U Files (*.m3u)|*.m3u",
                Title = "Guardar archivo M3U",
                FileName = $"{listName}.m3u",
                DefaultExt = ".m3u"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                {
                    writer.WriteLine("#EXTM3U");
                    foreach (var category in list.Categories.Where(c => c.IsSelected))
                    {
                        foreach (var entry in category.Entries)
                        {
                            writer.WriteLine($"#EXTINF:-1 tvg-id=\"{entry.TvgId}\" tvg-name=\"{entry.TvgName}\" tvg-logo=\"{entry.TvgLogo}\" group-title=\"{entry.Categoria}\",{entry.Nombre}");
                            // Usar la URL original al guardar
                            writer.WriteLine(entry.URL);
                        }
                    }
                }

                MessageBox.Show($"Lista M3U guardada con proxy exitosamente en: {saveFileDialog.FileName}");
            }
        }

        private void CategoryCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateM3UListView();
            UpdateGroupsListView();
            UpdateSelectAllCheckBox();
        }

        private void CategoryCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateM3UListView();
            UpdateGroupsListView();
            UpdateSelectAllCheckBox();
        }

        private void UpdateM3UListView()
        {
            // Filtrar los elementos seleccionados y aplicar orden si existe
            var selectedEntries = currentList.Categories
                .Where(c => c.IsSelected)
                .SelectMany(c => c.Entries)
                .ToList();

            M3UListView.ItemsSource = selectedEntries;
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            MoveCategory(-1);
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            MoveCategory(1);
        }

        private void MoveCategory(int direction)
        {
            // Seleccionar la categoría en la vista de grupos
            var selectedItem = GroupsListView.SelectedItem;
            if (selectedItem == null) return;

            string categoryName = (string)selectedItem.GetType().GetProperty("Name").GetValue(selectedItem);

            int index = -1;
            for (int i = 0; i < currentList.Categories.Count; i++)
            {
                if (currentList.Categories[i].Name == categoryName)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0) return;

            int newIndex = index + direction;
            if (newIndex < 0 || newIndex >= currentList.Categories.Count) return;

            // Intercambiar las categorías
            var temp = currentList.Categories[index];
            currentList.Categories.RemoveAt(index);
            currentList.Categories.Insert(newIndex, temp);

            // Actualizar vistas
            CategoryTreeView.ItemsSource = null;
            CategoryTreeView.ItemsSource = currentList.Categories;
            UpdateM3UListView();
            UpdateGroupsListView();

            // Seleccionar la categoría movida
            GroupsListView.SelectedIndex = newIndex;
        }

        private void ReorderButton_Click(object sender, RoutedEventArgs e)
        {
            // Abrir un diálogo para reorganizar las categorías
            var reorderWindow = new ReorderWindow(currentList.Categories);
            if (reorderWindow.ShowDialog() == true)
            {
                // Actualizar el orden de las categorías
                var orderedCategories = reorderWindow.OrderedCategories;
                currentList.Categories.Clear();
                foreach (var category in orderedCategories)
                {
                    currentList.Categories.Add(category);
                }

                // Actualizar vistas
                CategoryTreeView.ItemsSource = null;
                CategoryTreeView.ItemsSource = currentList.Categories;
                UpdateM3UListView();
                UpdateGroupsListView();
            }
        }

        private async void UploadToDropboxButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(DropboxAccessToken))
            {
                await AuthenticateDropbox();
            }

            if (!string.IsNullOrEmpty(DropboxAccessToken))
            {
                await UploadToDropbox();
            }
        }

        private async Task AuthenticateDropbox()
        {
            var oauth2State = Guid.NewGuid().ToString("N");
            var authorizeUri = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Token, DropboxAppKey,
                new Uri("https://localhost/authorize"), state: oauth2State);

            var webView = new System.Windows.Controls.WebBrowser();
            var authWindow = new Window
            {
                Content = webView,
                Width = 800,
                Height = 600,
                Title = "Autorizar Dropbox",
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            };

            webView.Navigated += (s, e) =>
            {
                if (e.Uri.ToString().StartsWith("https://localhost/authorize"))
                {
                    var result = DropboxOAuth2Helper.ParseTokenFragment(e.Uri);
                    if (result.State == oauth2State)
                    {
                        DropboxAccessToken = result.AccessToken;
                        authWindow.Close();
                    }
                }
            };

            webView.Navigate(authorizeUri);
            authWindow.ShowDialog();
        }

        private async Task UploadToDropbox()
        {
            try
            {
                string listName = currentList.Name;
                if (string.IsNullOrWhiteSpace(listName))
                {
                    MessageBox.Show("Por favor, ingrese un nombre para la lista.");
                    return;
                }

                using (var dbx = new DropboxClient(DropboxAccessToken))
                {
                    using (var memStream = new MemoryStream())
                    {
                        using (var writer = new StreamWriter(memStream))
                        {
                            writer.WriteLine("#EXTM3U");
                            foreach (var category in currentList.Categories.Where(c => c.IsSelected))
                            {
                                foreach (var entry in category.Entries)
                                {
                                    writer.WriteLine($"#EXTINF:-1 tvg-id=\"{entry.TvgId}\" tvg-name=\"{entry.TvgName}\" tvg-logo=\"{entry.TvgLogo}\" group-title=\"{entry.Categoria}\",{entry.Nombre}");
                                    writer.WriteLine(entry.URL);
                                }
                            }
                            writer.Flush();
                            memStream.Position = 0;

                            var fileName = $"{listName}.m3u";
                            var response = await dbx.Files.UploadAsync(
                                $"/{fileName}",
                                WriteMode.Overwrite.Instance,
                                body: memStream);

                            // Obtener el enlace compartido
                            var sharedLinkMetadata = await dbx.Sharing.CreateSharedLinkWithSettingsAsync($"/{fileName}");

                            var directLink = sharedLinkMetadata.Url.Replace("www.dropbox.com", "dl.dropboxusercontent.com");
                            MessageBox.Show($"Lista M3U subida exitosamente a Dropbox.\nEnlace directo: {directLink}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al subir a Dropbox: {ex.Message}");
            }
        }

        private void SaveM3UButton_Click2(object sender, RoutedEventArgs e)
        {
            SaveM3UFile2(currentList);
        }
    }

    public class M3UEntry
    {
        public string Categoria { get; set; }
        public string Nombre { get; set; }
        public string URL { get; set; }
        public string OriginalURL { get; set; } // Para guardar la URL original sin el proxy
        public string TvgId { get; set; }
        public string TvgName { get; set; }
        public string TvgLogo { get; set; }
    }

    public class Category
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; }
        public ObservableCollection<M3UEntry> Entries { get; set; }
        public int EntryCount { get; set; }
    }
}

