using HealthyChoice.Connect;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HealthyChoice.Views
{
    public partial class AddProductDialog : Window
    {
        public Products NewProduct { get; private set; }

        public AddProductDialog()
        {
            InitializeComponent();

            // Устанавливаем категорию по умолчанию
            if (cmbCategory != null && cmbCategory.Items.Count > 0)
            {
                cmbCategory.SelectedIndex = 0;
            }
        }

        private void Number_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка на пустые обязательные поля
                if (string.IsNullOrWhiteSpace(txtProductName.Text))
                {
                    ShowError("Введите название товара");
                    txtProductName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPrice.Text))
                {
                    ShowError("Введите цену товара");
                    txtPrice.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtStock.Text))
                {
                    ShowError("Введите количество товара");
                    txtStock.Focus();
                    return;
                }

                // Парсинг цены
                if (!decimal.TryParse(txtPrice.Text, out decimal price))
                {
                    ShowError("Введите корректную цену (число)");
                    txtPrice.Focus();
                    return;
                }

                // Парсинг количества
                if (!int.TryParse(txtStock.Text, out int stock))
                {
                    ShowError("Введите корректное количество (целое число)");
                    txtStock.Focus();
                    return;
                }

                // Валидация цены
                if (price <= 0)
                {
                    ShowError("Цена должна быть больше 0");
                    txtPrice.Focus();
                    return;
                }

                // Валидация количества
                if (stock < 0)
                {
                    ShowError("Количество не может быть отрицательным");
                    txtStock.Focus();
                    return;
                }

                // Получаем категорию
                string category = "Разное";
                if (cmbCategory.SelectedItem != null)
                {
                    category = (cmbCategory.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Разное";
                }

                // Создаем новый товар
                NewProduct = new Products
                {
                    ProductName = txtProductName.Text.Trim(),
                    Price = price,
                    StockQuantity = stock,
                    Category = category,
                    Description = txtDescription.Text?.Trim() ?? ""
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowError(string message)
        {
            if (txtError != null)
            {
                txtError.Text = message;
                txtError.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}