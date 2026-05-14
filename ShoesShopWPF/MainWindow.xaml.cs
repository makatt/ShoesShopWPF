using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShoesShopWPF
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Product> _products = new ObservableCollection<Product>();
        private Product _selectedProduct = null;
        private ProductEditWindow _currentEditWindow = null;

        public MainWindow()
        {
            InitializeComponent();
            ProductsList.SelectionChanged += ProductsList_SelectionChanged;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserInfo();
            LoadComboBoxes();
            LoadSortOptions();
            LoadProducts();
        }

        private void LoadUserInfo()
        {
            var user = CurrentUser.Instance;
            if (user == null) return;

            lblUserInfo.Text = $"{user.FullName} ({user.Role})";

            bool isAdmin = user.Role == "Администратор";
            bool canEdit = !(user.Role == "Гость" || user.Role == "Авторизированный клиент");

            // Блокировка фильтров для гостей
            txtSearch.IsEnabled = canEdit;
            cmbCategory.IsEnabled = canEdit;
            cmbManufacture.IsEnabled = canEdit;
            cmbSupplier.IsEnabled = canEdit;
            cmbSort.IsEnabled = canEdit;

            // Видимость кнопок администратора
            btnAdd.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnEdit.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnDelete.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadComboBoxes()
        {
            cmbCategory.ItemsSource = DBHelper.GetLookup("category", "categoryid", "categoryname").DefaultView;
            cmbCategory.DisplayMemberPath = "categoryname";
            cmbCategory.SelectedValuePath = "categoryid";
            cmbCategory.SelectedIndex = 0;

            cmbManufacture.ItemsSource = DBHelper.GetLookup("manufacturer", "manufacturerid", "manufacturername").DefaultView;
            cmbManufacture.DisplayMemberPath = "manufacturername";
            cmbManufacture.SelectedValuePath = "manufacturerid";
            cmbManufacture.SelectedIndex = 0;

            cmbSupplier.ItemsSource = DBHelper.GetLookup("supplier", "supplierid", "suppliername").DefaultView;
            cmbSupplier.DisplayMemberPath = "suppliername";
            cmbSupplier.SelectedValuePath = "supplierid";
            cmbSupplier.SelectedIndex = 0;
        }

        private void LoadSortOptions()
        {
            cmbSort.Items.Clear();
            cmbSort.Items.Add("По умолчанию");
            cmbSort.Items.Add("По цене (возрастание)");
            cmbSort.Items.Add("По цене (убывание)");
            cmbSort.Items.Add("По наличию (возрастание)");
            cmbSort.Items.Add("По наличию (убывание)");
            cmbSort.SelectedIndex = 0;
        }

        private void LoadProducts(string search = "")
        {
            _products.Clear();

            object catId = cmbCategory.SelectedValue;
            object manId = cmbManufacture.SelectedValue;
            object supId = cmbSupplier.SelectedValue;
            int sortIndex = cmbSort.SelectedIndex;

            var list = DBHelper.GetProducts(search, catId, manId, supId, sortIndex);

            foreach (var product in list)
            {
                _products.Add(product);
            }

            ProductsList.ItemsSource = _products;
        }

        private void ProductsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedProduct = ProductsList.SelectedItem as Product;
        }

        private void ProductsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_selectedProduct != null && btnEdit.Visibility == Visibility.Visible)
            {
                btnEdit_Click(sender, null);
            }
        }

        // ====================== Фильтры ======================
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadProducts(txtSearch.Text.Trim());
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            LoadProducts(txtSearch.Text.Trim());
        }

        private void cmbSort_SelectionChanged(object sender, RoutedEventArgs e)
        {
            LoadProducts(txtSearch.Text.Trim());
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            cmbCategory.SelectedIndex = 0;
            cmbManufacture.SelectedIndex = 0;
            cmbSupplier.SelectedIndex = 0;
            cmbSort.SelectedIndex = 0;
            txtSearch.Clear();
            LoadProducts();
        }

        // ====================== Админ панель ======================
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!CanOpenEditWindow()) return;

            var win = new ProductEditWindow();
            _currentEditWindow = win;
            win.Closed += EditWindow_Closed;

            if (win.ShowDialog() == true)
                LoadProducts(txtSearch.Text.Trim());
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null)
            {
                MessageBox.Show("Выберите товар для редактирования", "Внимание");
                return;
            }

            if (!CanOpenEditWindow()) return;

            var win = new ProductEditWindow(_selectedProduct.Article);
            _currentEditWindow = win;
            win.Closed += EditWindow_Closed;

            if (win.ShowDialog() == true)
                LoadProducts(txtSearch.Text.Trim());
        }

        private bool CanOpenEditWindow()
        {
            if (_currentEditWindow != null && _currentEditWindow.IsLoaded)
            {
                MessageBox.Show("Окно редактирования уже открыто!\n\nЗакройте текущее окно перед открытием нового.",
                              "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                _currentEditWindow.Activate();
                return false;
            }
            return true;
        }

        private void EditWindow_Closed(object sender, EventArgs e)
        {
            _currentEditWindow = null;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null)
            {
                MessageBox.Show("Выберите товар для удаления", "Внимание");
                return;
            }

            if (MessageBox.Show($"Удалить товар «{_selectedProduct.Name}»?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                return;

            DBHelper.DeleteProduct(_selectedProduct.Article);

            MessageBox.Show("Товар успешно удалён!", "Успех");
            LoadProducts(txtSearch.Text.Trim());
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти из системы?", "Выход", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
                System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
        }
    }
}