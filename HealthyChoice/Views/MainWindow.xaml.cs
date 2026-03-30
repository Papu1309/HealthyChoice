using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HealthyChoice.Connect;

namespace HealthyChoice.Views
{
    public partial class MainWindow : Window
    {
        private Users currentUser;
        private List<CartItem> cart = new List<CartItem>();

        public MainWindow()
        {
            InitializeComponent();
            currentUser = (Users)App.Current.Properties["CurrentUser"];
            txtUserInfo.Text = $"Добро пожаловать, {currentUser.FullName}";
            LoadCategories();
            LoadProducts();
            sliderPrice.ValueChanged += SliderPrice_ValueChanged;
            txtSearch.Text = "Поиск товаров...";
            txtSearch.Foreground = System.Windows.Media.Brushes.Gray;
            txtSearch.GotFocus += TxtSearch_GotFocus;
            txtSearch.LostFocus += TxtSearch_LostFocus;
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Поиск товаров...")
            {
                txtSearch.Text = "";
                txtSearch.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Поиск товаров...";
                txtSearch.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void LoadCategories()
        {
            var categories = Connection.entities.Products
                .Select(p => p.Category)
                .Distinct()
                .Where(c => c != null)
                .ToList();
            categories.Insert(0, "Все");
            lstCategories.ItemsSource = categories;
            lstCategories.SelectedIndex = 0;
        }

        private void LoadProducts(string category = null, string searchText = null, decimal maxPrice = 5000)
        {
            var products = Connection.entities.Products.AsQueryable();

            if (!string.IsNullOrEmpty(category) && category != "Все")
            {
                products = products.Where(p => p.Category == category);
            }

            products = products.Where(p => p.Price <= maxPrice);

            if (!string.IsNullOrEmpty(searchText) && searchText != "Поиск товаров...")
            {
                products = products.Where(p => p.ProductName.Contains(searchText) ||
                                                p.Description.Contains(searchText));
            }

            var productList = products.ToList();
            itemsProducts.ItemsSource = productList;
            txtProductCount.Text = $"Найдено товаров: {productList.Count}";
        }

        private void SliderPrice_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtMaxPrice != null)
            {
                txtMaxPrice.Text = $"Макс. цена: {sliderPrice.Value:F0} ₽";
                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            string category = lstCategories.SelectedItem?.ToString();
            string searchText = txtSearch.Text;
            decimal maxPrice = (decimal)sliderPrice.Value;

            LoadProducts(category, searchText, maxPrice);
        }

        private void Category_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstCategories.SelectedItem != null)
            {
                string category = lstCategories.SelectedItem.ToString();
                txtCategoryTitle.Text = category;
                ApplyFilters();
            }
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSearch.Text != "Поиск товаров...")
            {
                ApplyFilters();
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void SearchByPrice_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PriceRangeDialog();
            if (dialog.ShowDialog() == true)
            {
                SearchByPriceRange(dialog.MinPrice, dialog.MaxPrice);
            }
        }

        private void Product_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var product = border?.DataContext as Products;

            if (product != null)
            {
                OpenAddToCartDialog(product);
            }
        }

        private void AddToCartButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var border = button?.Parent as StackPanel;
            var parentBorder = border?.Parent as Border;
            var product = parentBorder?.DataContext as Products;

            if (product != null)
            {
                OpenAddToCartDialog(product);
            }
        }

        private void OpenAddToCartDialog(Products product)
        {
            if (product.StockQuantity == 0)
            {
                MessageBox.Show("Товар временно отсутствует на складе", "Информация",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new AddToCartDialog(product);
            if (dialog.ShowDialog() == true)
            {
                int quantity = dialog.Quantity;
                decimal totalPrice = dialog.TotalPrice;

                var existingItem = cart.FirstOrDefault(c => c.ProductID == product.ProductID);
                if (existingItem != null)
                {
                    if (existingItem.Quantity + quantity <= product.StockQuantity)
                    {
                        existingItem.Quantity += quantity;
                        existingItem.TotalPrice += totalPrice;
                        existingItem.GiftWrap = existingItem.GiftWrap || dialog.GiftWrap;
                        existingItem.ExpressDelivery = existingItem.ExpressDelivery || dialog.ExpressDelivery;
                        existingItem.Insurance = existingItem.Insurance || dialog.Insurance;

                        string message = $"✅ Товар добавлен в корзину!\n\n";
                        message += $"Товар: {product.ProductName}\n";
                        message += $"Количество: {quantity} шт.\n";
                        message += $"Сумма: {totalPrice:C}\n";
                        if (dialog.GiftWrap) message += $"🎁 Подарочная упаковка\n";
                        if (dialog.ExpressDelivery) message += $"⚡ Экспресс-доставка\n";
                        if (dialog.Insurance) message += $"🛡️ Страхование\n";
                        message += $"\nВсего в корзине: {existingItem.Quantity} шт.";

                        MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"⚠️ Недостаточно товара на складе.\nДоступно: {product.StockQuantity} шт.\nВ корзине: {existingItem.Quantity} шт.",
                                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        ProductID = product.ProductID,
                        ProductName = product.ProductName,
                        Price = product.Price,
                        Quantity = quantity,
                        StockQuantity = product.StockQuantity,
                        TotalPrice = totalPrice,
                        GiftWrap = dialog.GiftWrap,
                        ExpressDelivery = dialog.ExpressDelivery,
                        Insurance = dialog.Insurance
                    });

                    string message = $"✅ {product.ProductName} добавлен в корзину!\n\n";
                    message += $"Количество: {quantity} шт.\n";
                    message += $"Сумма: {totalPrice:C}\n";
                    if (dialog.GiftWrap) message += $"🎁 Подарочная упаковка\n";
                    if (dialog.ExpressDelivery) message += $"⚡ Экспресс-доставка\n";
                    if (dialog.Insurance) message += $"🛡️ Страхование\n";

                    MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void Cart_Click(object sender, RoutedEventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("🛒 Корзина пуста\n\nДобавьте товары из каталога",
                              "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            decimal total = cart.Sum(c => c.TotalPrice);
            int totalItems = cart.Sum(c => c.Quantity);

            string cartContent = $"🛒 ВАША КОРЗИНА\n\n";
            cartContent += $"Всего позиций: {cart.Count}\n";
            cartContent += $"Общее количество: {totalItems} шт.\n\n";
            cartContent += "Содержимое:\n";
            cartContent += new string('─', 40) + "\n";

            foreach (var item in cart)
            {
                cartContent += $"📦 {item.ProductName}\n";
                cartContent += $"   {item.Quantity} шт. × {item.Price:C} = {item.Price * item.Quantity:C}\n";
                if (item.GiftWrap) cartContent += $"   🎁 Подарочная упаковка\n";
                if (item.ExpressDelivery) cartContent += $"   ⚡ Экспресс-доставка\n";
                if (item.Insurance) cartContent += $"   🛡️ Страхование\n";
                if (item.TotalPrice != item.Price * item.Quantity)
                {
                    cartContent += $"   💰 С доп. услугами: {item.TotalPrice:C}\n";
                }
                cartContent += "\n";
            }

            cartContent += new string('─', 40) + "\n";
            cartContent += $"💰 ИТОГО: {total:C}\n\n";
            cartContent += "Перейти к оформлению заказа?";

            var result = MessageBox.Show(cartContent, "Корзина", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                PaymentWindow paymentWindow = new PaymentWindow(cart, currentUser);
                paymentWindow.ShowDialog();

                if (paymentWindow.DialogResult == true)
                {
                    cart.Clear();
                    LoadProducts();
                    MessageBox.Show("✅ Заказ успешно оформлен!\n\nИнформация о получении отправлена на экран.",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void Orders_Click(object sender, RoutedEventArgs e)
        {
            var orders = Connection.entities.Orders
                .Where(o => o.UserID == currentUser.UserID)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            if (orders.Count == 0)
            {
                MessageBox.Show("📋 У вас пока нет заказов\n\nПерейдите в каталог и сделайте первый заказ!",
                              "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string ordersInfo = "📦 ИСТОРИЯ ЗАКАЗОВ\n\n";
            foreach (var order in orders)
            {
                ordersInfo += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
                ordersInfo += $"📋 Заказ №{order.OrderID}\n";
                ordersInfo += $"📅 Дата: {order.OrderDate:dd.MM.yyyy HH:mm}\n";
                ordersInfo += $"💰 Сумма: {order.TotalAmount:C}\n";
                ordersInfo += $"📊 Статус: {(order.Status == "Completed" ? "✅ Выполнен" : "⏳ В обработке")}\n";
                ordersInfo += $"💳 Оплата: {(order.PaymentMethod == "Card" ? "Банковская карта" : "Наличные")}\n";
                ordersInfo += $"📍 Адрес выдачи: {order.PickupAddress}\n";
                ordersInfo += $"⏰ Время получения: {order.PickupTime:dd.MM.yyyy HH:mm}\n";

                var details = Connection.entities.OrderDetails
                    .Where(od => od.OrderID == order.OrderID)
                    .ToList();

                if (details.Any())
                {
                    ordersInfo += "\n🛍️ Товары в заказе:\n";
                    foreach (var detail in details)
                    {
                        var product = Connection.entities.Products.FirstOrDefault(p => p.ProductID == detail.ProductID);
                        ordersInfo += $"  • {product?.ProductName} x {detail.Quantity} = {detail.Quantity * detail.Price:C}\n";
                    }
                }

                ordersInfo += "\n";
            }

            var scrollViewer = new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = ordersInfo,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12,
                    Margin = new Thickness(10),
                    FontFamily = new System.Windows.Media.FontFamily("Consolas")
                },
                MaxHeight = 400,
                Width = 500
            };

            var window = new Window
            {
                Title = "Мои заказы",
                Content = scrollViewer,
                Width = 550,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            window.ShowDialog();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?", "Выход",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        // Новые функции

        private void ShowRecommendations_Click(object sender, RoutedEventArgs e)
        {
            ShowRecommendations();
        }

        private void ProductInfo_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem.Parent as ContextMenu;
            var border = contextMenu.PlacementTarget as Border;
            var product = border?.DataContext as Products;

            if (product != null)
            {
                ShowProductInfo(product);
            }
        }

        private void QuickAdd_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem.Parent as ContextMenu;
            var border = contextMenu.PlacementTarget as Border;
            var product = border?.DataContext as Products;

            if (product != null)
            {
                OpenAddToCartDialog(product);
            }
        }

        private void SimilarProducts_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem.Parent as ContextMenu;
            var border = contextMenu.PlacementTarget as Border;
            var product = border?.DataContext as Products;

            if (product != null)
            {
                ShowSimilarProducts(product);
            }
        }

        private void ShowProductInfo(Products product)
        {
            string info = $"📦 ИНФОРМАЦИЯ О ТОВАРЕ\n\n";
            info += $"Название: {product.ProductName}\n";
            info += $"Категория: {product.Category}\n";
            info += $"Цена: {product.Price:C}\n";
            info += $"В наличии: {product.StockQuantity} шт.\n";
            info += $"Описание: {product.Description}\n";

            MessageBox.Show(info, "Информация о товаре", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SearchByPriceRange(decimal minPrice, decimal maxPrice)
        {
            var products = Connection.entities.Products
                .Where(p => p.Price >= minPrice && p.Price <= maxPrice)
                .ToList();

            itemsProducts.ItemsSource = products;
            txtProductCount.Text = $"Найдено товаров: {products.Count}";
            txtCategoryTitle.Text = $"💰 Товары от {minPrice:C} до {maxPrice:C}";

            MessageBox.Show($"🔍 Найдено {products.Count} товаров в ценовом диапазоне от {minPrice:C} до {maxPrice:C}",
                          "Результаты поиска", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowRecommendations()
        {
            var popularProducts = Connection.entities.OrderDetails
                .GroupBy(od => od.ProductID)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(od => od.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToList();

            string recommendations = "🔥 ПОПУЛЯРНЫЕ ТОВАРЫ 🔥\n\n";
            int rank = 1;
            foreach (var item in popularProducts)
            {
                var product = Connection.entities.Products.FirstOrDefault(p => p.ProductID == item.ProductId);
                if (product != null)
                {
                    recommendations += $"{rank}. {product.ProductName}\n";
                    recommendations += $"   Цена: {product.Price:C}\n";
                    recommendations += $"   Продано: {item.TotalSold} шт.\n";
                    recommendations += $"   ⭐ Рейтинг: {Math.Min(5, item.TotalSold / 10 + 1)}/5\n\n";
                    rank++;
                }
            }

            if (popularProducts.Count == 0)
            {
                recommendations += "Пока нет данных для рекомендаций.\nСделайте первый заказ!";
            }

            MessageBox.Show(recommendations, "Рекомендации", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowSimilarProducts(Products product)
        {
            var similarProducts = Connection.entities.Products
                .Where(p => p.Category == product.Category && p.ProductID != product.ProductID)
                .Take(5)
                .ToList();

            if (similarProducts.Any())
            {
                string similar = $"🔍 ПОХОЖИЕ ТОВАРЫ\nКатегория: {product.Category}\n\n";
                foreach (var p in similarProducts)
                {
                    similar += $"📦 {p.ProductName}\n";
                    similar += $"   Цена: {p.Price:C}\n";
                    similar += $"   В наличии: {p.StockQuantity} шт.\n\n";
                }
                MessageBox.Show(similar, "Похожие товары", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Нет похожих товаров в этой категории", "Информация",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    public class CartItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int StockQuantity { get; set; }
        public decimal TotalPrice { get; set; }
        public bool GiftWrap { get; set; }
        public bool ExpressDelivery { get; set; }
        public bool Insurance { get; set; }
    }

    public class PriceRangeDialog : Window
    {
        public decimal MinPrice { get; private set; }
        public decimal MaxPrice { get; private set; }

        public PriceRangeDialog()
        {
            this.Title = "Поиск по цене";
            this.Width = 350;
            this.Height = 400;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.ResizeMode = ResizeMode.NoResize;
            this.Background = System.Windows.Media.Brushes.White;

            var mainPanel = new StackPanel { Margin = new Thickness(20) };

            mainPanel.Children.Add(new TextBlock { Text = "Минимальная цена:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            var txtMinPrice = new TextBox { Height = 35, Margin = new Thickness(0, 0, 0, 15), FontSize = 12 };
            mainPanel.Children.Add(txtMinPrice);

            mainPanel.Children.Add(new TextBlock { Text = "Максимальная цена:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            var txtMaxPrice = new TextBox { Height = 35, Margin = new Thickness(0, 0, 0, 15), FontSize = 12 };
            mainPanel.Children.Add(txtMaxPrice);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var btnSearch = new Button { Content = "🔍 Найти", Width = 100, Height = 35, Margin = new Thickness(5), FontSize = 12, FontWeight = FontWeights.Bold };
            var btnCancel = new Button { Content = "❌ Отмена", Width = 100, Height = 35, Margin = new Thickness(5), FontSize = 12 };

            btnSearch.Background = System.Windows.Media.Brushes.LightGreen;
            btnSearch.Click += (s, e) => {
                if (decimal.TryParse(txtMinPrice.Text, out decimal min) && decimal.TryParse(txtMaxPrice.Text, out decimal max))
                {
                    if (min >= 0 && max >= min)
                    {
                        MinPrice = min;
                        MaxPrice = max;
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Цена должна быть положительной и максимальная цена должна быть больше минимальной",
                                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Введите корректные цены", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            btnCancel.Click += (s, e) => {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(btnSearch);
            buttonPanel.Children.Add(btnCancel);
            mainPanel.Children.Add(buttonPanel);

            this.Content = mainPanel;
        }
    }
}