using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;

namespace ShoesShopWPF
{
    public static class DBHelper
    {
        private const string ConnString = "host=localhost;port=5432;username=postgres;password=navatak_21;database=postgres";

        private static NpgsqlConnection CreateConnection()
        {
            var conn = new NpgsqlConnection(ConnString);
            conn.Open();
            return conn;
        }

        #region Авторизация
        public static User Authenticate(string login, string password)
        {
            using (var conn = CreateConnection())
            {
                const string query = @"
                    SELECT u.userid, r.rolename, u.username, u.userpatronymic 
                    FROM ""OBYV"".""User"" u 
                    JOIN ""OBYV"".""role"" r ON u.userrole = r.roleid 
                    WHERE u.userlogin = @login AND u.userpassword = @pass";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("login", login.Trim());
                    cmd.Parameters.AddWithValue("pass", password);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null;

                        string fullName = reader.GetString(2);
                        if (!reader.IsDBNull(3))
                            fullName += " " + reader.GetString(3);

                        return new User
                        {
                            Id = reader.GetInt32(0),
                            Role = reader.GetString(1),
                            FullName = fullName.Trim()
                        };
                    }
                }
            }
        }
        #endregion

        #region Получение товаров
        public static List<Product> GetProducts(string search = "",
            object categoryId = null,
            object manufacturerId = null,
            object supplierId = null,
            int sortIndex = 0)
        {
            var products = new List<Product>();

            using (var conn = CreateConnection())
            {
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

                if (categoryId != null && categoryId != DBNull.Value)
                    query += " AND p.productcategory = @cat";
                if (manufacturerId != null && manufacturerId != DBNull.Value)
                    query += " AND p.productmanufacturer = @man";
                if (supplierId != null && supplierId != DBNull.Value)
                    query += " AND p.productsupplier = @sup";

                // Сортировка
                switch (sortIndex)
                {
                    case 1: query += " ORDER BY p.productcost ASC"; break;
                    case 2: query += " ORDER BY p.productcost DESC"; break;
                    case 3: query += " ORDER BY p.productquantityinStock ASC"; break;
                    case 4: query += " ORDER BY p.productquantityinStock DESC"; break;
                    default: query += " ORDER BY p.productarticlenumber"; break;
                }

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    if (!string.IsNullOrWhiteSpace(search))
                        cmd.Parameters.AddWithValue("search", "%" + search + "%");

                    if (categoryId != null && categoryId != DBNull.Value)
                        cmd.Parameters.AddWithValue("cat", categoryId);
                    if (manufacturerId != null && manufacturerId != DBNull.Value)
                        cmd.Parameters.AddWithValue("man", manufacturerId);
                    if (supplierId != null && supplierId != DBNull.Value)
                        cmd.Parameters.AddWithValue("sup", supplierId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(MapProduct(reader));
                        }
                    }
                }
            }

            return products;
        }

        private static Product MapProduct(NpgsqlDataReader r)
        {
            return new Product
            {
                Article = r.GetString(0),
                Name = r.GetString(1),
                Category = r.GetString(2),
                Manufacturer = r.GetString(3),
                Supplier = r.GetString(4),
                Price = r.GetDecimal(5),
                Discount = r.GetInt32(6),
                Stock = r.GetInt32(7),
                Unit = r.GetString(8),
                Description = r.IsDBNull(9) ? "" : r.GetString(9),
                Photo = r.IsDBNull(10) ? null : r.GetString(10)
            };
        }
        #endregion

        #region Комбобоксы (справочники)
        public static DataTable GetLookup(string table, string idField, string nameField)
        {
            using (var conn = CreateConnection())
            {
                string query = $"SELECT {idField}, {nameField} FROM \"OBYV\".\"{table}\" ORDER BY {nameField}";

                using (var da = new NpgsqlDataAdapter(query, conn))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    DataRow row = dt.NewRow();
                    row[idField] = DBNull.Value;
                    row[nameField] = "Все";
                    dt.Rows.InsertAt(row, 0);

                    return dt;
                }
            }
        }
        #endregion

        #region CRUD Товаров
        public static void SaveProduct(ProductEditData data, bool isNew)
        {
            using (var conn = CreateConnection())
            {
                int nameId = GetOrCreateProductName(conn, data.Name);

                string sql = isNew ?
                    @"INSERT INTO ""OBYV"".""product"" 
                      (productarticlenumber, productname, productcategory, productmanufacturer, 
                       productsupplier, productcost, productdiscountamount, productquantityinStock, 
                       productdescription, unitofmeasurement, productphoto)
                      VALUES (@article, @nameid, @cat, @manuf, @sup, @price, @discount, 
                              @stock, @desc, @unit, @photo)" :

                    @"UPDATE ""OBYV"".""product"" SET 
                        productname = @nameid, 
                        productcategory = @cat, 
                        productmanufacturer = @manuf,
                        productsupplier = @sup, 
                        productcost = @price, 
                        productdiscountamount = @discount,
                        productquantityinStock = @stock, 
                        productdescription = @desc,
                        unitofmeasurement = @unit, 
                        productphoto = @photo
                      WHERE productarticlenumber = @article";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    FillProductParameters(cmd, data, nameId, isNew ? null : data.Article);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteProduct(string article)
        {
            using (var conn = CreateConnection())
            {
                using (var cmd = new NpgsqlCommand(
                    @"DELETE FROM ""OBYV"".""product"" WHERE productarticlenumber = @article", conn))
                {
                    cmd.Parameters.AddWithValue("article", article);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static int GetOrCreateProductName(NpgsqlConnection conn, string name)
        {
            using (var cmd = new NpgsqlCommand(
                "SELECT nameid FROM \"OBYV\".\"product_names\" WHERE name = @name", conn))
            {
                cmd.Parameters.AddWithValue("name", name);
                var result = cmd.ExecuteScalar();
                if (result != null)
                    return Convert.ToInt32(result);
            }

            using (var cmd = new NpgsqlCommand(
                "INSERT INTO \"OBYV\".\"product_names\" (name) VALUES (@name) RETURNING nameid", conn))
            {
                cmd.Parameters.AddWithValue("name", name);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static void FillProductParameters(NpgsqlCommand cmd, ProductEditData data, int nameId, string article = null)
        {
            cmd.Parameters.AddWithValue("article", data.Article);
            cmd.Parameters.AddWithValue("nameid", nameId);
            cmd.Parameters.AddWithValue("cat", data.CategoryId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("manuf", data.ManufacturerId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("sup", data.SupplierId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("price", data.Price);
            cmd.Parameters.AddWithValue("discount", data.Discount);
            cmd.Parameters.AddWithValue("stock", data.Stock);

            cmd.Parameters.AddWithValue("desc", string.IsNullOrWhiteSpace(data.Description)
                ? (object)DBNull.Value
                : data.Description);

            cmd.Parameters.AddWithValue("unit", data.Unit);
            cmd.Parameters.AddWithValue("photo", string.IsNullOrEmpty(data.Photo)
                ? (object)DBNull.Value
                : data.Photo);
        }
        #endregion
    }
}