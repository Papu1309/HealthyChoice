using HealthyChoice.Connect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HealthyChoice.Views
{
    public partial class PaymentWindow : Window
    {
        private List<CartItem> cart;
        private Users currentUser;

        public PaymentWindow(List<CartItem> cart, Users user)
        {
            InitializeComponent();
            this.cart = cart;
            this.currentUser = user;

            // Инициализация даты
            if (dpPickupDate != null)
            {
                dpPickupDate.SelectedDate = DateTime.Now.AddDays(1);
            }

            UpdateOrderInfo();

            // Убедимся что панель карты видима по умолчанию
            if (cardDataPanel != null)
            {
                cardDataPanel.Visibility = Visibility.Visible;
            }
        }

        private void PaymentMethod_Changed(object sender, RoutedEventArgs e)
        {
            if (cardDataPanel != null)
            {
                if (rbCard.IsChecked == true)
                {
                    cardDataPanel.Visibility = Visibility.Visible;
                }
                else if (rbCash.IsChecked == true)
                {
                    cardDataPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void UpdateOrderInfo()
        {
            if (cart != null && txtOrderInfo != null)
            {
                decimal total = cart.Sum(c => c.TotalPrice);
                int totalItems = cart.Sum(c => c.Quantity);
                string info = $"🛍️ Товаров в корзине: {totalItems} шт.\n";
                info += $"💰 Сумма: {total:C}\n";
                info += $"📦 Позиций: {cart.Count}";
                txtOrderInfo.Text = info;
            }
        }

        private void CardNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void CardNumber_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                // Убираем все пробелы
                string number = textBox.Text.Replace(" ", "");
                if (number.Length > 16)
                {
                    number = number.Substring(0, 16);
                }

                // Форматируем номер карты группами по 4 цифры
                if (number.Length > 0)
                {
                    string formatted = "";
                    for (int i = 0; i < number.Length; i++)
                    {
                        if (i > 0 && i % 4 == 0)
                        {
                            formatted += " ";
                        }
                        formatted += number[i];
                    }

                    // Обновляем текст, если он изменился
                    if (textBox.Text != formatted)
                    {
                        textBox.Text = formatted;
                        textBox.CaretIndex = textBox.Text.Length;
                    }
                }
            }
        }

        private void Expiry_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void Expiry_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                // Убираем слеш
                string text = textBox.Text.Replace("/", "");

                if (text.Length > 4)
                {
                    text = text.Substring(0, 4);
                }

                if (text.Length >= 2)
                {
                    string month = text.Substring(0, 2);
                    int monthNum;
                    if (int.TryParse(month, out monthNum))
                    {
                        if (monthNum > 12)
                        {
                            month = "12";
                            text = month + text.Substring(2);
                        }
                        else if (monthNum < 1 && text.Length == 2)
                        {
                            month = "01";
                            text = month + text.Substring(2);
                        }
                    }

                    string formatted = month;
                    if (text.Length > 2)
                    {
                        formatted += "/" + text.Substring(2);
                    }

                    if (textBox.Text != formatted)
                    {
                        textBox.Text = formatted;
                        textBox.CaretIndex = textBox.Text.Length;
                    }
                }
            }
        }

        private bool ValidateCardData()
        {
            if (rbCard.IsChecked != true)
                return true;

            // Проверка номера карты
            if (txtCardNumber == null || string.IsNullOrWhiteSpace(txtCardNumber.Text))
            {
                ShowError("Введите номер карты");
                txtCardNumber.Focus();
                return false;
            }

            string cardNumber = txtCardNumber.Text.Replace(" ", "");
            if (cardNumber.Length != 16)
            {
                ShowError("Номер карты должен содержать 16 цифр");
                txtCardNumber.Focus();
                return false;
            }

            // Проверка имени держателя
            if (txtCardHolder == null || string.IsNullOrWhiteSpace(txtCardHolder.Text))
            {
                ShowError("Введите имя держателя карты (как на карте)");
                txtCardHolder.Focus();
                return false;
            }

            // Проверка срока действия
            if (txtExpiry == null || string.IsNullOrWhiteSpace(txtExpiry.Text))
            {
                ShowError("Введите срок действия карты в формате ММ/ГГ");
                txtExpiry.Focus();
                return false;
            }

            string expiry = txtExpiry.Text;
            if (!Regex.IsMatch(expiry, @"^\d{2}/\d{2}$"))
            {
                ShowError("Введите срок действия карты в формате ММ/ГГ (например, 12/25)");
                txtExpiry.Focus();
                return false;
            }

            // Проверка, что срок не истек
            string[] parts = expiry.Split('/');
            if (parts.Length == 2)
            {
                int month = int.Parse(parts[0]);
                int year = int.Parse(parts[1]) + 2000;
                DateTime cardDate = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
                if (cardDate < DateTime.Now)
                {
                    ShowError("Срок действия карты истек");
                    txtExpiry.Focus();
                    return false;
                }
            }

            // Проверка CVV
            if (txtCVV == null || string.IsNullOrWhiteSpace(txtCVV.Password))
            {
                ShowError("Введите CVV/CVC код (3 цифры на обороте карты)");
                txtCVV.Focus();
                return false;
            }

            string cvv = txtCVV.Password;
            if (cvv.Length != 3 || !Regex.IsMatch(cvv, @"^\d{3}$"))
            {
                ShowError("CVV/CVC должен содержать 3 цифры");
                txtCVV.Focus();
                return false;
            }

            return true;
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

        private void Pay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateCardData())
                    return;

                if (dpPickupDate == null || dpPickupDate.SelectedDate == null)
                {
                    ShowError("Выберите дату получения заказа");
                    return;
                }

                DateTime pickupDateTime = dpPickupDate.SelectedDate.Value.Date;
                if (cbPickupTime != null && cbPickupTime.SelectedItem != null)
                {
                    string timeStr = (cbPickupTime.SelectedItem as ComboBoxItem)?.Content.ToString();
                    if (!string.IsNullOrEmpty(timeStr))
                    {
                        var timeParts = timeStr.Split(':');
                        pickupDateTime = pickupDateTime.AddHours(int.Parse(timeParts[0]));
                        if (timeParts.Length > 1)
                            pickupDateTime = pickupDateTime.AddMinutes(int.Parse(timeParts[1]));
                    }
                }

                // Получаем адрес из ComboBox
                string pickupAddress = "г. Москва, ул. Здоровья, д. 15";
                if (cmbPickupAddress != null && cmbPickupAddress.SelectedItem != null)
                {
                    pickupAddress = (cmbPickupAddress.SelectedItem as ComboBoxItem)?.Content.ToString() ?? pickupAddress;
                }
                else if (cmbPickupAddress != null && !string.IsNullOrEmpty(cmbPickupAddress.Text))
                {
                    pickupAddress = cmbPickupAddress.Text;
                }

                decimal totalAmount = cart.Sum(c => c.TotalPrice);
                string paymentMethod = (rbCard.IsChecked == true) ? "Card" : "Cash";

                var order = new Orders
                {
                    UserID = currentUser.UserID,
                    OrderDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    PaymentMethod = paymentMethod,
                    Status = "Completed",
                    PickupAddress = pickupAddress,
                    PickupTime = pickupDateTime
                };

                Connection.entities.Orders.Add(order);
                Connection.entities.SaveChanges();

                foreach (var cartItem in cart)
                {
                    var orderDetail = new OrderDetails
                    {
                        OrderID = order.OrderID,
                        ProductID = cartItem.ProductID,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Price
                    };

                    Connection.entities.OrderDetails.Add(orderDetail);

                    var product = Connection.entities.Products.Find(cartItem.ProductID);
                    if (product != null)
                    {
                        product.StockQuantity -= cartItem.Quantity;
                    }
                }

                Connection.entities.SaveChanges();

                MessageBox.Show(
                    $"✅ ЗАКАЗ УСПЕШНО ОФОРМЛЕН!\n\n" +
                    $"📋 Номер заказа: {order.OrderID}\n" +
                    $"💰 Сумма: {totalAmount:C}\n" +
                    $"💳 Способ оплаты: {(paymentMethod == "Card" ? "Банковская карта" : "Наличные")}\n\n" +
                    $"📍 САМОВЫВОЗ:\n{pickupAddress}\n\n" +
                    $"⏰ ВРЕМЯ ПОЛУЧЕНИЯ:\n{pickupDateTime:dd.MM.yyyy HH:mm}\n\n" +
                    $"🙏 Спасибо за покупку!\nЖдем вас снова!",
                    "Оплата успешна",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при оформлении заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}