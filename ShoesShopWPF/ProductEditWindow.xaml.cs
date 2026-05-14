using System;
using System.Windows;

namespace ShoesShopWPF
{
    public partial class ProductEditWindow : Window
    {
        private string originalArticle = null;

        public ProductEditWindow()
        {
            InitializeComponent();
            LoadComboBoxes();
            Title = "Добавление товара";
        }

        public ProductEditWindow(string article) : this()
        {
            originalArticle = article;
            Title = "Редактирование товара";
            LoadProductData(article);
        }

        private void LoadComboBoxes()
        {
            cmbCategory.ItemsSource = DBHelper.GetLookup("category", "categoryid", "categoryname").DefaultView;
            cmbCategory.DisplayMemberPath = "categoryname";
            cmbCategory.SelectedValuePath = "categoryid";

            cmbManufacturer.ItemsSource = DBHelper.GetLookup("manufacturer", "manufacturerid", "manufacturername").DefaultView;
            cmbManufacturer.DisplayMemberPath = "manufacturername";
            cmbManufacturer.SelectedValuePath = "manufacturerid";

            cmbSupplier.ItemsSource = DBHelper.GetLookup("supplier", "supplierid", "suppliername").DefaultView;
            cmbSupplier.DisplayMemberPath = "suppliername";
            cmbSupplier.SelectedValuePath = "supplierid";

            // Единицы измерения
            cmbUnit.Items.Add("шт");
            cmbUnit.Items.Add("пара");
            cmbUnit.Items.Add("коробка");
            cmbUnit.Items.Add("упаковка");
            cmbUnit.SelectedIndex = 0;
        }

        private void LoadProductData(string article)
        {
            string connStr = "host=localhost;port=5432;username=postgres;password=navatak_21;database=postgres";

            using (var conn = new Npgsql.NpgsqlConnection(connStr))
            {
                conn.Open();
                string query = @"
                    SELECT p.productarticlenumber, pn.name, p.productcategory, p.productmanufacturer,
                           p.productsupplier, p.productcost, p.productdiscountamount,
                           p.productquantityinStock, p.productdescription, p.unitofmeasurement
                    FROM ""OBYV"".""product"" p
                    LEFT JOIN ""OBYV"".""product_names"" pn ON p.productname = pn.nameid
                    WHERE p.productarticlenumber = @article";

                using (var cmd = new Npgsql.NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("article", article);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtArticle.Text = reader.GetString(0);
                            txtName.Text = reader.GetString(1);

                            cmbCategory.SelectedValue = reader.IsDBNull(2) ? null : reader.GetValue(2);
                            cmbManufacturer.SelectedValue = reader.IsDBNull(3) ? null : reader.GetValue(3);
                            cmbSupplier.SelectedValue = reader.IsDBNull(4) ? null : reader.GetValue(4);

                            txtPrice.Text = reader.GetDecimal(5).ToString("0.00");
                            txtDiscount.Text = reader.GetInt32(6).ToString();
                            txtStock.Text = reader.GetInt32(7).ToString();
                            txtDescription.Text = reader.IsDBNull(8) ? "" : reader.GetString(8);
                            cmbUnit.Text = reader.IsDBNull(9) ? "шт" : reader.GetString(9);
                        }
                    }
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtArticle.Text) || string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Артикул и Название — обязательные поля!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrice.Text, out decimal price) ||
                !int.TryParse(txtDiscount.Text, out int discount) ||
                !int.TryParse(txtStock.Text, out int stock))
            {
                MessageBox.Show("Некорректные числовые значения в полях Цена, Скидка или На складе!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var data = new ProductEditData
                {
                    Article = txtArticle.Text.Trim(),
                    Name = txtName.Text.Trim(),
                    CategoryId = cmbCategory.SelectedValue,
                    ManufacturerId = cmbManufacturer.SelectedValue,
                    SupplierId = cmbSupplier.SelectedValue,
                    Price = price,
                    Discount = discount,
                    Stock = stock,
                    Unit = cmbUnit.Text,
                    Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text
                };

                bool isNew = originalArticle == null;
                DBHelper.SaveProduct(data, isNew);

                MessageBox.Show(isNew ? "Товар успешно добавлен!" : "Товар успешно обновлён!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}