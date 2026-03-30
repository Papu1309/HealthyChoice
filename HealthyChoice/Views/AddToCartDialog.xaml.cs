using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using HealthyChoice.Connect;

namespace HealthyChoice.Views
{
    public partial class AddToCartDialog : Window
    {
        private Products product;
        private int quantity = 1;
        private decimal additionalCost = 0;

        public int Quantity { get; private set; }
        public decimal TotalPrice { get; private set; }
        public bool GiftWrap { get; private set; }
        public bool ExpressDelivery { get; private set; }
        public bool Insurance { get; private set; }

        public AddToCartDialog(Products product)
        {
            InitializeComponent();

            if (product == null)
            {
                MessageBox.Show("Ошибка: товар не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
                return;
            }

            this.product = product;

            if (txtProductName != null)
                txtProductName.Text = product.ProductName;

            if (txtProductPrice != null)
                txtProductPrice.Text = $"Цена: {product.Price:C}";

            if (txtProductStock != null)
                txtProductStock.Text = $"В наличии: {product.StockQuantity} шт.";

            UpdateTotal();
        }

        private void UpdateTotal()
        {
            try
            {
                if (product == null) return;

                decimal productTotal = product.Price * quantity;
                TotalPrice = productTotal + additionalCost;

                if (txtTotal != null)
                    txtTotal.Text = TotalPrice.ToString("C");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в UpdateTotal: {ex.Message}");
            }
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (product == null) return;

                if (quantity < product.StockQuantity)
                {
                    quantity++;
                    if (txtQuantity != null)
                        txtQuantity.Text = quantity.ToString();
                    UpdateTotal();
                }
                else
                {
                    MessageBox.Show($"Максимальное количество: {product.StockQuantity} шт.",
                                  "Ограничение", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (quantity > 1)
                {
                    quantity--;
                    if (txtQuantity != null)
                        txtQuantity.Text = quantity.ToString();
                    UpdateTotal();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Quantity_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void Quantity_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (product == null) return;

                if (txtQuantity == null) return;

                if (int.TryParse(txtQuantity.Text, out int newQuantity))
                {
                    if (newQuantity >= 1 && newQuantity <= product.StockQuantity)
                    {
                        quantity = newQuantity;
                        UpdateTotal();
                        if (txtError != null)
                            txtError.Visibility = Visibility.Collapsed;
                    }
                    else if (newQuantity > product.StockQuantity)
                    {
                        if (txtError != null)
                        {
                            txtError.Text = $"Максимальное количество: {product.StockQuantity} шт.";
                            txtError.Visibility = Visibility.Visible;
                        }
                    }
                    else if (newQuantity < 1)
                    {
                        if (txtError != null)
                        {
                            txtError.Text = "Количество должно быть не менее 1";
                            txtError.Visibility = Visibility.Visible;
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(txtQuantity.Text))
                {
                    if (txtError != null)
                    {
                        txtError.Text = "Введите корректное число";
                        txtError.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в Quantity_TextChanged: {ex.Message}");
                if (txtError != null)
                {
                    txtError.Text = "Ошибка при вводе количества";
                    txtError.Visibility = Visibility.Visible;
                }
            }
        }

        private void GiftWrap_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (chkGiftWrap != null)
                    GiftWrap = chkGiftWrap.IsChecked == true;

                additionalCost = (GiftWrap ? 150 : 0) + (ExpressDelivery ? 300 : 0) + (Insurance ? 200 : 0);
                UpdateTotal();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в GiftWrap_Changed: {ex.Message}");
            }
        }

        private void Express_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (chkExpress != null)
                    ExpressDelivery = chkExpress.IsChecked == true;

                additionalCost = (GiftWrap ? 150 : 0) + (ExpressDelivery ? 300 : 0) + (Insurance ? 200 : 0);
                UpdateTotal();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в Express_Changed: {ex.Message}");
            }
        }

        private void Insurance_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (chkInsurance != null)
                    Insurance = chkInsurance.IsChecked == true;

                additionalCost = (GiftWrap ? 150 : 0) + (ExpressDelivery ? 300 : 0) + (Insurance ? 200 : 0);
                UpdateTotal();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в Insurance_Changed: {ex.Message}");
            }
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (product == null)
                {
                    MessageBox.Show("Ошибка: данные товара не найдены", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                    Close();
                    return;
                }

                if (quantity >= 1 && quantity <= product.StockQuantity)
                {
                    Quantity = quantity;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    if (txtError != null)
                    {
                        txtError.Text = "Проверьте правильность выбранного количества";
                        txtError.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (product == null)
                {
                    MessageBox.Show("Ошибка: данные товара не загружены", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке окна: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
            }
        }
    }
}