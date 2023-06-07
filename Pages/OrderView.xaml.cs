using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace OOO_Knizhnii_Klub.Pages
{
    /// <summary>
    /// Логика взаимодействия для OrderView.xaml
    /// </summary>
    public partial class OrderView : Page
    {

        Order order;
        List<ProductsInOrder> productsInOrder;
        
        decimal allProductPrice;
        decimal allProductAmount;
        public OrderView(List<Product> products)
        {
            InitializeComponent();

            order = new Order();
            order.ID = DE3Entities.GetContext().Order.Max(o => o.ID) + 1;
            productsInOrder = new List<ProductsInOrder>();
            PickUpPointCB.ItemsSource = DE3Entities.GetContext().PickUpPoint.ToList();

            foreach (var product in products)
            {
                var existingProductInOrder = productsInOrder.FirstOrDefault(p => p.ProductID == product.ID);
                if (existingProductInOrder != null)
                {
                    // Увеличиваем количество товара, если он уже есть в корзине
                    existingProductInOrder.QuantityProducts++;
                }
                else
                {
                    var productInOrder = new ProductsInOrder()
                    {
                        OrderID = order.ID,
                        Product = product,
                        ProductID = product.ID,
                        QuantityProducts = 1
                    };
                    productsInOrder.Add(productInOrder);
                }
            }

            OrderList.ItemsSource = productsInOrder;
            CalculShowPriceAmount(productsInOrder);
        }

        private void CalculShowPriceAmount(List<ProductsInOrder> productList)
        {
            allProductPrice = 0; // Сбрасываем значение перед расчетом новой суммы
            allProductAmount = 0; // Сбрасываем значение перед расчетом новой суммы

            foreach (var product in productList)
            {
                decimal productTotalPrice = product.Product.Price * product.QuantityProducts;
                allProductPrice += productTotalPrice;

                if (product.Product.Discount != null)
                {
                    decimal discountAmount = (decimal)(productTotalPrice - ((productTotalPrice / 100) * product.Product.Discount));
                    allProductAmount += discountAmount;
                }
            }

            ProductAmountTB.Text = $"{allProductAmount.ToString()}";
            ProductPriceTB.Text = $"{allProductPrice.ToString()}";
        }

        private void plus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext is ProductsInOrder orderProduct)
            {
                var productInOrder = productsInOrder.Find(x => x.ProductID == orderProduct.ProductID);
                if (productInOrder != null)
                {
                    productInOrder.QuantityProducts++;
                }
            }
            CalculShowPriceAmount(productsInOrder);
            OrderList.ItemsSource = productsInOrder;
            OrderList.Items.Refresh();
        }

        private void minus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext is ProductsInOrder orderProduct)
            {
                productsInOrder.Where(x => x.ProductID == orderProduct.ProductID).FirstOrDefault().QuantityProducts--;
                if (productsInOrder.Where(x => x.ProductID == orderProduct.ProductID).FirstOrDefault().QuantityProducts == 0)
                {
                    productsInOrder.Remove(productsInOrder.Where(x => x.ProductID == orderProduct.ProductID).FirstOrDefault());
                }
            }
            CalculShowPriceAmount(productsInOrder);
            OrderList.ItemsSource = productsInOrder;
            OrderList.Items.Refresh();
        }
        private void FormOrder_Click(object sender, RoutedEventArgs e)
        {
            if (PickUpPointCB.SelectedItem != null)
            {
                var maxOrderCode = DE3Entities.GetContext().Order.Max(o => o.PickUpCode);
                var newOrder = new Order()
                {
                    DeliveryDate = DeliveryDate(productsInOrder),
                    PickUpPointID = ((PickUpPoint)PickUpPointCB.SelectedItem).ID,
                    OrderStatusID = 0,
                    PickUpCode = maxOrderCode++,
                };
                DE3Entities.GetContext().Order.Add(newOrder);
                foreach (var item in productsInOrder)
                {
                    var newOrderProduct = new ProductsInOrder()
                    {
                        OrderID = newOrder.ID,
                        ProductID = item.ProductID,
                        QuantityProducts = item.QuantityProducts
                    };
                    DE3Entities.GetContext().ProductsInOrder.Add(newOrderProduct);
                }
                DE3Entities.GetContext().SaveChanges();

                // Формирование сообщения с информацией о заказе и списком товаров
                StringBuilder messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Заказ оформлен");
                messageBuilder.AppendLine($"Номер заказа: {newOrder.ID}");
                messageBuilder.AppendLine($"Дата доставки: {newOrder.DeliveryDate}");
                messageBuilder.AppendLine($"Пункт выдачи: {((PickUpPoint)PickUpPointCB.SelectedItem).Name}");
                messageBuilder.AppendLine($"Код получения: {newOrder.PickUpCode}");

                messageBuilder.AppendLine("\nСписок товаров в заказе:");
                foreach (var item in productsInOrder)
                {
                    var product = DE3Entities.GetContext().Product.FirstOrDefault(p => p.ID == item.ProductID);
                    messageBuilder.AppendLine($"{product.Name} - {item.QuantityProducts} шт.");
                }

                string message = messageBuilder.ToString();

                MessageBox.Show(message, "Информация о заказе", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Выберите пункт выдачи.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private DateTime DeliveryDate(List<ProductsInOrder> oProductList)
        {
            if (oProductList.Count >= 6)
            {
                return DateTime.Now.AddDays(6);
            }
            else
            {
                foreach (var item in oProductList)
                {
                    if (item.Product.ProductQuantity < item.QuantityProducts)
                    {
                        return DateTime.Now.AddDays(6);
                    }
                }
            }
            return DateTime.Now.AddDays(3);

        }
       
    }
}
