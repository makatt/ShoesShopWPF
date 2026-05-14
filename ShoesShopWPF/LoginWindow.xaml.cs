using Npgsql;
using System.Windows;

namespace ShoesShopWPF
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string connStr = "host=localhost;port=5432;username=postgres;password=navatak_21;database=postgres";

            using (var conn = new NpgsqlConnection(connStr))
            {
                conn.Open();
                string query = @"
                    SELECT u.userid, r.rolename, u.username, u.userpatronymic 
                    FROM ""OBYV"".""User"" u 
                    JOIN ""OBYV"".""role"" r ON u.userrole = r.roleid 
                    WHERE u.userlogin = @login AND u.userpassword = @pass";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("login", txtLogin.Text.Trim());
                    cmd.Parameters.AddWithValue("pass", txtPassword.Password);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string fullName = reader.GetString(2);
                            if (!reader.IsDBNull(3)) fullName += " " + reader.GetString(3);

                            CurrentUser.Instance = new User
                            {
                                Id = reader.GetInt32(0),
                                Role = reader.GetString(1),
                                FullName = fullName.Trim()
                            };

                            OpenMainWindow();
                        }
                        else
                        {
                            MessageBox.Show("Неверный логин или пароль!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
        }

        private void btnGuest_Click(object sender, RoutedEventArgs e)
        {
            CurrentUser.Instance = new User
            {
                Id = 0,
                Role = "Гость",
                FullName = "Гость"
            };
            OpenMainWindow();
        }

        private void OpenMainWindow()
        {
            MainWindow main = new MainWindow();
            main.Show();
            this.Close();
        }
    }
}