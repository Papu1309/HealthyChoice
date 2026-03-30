using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using HealthyChoice.Connect;

namespace HealthyChoice.Views
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры, + и -
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9+\-]+$");
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Password;
                string confirmPassword = txtConfirmPassword.Password;
                string fullName = txtFullName.Text.Trim();
                string email = txtEmail.Text.Trim();
                string phone = txtPhone.Text.Trim();

                // Валидация
                if (string.IsNullOrEmpty(username))
                {
                    ShowError("Введите логин");
                    txtUsername.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(password))
                {
                    ShowError("Введите пароль");
                    txtPassword.Focus();
                    return;
                }

                if (password.Length < 3)
                {
                    ShowError("Пароль должен содержать не менее 3 символов");
                    txtPassword.Focus();
                    return;
                }

                if (password != confirmPassword)
                {
                    ShowError("Пароли не совпадают");
                    txtConfirmPassword.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(fullName))
                {
                    ShowError("Введите ФИО");
                    txtFullName.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(email))
                {
                    ShowError("Введите Email");
                    txtEmail.Focus();
                    return;
                }

                if (!IsValidEmail(email))
                {
                    ShowError("Введите корректный email адрес (пример: user@mail.ru)");
                    txtEmail.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(phone))
                {
                    ShowError("Введите номер телефона");
                    txtPhone.Focus();
                    return;
                }

                if (!IsValidPhone(phone))
                {
                    ShowError("Введите корректный номер телефона (пример: +79161234567 или 89161234567)");
                    txtPhone.Focus();
                    return;
                }

                // Проверка существования пользователя
                var existingUser = Connection.entities.Users
                    .FirstOrDefault(u => u.Username == username);

                if (existingUser != null)
                {
                    ShowError("Пользователь с таким логином уже существует");
                    txtUsername.Focus();
                    return;
                }

                // Проверка email
                var existingEmail = Connection.entities.Users
                    .FirstOrDefault(u => u.Email == email);

                if (existingEmail != null)
                {
                    ShowError("Пользователь с таким email уже существует");
                    txtEmail.Focus();
                    return;
                }

                // Создание нового пользователя
                var newUser = new Users
                {
                    Username = username,
                    Password = password,
                    FullName = fullName,
                    Email = email,
                    Phone = phone,
                    Role = "User",
                    CreatedDate = DateTime.Now
                };

                Connection.entities.Users.Add(newUser);
                Connection.entities.SaveChanges();

                MessageBox.Show("✅ Регистрация успешно завершена!\n\n" +
                              $"👤 Логин: {username}\n" +
                              $"📧 Email: {email}\n\n" +
                              "Теперь вы можете войти в систему, используя свои учетные данные.",
                              "Успех",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при регистрации: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            // Убираем все нецифровые символы для проверки
            string digits = Regex.Replace(phone, @"[^\d]", "");
            // Проверяем длину (10-12 цифр)
            return digits.Length >= 10 && digits.Length <= 12;
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }
    }
}