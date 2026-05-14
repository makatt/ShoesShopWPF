using Npgsql;
using Microsoft.Win32;
using System;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace ShoesShopWPF
{
    public partial class ProductEditWindow : Window
    {
        private string originalArticle = null;
        private string currentPhotoFileName = null;

        public ProductEditWindow()
        {
            InitializeComponent();
            LoadComboBoxes();
        }

        public ProductEditWindow(string article) : this()
        {
            originalArticle = article;
            Title = "Редактирование товара";
            LoadProductData(article);
        }

        private void LoadComboBoxes()
        {
            LoadCombo(cmbCategory, "category", "categoryid", "categoryname");
            LoadCombo(cmbManufacturer, "manufacturer", "manufacturerid", "manufacturername");
            LoadCombo(cmbSupplier, "supplier", "supplierid", "suppliername");

            
            cmbUnit.Items.Add("шт");
            cmbUnit.Items.Add("пара");
            cmbUnit.Items.Add("коробка");
            cmbUnit.Items.Add("упаковка");
            cmbUnit.SelectedIndex = 0;
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

                    cmb.DisplayMemberPath = nameField;
                    cmb.SelectedValuePath = idField;
                    cmb.ItemsSource = dt.DefaultView;
                }
            }
        }

        private void LoadProductData(string article)
        {
            string connStr = "host=localhost;port=5432;username=postgres;password=navatak_21;database=postgres";

            using (var conn = new NpgsqlConnection(connStr))
            {
                conn.Open();
                string query = @"
                    SELECT p.productarticlenumber, pn.name, p.productcategory, p.productmanufacturer,
                           p.productsupplier, p.productcost, p.productdiscountamount,
                           p.productquantityinStock, p.productdescription, p.unitofmeasurement, p.productphoto
                    FROM ""OBYV"".""product"" p
                    LEFT JOIN ""OBYV"".""product_names"" pn ON p.productname = pn.nameid
                    WHERE p.productarticlenumber = @article";

                using (var cmd = new NpgsqlCommand(query, conn))
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

                            currentPhotoFileName = reader.IsDBNull(10) ? null : reader.GetString(10);
                            LoadCurrentPhoto();
                        }
                    }
                }
            }
        }

        private void LoadCurrentPhoto()
        {
            if (string.IsNullOrEmpty(currentPhotoFileName)) return;

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", currentPhotoFileName);
            if (File.Exists(path))
                imgPhoto.Source = new BitmapImage(new Uri(path));
        }

        private void btnLoadPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp" };

            if (ofd.ShowDialog() == true)
            {
                
                    string ext = Path.GetExtension(ofd.FileName).ToLower();
                    string newFileName = txtArticle.Text.Trim() + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ext;

                    string targetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", newFileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

                    File.Copy(ofd.FileName, targetPath, true);

                    currentPhotoFileName = newFileName;
                    imgPhoto.Source = new BitmapImage(new Uri(targetPath));
                
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtArticle.Text) || string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Артикул и Название обязательные поля!");
                return;
            }

            try
            {
                string connStr = "host=localhost;port=5432;username=postgres;password=navatak_21;database=postgres";

                using (var conn = new NpgsqlConnection(connStr))
                {
                    conn.Open();
                    int nameId = GetOrCreateProductName(conn, txtName.Text);

                    if (originalArticle == null) // Добавление
                    {
                        string sql = @"
                            INSERT INTO ""OBYV"".""product""
                            (productarticlenumber, productname, productcategory, productmanufacturer,
                             productsupplier, productcost, productdiscountamount, productquantityinStock,
                             productdescription, unitofmeasurement, productphoto)
                            VALUES (@article, @nameid, @cat, @manuf, @sup, @price, @discount,
                                    @stock, @desc, @unit, @photo)";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            FillParameters(cmd, nameId);
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Товар успешно добавлен!");
                    }
                    else // Редактирование
                    {
                        string sql = @"
                            UPDATE ""OBYV"".""product"" SET
                                productname = @nameid, productcategory = @cat, productmanufacturer = @manuf,
                                productsupplier = @sup, productcost = @price, productdiscountamount = @discount,
                                productquantityinStock = @stock, productdescription = @desc,
                                unitofmeasurement = @unit, productphoto = @photo
                            WHERE productarticlenumber = @article";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            FillParameters(cmd, nameId);
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Товар успешно обновлён!");
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillParameters(NpgsqlCommand cmd, int nameId)
        {
            cmd.Parameters.AddWithValue("article", txtArticle.Text.Trim());
            cmd.Parameters.AddWithValue("nameid", nameId);
            cmd.Parameters.AddWithValue("cat", cmbCategory.SelectedValue ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("manuf", cmbManufacturer.SelectedValue ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("sup", cmbSupplier.SelectedValue ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("price", decimal.Parse(txtPrice.Text));
            cmd.Parameters.AddWithValue("discount", int.Parse(txtDiscount.Text));
            cmd.Parameters.AddWithValue("stock", int.Parse(txtStock.Text));

            if (string.IsNullOrWhiteSpace(txtDescription.Text))
                cmd.Parameters.AddWithValue("desc", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("desc", txtDescription.Text);

            cmd.Parameters.AddWithValue("unit", cmbUnit.Text);
            cmd.Parameters.AddWithValue("photo", string.IsNullOrEmpty(currentPhotoFileName) ? (object)DBNull.Value : currentPhotoFileName);
        }

        private int GetOrCreateProductName(NpgsqlConnection conn, string name)
        {
            using (var cmd = new NpgsqlCommand("SELECT nameid FROM \"OBYV\".\"product_names\" WHERE name = @name", conn))
            {
                cmd.Parameters.AddWithValue("name", name);
                var result = cmd.ExecuteScalar();
                if (result != null) return Convert.ToInt32(result);
            }

            using (var cmd = new NpgsqlCommand("INSERT INTO \"OBYV\".\"product_names\" (name) VALUES (@name) RETURNING nameid", conn))
            {
                cmd.Parameters.AddWithValue("name", name);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}