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

        private DE3Entities _context = new DE3Entities();
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
            foreach (var product in productList)
            {
                allProductPrice += product.Product.Price * product.QuantityProducts;
                if (product.Product.Discount != null)
                {
                    allProductPrice += Convert.ToDecimal(product.Product.Price - ((product.Product.Price / 100) * product.Product.Discount));
                    allProductAmount += Convert.ToDecimal(product.Product.Discount);
                }
            }
            ProductAmountTB.Text = $"{allProductAmount.ToString()}%";
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
                CreateTalon(newOrder, productsInOrder);

            }
            MessageBox.Show("Заказ оформлен");
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
        private void CreateTalon(Order order, List<ProductsInOrder> productsInOrders)
        {
            // Создание документа PDF
            iTextSharp.text.Document doc = new iTextSharp.text.Document();
            string filePath = "C:/Users/Rocket/Downloads/OrderReceipt.pdf"; // Путь для сохранения файла
            iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));

            doc.Open();

            // Добавление заголовка
            iTextSharp.text.Paragraph title = new iTextSharp.text.Paragraph("Талон заказа");
            title.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
            doc.Add(title);

            // Создание таблицы для отображения информации о заказе
            PdfPTable table = new PdfPTable(2); // 2 столбца

            // Добавление информации о заказе в таблицу
            table.AddCell("Номер заказа");
            table.AddCell(order.ID.ToString());

            table.AddCell("Список продуктов");
            foreach (var item in productsInOrders)
            {
                table.AddCell(item.Product.Name);
            }

            table.AddCell("Цена");
            table.AddCell(allProductPrice.ToString());

            table.AddCell("Скидка");
            table.AddCell(allProductAmount.ToString());

            table.AddCell("Пункт выдачи");
            table.AddCell(DE3Entities.GetContext().PickUpPoint.Where(x => x.ID == order.PickUpPointID).FirstOrDefault().Name);

            table.AddCell("Код получения");
            table.AddCell(order.PickUpCode.ToString());

            // Добавление таблицы в документ
            doc.Add(table);

            doc.Close();

            MessageBox.Show("Талон заказа сохранен в файл " + filePath + ".");
        }
    }
}
