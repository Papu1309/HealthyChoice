using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HealthyChoice.Connect;

namespace HealthyChoice.Views
{
    public partial class EditProductDialog : Window
    {
        private Products product;

        public EditProductDialog(Products product)
        {
            InitializeComponent();
            this.product = product;

            txtName.Text = product.ProductName;
            txtPrice.Text = product.Price.ToString();
            txtStock.Text = product.StockQuantity.ToString();
            txtDescription.Text = product.Description;

            if (!string.IsNullOrEmpty(product.Category))
            {
                for (int i = 0; i < cmbCategory.Items.Count; i++)
                {
                    var item = cmbCategory.Items[i] as ComboBoxItem;
                    if (item != null && item.Content.ToString() == product.Category)
                    {
                        cmbCategory.SelectedIndex = i;
                        break;
                    }
                }
            }

            if (cmbCategory.SelectedIndex == -1)
            {
                cmbCategory.Text = product.Category;
            }
        }

        private void Number_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    ShowError("Введите название товара");
                    txtName.Focus();
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

                if (!decimal.TryParse(txtPrice.Text, out decimal price))
                {
                    ShowError("Введите корректную цену (число)");
                    txtPrice.Focus();
                    return;
                }

                if (!int.TryParse(txtStock.Text, out int stock))
                {
                    ShowError("Введите корректное количество (целое число)");
                    txtStock.Focus();
                    return;
                }

                if (price <= 0)
                {
                    ShowError("Цена должна быть больше 0");
                    txtPrice.Focus();
                    return;
                }

                if (stock < 0)
                {
                    ShowError("Количество не может быть отрицательным");
                    txtStock.Focus();
                    return;
                }

                string category = "Разное";
                if (cmbCategory.SelectedItem != null)
                {
                    category = (cmbCategory.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Разное";
                }
                else if (!string.IsNullOrEmpty(cmbCategory.Text))
                {
                    category = cmbCategory.Text;
                }

                product.ProductName = txtName.Text.Trim();
                product.Price = price;
                product.StockQuantity = stock;
                product.Category = category;
                product.Description = txtDescription.Text?.Trim() ?? "";

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