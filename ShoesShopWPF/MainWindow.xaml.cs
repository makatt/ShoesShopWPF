using Npgsql;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShoesShopWPF
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Product> _products = new ObservableCollection<Product>();
        private Product _selectedProduct = null;

        // Контроль открытия только одного окна редактирования
        private ProductEditWindow _currentEditWindow = null;

        public MainWindow()
        {
            InitializeComponent();

            // Подписываемся на события
            ProductsList.SelectionChanged += ProductsList_SelectionChanged;
            ProductsList.MouseDoubleClick += ProductsList_MouseDoubleClick;

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

            txtSearch.IsEnabled = canEdit;
            cmbCategory.IsEnabled = canEdit;
            cmbManufacture.IsEnabled = canEdit;
            cmbSupplier.IsEnabled = canEdit;
            cmbSort.IsEnabled = canEdit;

            btnAdd.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnEdit.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnDelete.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadComboBoxes()
        {
            LoadCombo(cmbCategory, "category", "categoryid", "categoryname");
            LoadCombo(cmbManufacture, "manufacturer", "manufacturerid", "manufacturername");
            LoadCombo(cmbSupplier, "supplier", "supplierid", "suppliername");
        }

        private void LoadCombo(ComboBox cmb, string table, string idField, string nameField)
        {
            string connStr = "host=localhost;port=5432;username=postgres;password=navatak_21;database=postgres";

            using (var conn = new NpgsqlConnection(connStr))
            {
                conn.Open();
                string query = $"SELECT {idField}, {nameField} FROM \"OBYV\".\"{table}\" ORDER BY {nameField}";

                using (var da = new NpgsqlDataAdapter(query, conn))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    DataRow row = dt.NewRow();
                    row[idField] = DBNull.Value;
                    row[nameField] = "Все";
                    dt.Rows.InsertAt(row, 0);

                    cmb.ItemsSource = dt.DefaultView;
                    cmb.DisplayMemberPath = nameField;
                    cmb.SelectedValuePath = idField;
                    cmb.SelectedIndex = 0;
                }
            }
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

            string connStr = "host=localhost;port=5432;username=postgres;password=navatak_21;database=postgres";

            using (var conn = new NpgsqlConnection(connStr))
            {
                conn.Open();

                string query = @"
            SELECT 
                p.productarticlenumber AS Article,
                pn.name AS Name,
                c.categoryname AS Category,
                m.manufacturername AS Manufacturer,
                s.suppliername AS Supplier,
                p.productcost AS Price,
                p.productdiscountamount AS Discount,
                p.productquantityinStock AS Stock,
                p.unitofmeasurement AS Unit,
                p.productdescription AS Description,
                p.productphoto AS Photo
            FROM ""OBYV"".""product"" p
            LEFT JOIN ""OBYV"".""product_names"" pn ON p.productname = pn.nameid
            LEFT JOIN ""OBYV"".""category"" c ON p.productcategory = c.categoryid
            LEFT JOIN ""OBYV"".""manufacturer"" m ON p.productmanufacturer = m.manufacturerid
            LEFT JOIN ""OBYV"".""supplier"" s ON p.productsupplier = s.supplierid
            WHERE 1=1";

                if (!string.IsNullOrWhiteSpace(search))
                    query += " AND (pn.name ILIKE @search OR p.productarticlenumber ILIKE @search)";

                if (cmbCategory.SelectedValue != null && cmbCategory.SelectedValue != DBNull.Value)
                    query += " AND p.productcategory = @category";

                if (cmbManufacture.SelectedValue != null && cmbManufacture.SelectedValue != DBNull.Value)
                    query += " AND p.productmanufacturer = @manufacture";

                if (cmbSupplier.SelectedValue != null && cmbSupplier.SelectedValue != DBNull.Value)
                    query += " AND p.productsupplier = @supplier";

                // Сортировка
                string orderBy = " ORDER BY p.productarticlenumber";
                switch (cmbSort.SelectedIndex)
                {
                    case 1: orderBy = " ORDER BY p.productcost ASC"; break;
                    case 2: orderBy = " ORDER BY p.productcost DESC"; break;
                    case 3: orderBy = " ORDER BY p.productquantityinStock ASC"; break;
                    case 4: orderBy = " ORDER BY p.productquantityinStock DESC"; break;
                }
                query += orderBy;

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    if (!string.IsNullOrWhiteSpace(search))
                        cmd.Parameters.AddWithValue("search", $"%{search}%");

                    if (cmbCategory.SelectedValue != null && cmbCategory.SelectedValue != DBNull.Value)
                        cmd.Parameters.AddWithValue("category", cmbCategory.SelectedValue);

                    if (cmbManufacture.SelectedValue != null && cmbManufacture.SelectedValue != DBNull.Value)
                        cmd.Parameters.AddWithValue("manufacture", cmbManufacture.SelectedValue);

                    if (cmbSupplier.SelectedValue != null && cmbSupplier.SelectedValue != DBNull.Value)
                        cmd.Parameters.AddWithValue("supplier", cmbSupplier.SelectedValue);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var p = new Product
                            {
                                Article = reader.GetString(0),
                                Name = reader.GetString(1),
                                Category = reader.GetString(2),
                                Manufacturer = reader.GetString(3),
                                Supplier = reader.GetString(4),
                                Price = reader.GetDecimal(5),
                                Discount = reader.GetInt32(6),
                                Stock = reader.GetInt32(7),
                                Unit = reader.GetString(8),
                                Description = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                Photo = reader.IsDBNull(10) ? null : reader.GetString(10)
                            };

                            _products.Add(p);
                        }
                    }
                }
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

        //  Фильтры 
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

        //  Админ панель 
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
                              "Предупреждение",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
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

            try
            {
                string connStr = "host=localhost;port=5432;username=postgres;password=navatak_21;database=postgres";

                using (var conn = new NpgsqlConnection(connStr))
                {
                    conn.Open();
                    string sql = @"DELETE FROM ""OBYV"".""product"" WHERE productarticlenumber = @article";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("article", _selectedProduct.Article);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Товар успешно удалён!", "Успех");
                LoadProducts(txtSearch.Text.Trim());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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