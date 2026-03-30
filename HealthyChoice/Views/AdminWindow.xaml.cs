using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HealthyChoice.Connect;

namespace HealthyChoice.Views
{
    public partial class AdminWindow : Window
    {
        private Users currentUser;
        private string productSearchText = "";

        public AdminWindow()
        {
            InitializeComponent();
            currentUser = (Users)App.Current.Properties["CurrentUser"];
            txtAdminInfo.Text = $"Администратор: {currentUser.FullName}";
            LoadStatistics();
            LoadUsers();
            LoadProducts();
            LoadOrders();

            // Инициализация поиска товаров
            if (txtSearchProduct != null)
            {
                txtSearchProduct.Text = "Поиск товаров...";
                txtSearchProduct.Foreground = System.Windows.Media.Brushes.Gray;
                txtSearchProduct.GotFocus += TxtSearchProduct_GotFocus;
                txtSearchProduct.LostFocus += TxtSearchProduct_LostFocus;
            }
        }

        #region Поиск товаров
        private void TxtSearchProduct_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearchProduct.Text == "Поиск товаров...")
            {
                txtSearchProduct.Text = "";
                txtSearchProduct.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void TxtSearchProduct_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearchProduct.Text))
            {
                txtSearchProduct.Text = "Поиск товаров...";
                txtSearchProduct.Foreground = System.Windows.Media.Brushes.Gray;
                productSearchText = "";
                LoadProducts();
            }
        }

        private void SearchProduct_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSearchProduct.Text != "Поиск товаров...")
            {
                productSearchText = txtSearchProduct.Text;
                LoadProducts();
            }
        }

        private void SearchProduct_Click(object sender, RoutedEventArgs e)
        {
            if (txtSearchProduct.Text != "Поиск товаров...")
            {
                productSearchText = txtSearchProduct.Text;
                LoadProducts();
            }
        }
        #endregion

        #region Загрузка данных
        private void LoadStatistics()
        {
            try
            {
                var completedOrders = Connection.entities.Orders.Where(o => o.Status == "Completed");

                // Общая выручка
                decimal totalRevenue = 0;
                if (completedOrders.Any())
                {
                    totalRevenue = completedOrders.Sum(o => o.TotalAmount);
                }
                txtTotalRevenue.Text = totalRevenue.ToString("C");

                // Всего заказов
                int totalOrders = Connection.entities.Orders.Count();
                txtTotalOrders.Text = totalOrders.ToString();

                // Всего клиентов
                int totalUsers = Connection.entities.Users.Count(u => u.Role == "User");
                txtTotalUsers.Text = totalUsers.ToString();

                // Всего товаров
                int totalProducts = Connection.entities.Products.Count();
                txtTotalProducts.Text = totalProducts.ToString();

                // Средний чек
                decimal avgOrder = 0;
                if (completedOrders.Any())
                {
                    avgOrder = completedOrders.Average(o => o.TotalAmount);
                }
                txtAvgOrder.Text = avgOrder.ToString("C");

                // Топ-5 популярных товаров
                var topProducts = Connection.entities.OrderDetails
                    .GroupBy(od => od.ProductID)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        TotalSold = g.Sum(od => od.Quantity),
                        Revenue = g.Sum(od => od.Quantity * od.Price)
                    })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(5)
                    .ToList();

                var topProductsList = topProducts.Select(x => new
                {
                    Product = Connection.entities.Products.FirstOrDefault(p => p.ProductID == x.ProductId)?.ProductName ?? "Неизвестно",
                    x.TotalSold,
                    x.Revenue
                }).ToList();

                dgTopProducts.ItemsSource = topProductsList;

                // Динамика заказов за последние 7 дней
                var ordersByDay = Connection.entities.Orders
                    .Where(o => o.Status == "Completed")
                    .ToList()
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count(),
                        Total = g.Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(x => x.Date)
                    .Take(7)
                    .ToList();

                dgOrdersByDay.ItemsSource = ordersByDay;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUsers()
        {
            try
            {
                dgUsers.ItemsSource = Connection.entities.Users.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProducts()
        {
            try
            {
                var products = Connection.entities.Products.AsQueryable();

                if (!string.IsNullOrEmpty(productSearchText) && productSearchText != "Поиск товаров...")
                {
                    products = products.Where(p => p.ProductName.Contains(productSearchText) ||
                                                   p.Description.Contains(productSearchText));
                }

                dgProducts.ItemsSource = products.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadOrders()
        {
            try
            {
                var orders = Connection.entities.Orders.ToList();
                var orderList = orders.Select(o => new
                {
                    o.OrderID,
                    UserFullName = Connection.entities.Users.FirstOrDefault(u => u.UserID == o.UserID)?.FullName ?? "Неизвестно",
                    o.OrderDate,
                    o.TotalAmount,
                    o.PaymentMethod,
                    o.Status,
                    o.PickupAddress,
                    o.PickupTime
                }).OrderByDescending(o => o.OrderDate).ToList();

                dgOrders.ItemsSource = orderList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Обновление данных
        private void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadUsers();
                MessageBox.Show("✅ Список пользователей успешно обновлен!", "Обновление",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshProducts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadProducts();
                MessageBox.Show("✅ Список товаров успешно обновлен!", "Обновление",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Управление товарами
        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddProductDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    Connection.entities.Products.Add(dialog.NewProduct);
                    Connection.entities.SaveChanges();

                    LoadProducts();
                    LoadStatistics();

                    MessageBox.Show($"✅ Товар '{dialog.NewProduct.ProductName}' успешно добавлен!\n\n" +
                                  $"💰 Цена: {dialog.NewProduct.Price:C}\n" +
                                  $"📦 Количество: {dialog.NewProduct.StockQuantity} шт.\n" +
                                  $"📁 Категория: {dialog.NewProduct.Category}",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Ошибка при добавлении товара: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem == null)
            {
                MessageBox.Show("⚠️ Выберите товар для редактирования",
                              "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var product = dgProducts.SelectedItem as Products;

            var dialog = new EditProductDialog(product);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    Connection.entities.SaveChanges();
                    LoadProducts();
                    LoadStatistics();
                    MessageBox.Show($"✅ Товар '{product.ProductName}' успешно обновлен!",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Ошибка при обновлении: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem == null)
            {
                MessageBox.Show("⚠️ Выберите товар для удаления",
                              "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var product = dgProducts.SelectedItem as Products;

            // Проверяем, есть ли заказы с этим товаром
            var hasOrders = Connection.entities.OrderDetails.Any(od => od.ProductID == product.ProductID);

            if (hasOrders)
            {
                MessageBox.Show($"❌ Невозможно удалить товар '{product.ProductName}', так как он присутствует в заказах!\n\n" +
                              $"📋 Сначала удалите или отмените заказы с этим товаром.",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"🗑️ Удалить товар '{product.ProductName}'?\n\n" +
                                        $"💰 Цена: {product.Price:C}\n" +
                                        $"📦 В наличии: {product.StockQuantity} шт.\n\n" +
                                        "⚠️ Это действие нельзя отменить!",
                                        "Подтверждение удаления",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Connection.entities.Products.Remove(product);
                    Connection.entities.SaveChanges();
                    LoadProducts();
                    LoadStatistics();
                    MessageBox.Show($"✅ Товар '{product.ProductName}' успешно удален!",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Ошибка при удалении: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Products_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить логику при выборе товара
        }
        #endregion

        #region Управление заказами
        private void Orders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить логику при выборе заказа
        }

        private void ViewOrderDetails_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrders.SelectedItem == null)
            {
                MessageBox.Show("⚠️ Выберите заказ для просмотра деталей",
                              "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                dynamic selectedOrder = dgOrders.SelectedItem;
                int orderId = selectedOrder.OrderID;

                var order = Connection.entities.Orders.FirstOrDefault(o => o.OrderID == orderId);
                var user = Connection.entities.Users.FirstOrDefault(u => u.UserID == order.UserID);
                var details = Connection.entities.OrderDetails.Where(od => od.OrderID == orderId).ToList();

                string detailsText = $"📋 ДЕТАЛИ ЗАКАЗА №{order.OrderID}\n\n";
                detailsText += $"👤 Клиент: {user?.FullName}\n";
                detailsText += $"📧 Email: {user?.Email}\n";
                detailsText += $"📞 Телефон: {user?.Phone}\n";
                detailsText += $"📅 Дата заказа: {order.OrderDate:dd.MM.yyyy HH:mm}\n";
                detailsText += $"💳 Оплата: {(order.PaymentMethod == "Card" ? "Банковская карта" : "Наличные")}\n";
                detailsText += $"📊 Статус: {(order.Status == "Completed" ? "✅ Выполнен" : "⏳ В обработке")}\n";
                detailsText += $"💰 Общая сумма: {order.TotalAmount:C}\n\n";
                detailsText += "🛍️ ТОВАРЫ В ЗАКАЗЕ:\n";
                detailsText += new string('═', 40) + "\n";

                foreach (var detail in details)
                {
                    var product = Connection.entities.Products.FirstOrDefault(p => p.ProductID == detail.ProductID);
                    detailsText += $"📦 {product?.ProductName}\n";
                    detailsText += $"   {detail.Quantity} шт. × {detail.Price:C} = {detail.Quantity * detail.Price:C}\n\n";
                }

                detailsText += new string('═', 40) + "\n";
                detailsText += $"📍 Адрес выдачи: {order.PickupAddress}\n";
                detailsText += $"⏰ Время получения: {order.PickupTime:dd.MM.yyyy HH:mm}";

                var scrollViewer = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = detailsText,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12,
                        Margin = new Thickness(10),
                        FontFamily = new System.Windows.Media.FontFamily("Consolas")
                    },
                    MaxHeight = 500,
                    Width = 500
                };

                var window = new Window
                {
                    Title = $"Детали заказа №{order.OrderID}",
                    Content = scrollViewer,
                    Width = 550,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка при просмотре деталей заказа: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CompleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrders.SelectedItem == null)
            {
                MessageBox.Show("⚠️ Выберите заказ", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                dynamic selectedOrder = dgOrders.SelectedItem;
                int orderId = selectedOrder.OrderID;
                var order = Connection.entities.Orders.FirstOrDefault(o => o.OrderID == orderId);

                if (order.Status == "Completed")
                {
                    MessageBox.Show("ℹ️ Заказ уже выполнен", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show($"✅ Отметить заказ №{order.OrderID} как выполненный?\n\n" +
                                            $"💰 Сумма: {order.TotalAmount:C}\n" +
                                            $"👤 Клиент: {selectedOrder.UserFullName}",
                                            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    order.Status = "Completed";
                    Connection.entities.SaveChanges();
                    LoadOrders();
                    LoadStatistics();
                    MessageBox.Show($"✅ Заказ №{order.OrderID} отмечен как выполненный!",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelOrder_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrders.SelectedItem == null)
            {
                MessageBox.Show("⚠️ Выберите заказ", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                dynamic selectedOrder = dgOrders.SelectedItem;
                int orderId = selectedOrder.OrderID;
                var order = Connection.entities.Orders.FirstOrDefault(o => o.OrderID == orderId);

                if (order.Status == "Cancelled")
                {
                    MessageBox.Show("ℹ️ Заказ уже отменен", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show($"❌ ОТМЕНИТЬ ЗАКАЗ №{order.OrderID}?\n\n" +
                                            $"💰 Сумма: {order.TotalAmount:C}\n" +
                                            $"👤 Клиент: {selectedOrder.UserFullName}\n\n" +
                                            "⚠️ Товары будут возвращены на склад!\n\n" +
                                            "Это действие нельзя отменить!",
                                            "Подтверждение отмены",
                                            MessageBoxButton.YesNo,
                                            MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var details = Connection.entities.OrderDetails.Where(od => od.OrderID == order.OrderID).ToList();

                    foreach (var detail in details)
                    {
                        var product = Connection.entities.Products.Find(detail.ProductID);
                        if (product != null)
                        {
                            product.StockQuantity += detail.Quantity;
                        }
                    }

                    order.Status = "Cancelled";
                    Connection.entities.SaveChanges();
                    LoadOrders();
                    LoadProducts();
                    LoadStatistics();
                    MessageBox.Show($"✅ Заказ №{order.OrderID} отменен!\n\n" +
                                  $"📦 Товары ({details.Count} позиций) возвращены на склад.",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка при отмене заказа: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Выход
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из панели администратора?", "Выход",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
        #endregion
    }
}