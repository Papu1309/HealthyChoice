using System;
using System.Linq;
using System.Windows;
using HealthyChoice.Connect;

namespace HealthyChoice.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Password;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ShowError("Введите логин и пароль");
                    return;
                }

                var user = Connection.entities.Users
                    .FirstOrDefault(u => u.Username == username && u.Password == password);

                if (user != null)
                {
                    App.Current.Properties["CurrentUser"] = user;

                    if (user.Role == "Admin")
                    {
                        AdminWindow adminWindow = new AdminWindow();
                        adminWindow.Show();
                    }
                    else
                    {
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();
                    }

                    this.Close();
                }
                else
                {
                    ShowError("Неверный логин или пароль");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка подключения к базе данных: {ex.Message}");
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.Owner = this;
            registerWindow.ShowDialog();
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }
    }
}