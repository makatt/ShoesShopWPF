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
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            // Проверка на пустые поля
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Авторизация через DBHelper
            User user = DBHelper.Authenticate(login, password);

            if (user != null)
            {
                CurrentUser.Instance = user;
                OpenMainWindow();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль!", "Ошибка авторизации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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