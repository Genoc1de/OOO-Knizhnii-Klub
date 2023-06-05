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
using Excel = Microsoft.Office.Interop.Excel;

namespace OOO_Knizhnii_Klub
{
    /// <summary>
    /// Логика взаимодействия для ProductPage.xaml
    /// </summary>
    public partial class ProductPage : Page
    {
        private DE3Entities _context = new DE3Entities();
        private Order CurrentOrder { get; set; } = new();
        private List<ProductsInOrder> ProductsInOrder { get; set; } = new();



        public ProductPage()
        {
            InitializeComponent();
            ListProducts.ItemsSource = DE3Entities.GetContext().Product.ToList();

            ListOrderProducts.ItemsSource = DE3Entities.GetContext().ProductsInOrder.ToList();

            ComboBoxOrderStatus.ItemsSource = _context.OrderStatus.ToList();
            ComboBoxOrderStatus.SelectedItem = ((ComboBoxOrderStatus.ItemsSource as List<OrderStatus>)!).FirstOrDefault();
            ComboBoxPickupPoint.ItemsSource = _context.PickUpPoint.ToList();


        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new EditPage(null));
        }

        private void BtnDel_Click(object sender, RoutedEventArgs e)
        {
            var productsForRemoving = ListProducts.SelectedItems.Cast<Product>().ToList();

            if (MessageBox.Show($"Вы точно хотите удалить следующие {productsForRemoving.Count()} элементов?", "Внимание",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    DE3Entities.GetContext().Product.RemoveRange(productsForRemoving);
                    DE3Entities.GetContext().SaveChanges();
                    MessageBox.Show("Данные удалены");

                    ListProducts.ItemsSource = DE3Entities.GetContext().Product.ToList();

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
            }

        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {

            Manager.MainFrame.Navigate(new EditPage((sender as Button).DataContext as Product));
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch ((((TabControl)sender).SelectedItem as TabItem)?.Name)
            {
                case "TabItemListProducts":
                    ButtonCreateOrder.Visibility = Visibility.Hidden;
                    break;
                case "TabItemOrder":
                    ButtonCreateOrder.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void DeleteOrderProduct_Click(object sender, RoutedEventArgs e)
        {
            ProductsInOrder.Remove((((Button)sender).DataContext as ProductsInOrder)!);

            ListOrderProducts.ItemsSource = new List<ProductsInOrder>();
            ListOrderProducts.ItemsSource = ProductsInOrder;
        }

        private void GridProducts_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                var result = MessageBox.Show("Add a product to an order?", "Adding a product to the cart", MessageBoxButton.YesNo);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        AddOrderProduct(sender);
                        if (TabItemOrder.IsEnabled == false)
                        {
                            TabItemOrder.IsEnabled = true;
                        }
                        break;
                    case MessageBoxResult.Cancel:
                        break;
                    case MessageBoxResult.None:
                    case MessageBoxResult.OK:
                    case MessageBoxResult.No:
                    default:
                        break;
                }

                ListOrderProducts.ItemsSource = new List<ProductsInOrder>();
                ListOrderProducts.ItemsSource = ProductsInOrder;
            }
        }

        private void AddOrderProduct(object sender)
        {
            foreach (var p1 in ProductsInOrder.Where(p1 => p1.Product.ID == (((Product)((Grid)sender).DataContext)!).ID))
            {
                p1.QuantityProducts++;
                return;
            }

            ProductsInOrder.Add(new ProductsInOrder()
            {
                Order = CurrentOrder,
                QuantityProducts = 1,
                Product = (Product)((Grid)sender).DataContext ?? new Product(),
                OrderID = CurrentOrder.ID,
                ProductID = (((Product)((Grid)sender).DataContext)!).ID
            });
        }

        private void ButtonCreateOrder_Click(object sender, RoutedEventArgs e)
        {
            switch (TabItemMakingOrder.IsEnabled)
            {
                case true:
                    TabItemMakingOrder.IsEnabled = false;
                    ButtonCreateOrder.Content = "CREATE ORDER";
                    break;
                case false:
                    TabItemMakingOrder.IsEnabled = true;
                    ButtonCreateOrder.Content = "ORDER CANCELLATION";
                    break;
            }
        }


        private async void ButtonSendOrder_Click(object sender, RoutedEventArgs e)
        {
            var maxGetCode = _context.Order.Max(o => o.PickUpCode);

            try
            {
                await AddOrder(maxGetCode);
                MessageBox.Show("Success");
                await PrintOrderCard();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }

            RefreshWindow();
        }

        private async Task AddOrder(int maxGetCode)
        {
            using (var context = new DE3Entities())
            {
                this.CurrentOrder = new Order()
                {
                    PickUpPointID = ((PickUpPoint)ComboBoxPickupPoint.SelectedItem).ID,
                    OrderStatusID = ((OrderStatus)ComboBoxOrderStatus.SelectedItem).ID,
                    DeliveryDate = DateTime.Now,
                    PickUpCode = maxGetCode++
                };

                DE3Entities.GetContext().Order.Add(CurrentOrder);
                await context.SaveChangesAsync();

                this.CurrentOrder.ProductsInOrder = this.ProductsInOrder;

            }
        }
        private Task PrintOrderCard()
        {
            var orders = CurrentOrder;
            var app = new Excel.Application
            {
                SheetsInNewWorkbook = 1
            };

            var workbook = app.Workbooks.Add(Type.Missing);

            Excel.Worksheet worksheet = app.Worksheets.Item[1];
            worksheet.Name = "Card";

            worksheet.Cells[1][1] = "Order number";
            worksheet.Cells[1][2] = "Product list";
            worksheet.Cells[1][3] = "Total cost";

            worksheet.Cells[2][1] = orders.ID;

            var fullProductList = string.Empty;
            fullProductList = orders.ProductsInOrder.Aggregate(fullProductList,
                (current, product) => current + $"{product.Product.Name}\n");
            worksheet.Cells[2][2] = fullProductList;
            worksheet.Cells[2][3] = orders.ProductsInOrder.Sum(p => p.Product.Price);

            worksheet.Columns.AutoFit();

            app.Visible = true;

            app.Application.ActiveWorkbook.SaveAs(@"C:\Users\Rocket\source\repos\OOO Knizhnii Klub\test.xlsx"); 

            var excelDocument = app.Workbooks.Open(@"C:\Users\Rocket\source\repos\OOO Knizhnii Klub\test.xlsx");

            excelDocument.ExportAsFixedFormat(Excel.XlFixedFormatType.xlTypePDF, @"C:\Users\Rocket\source\repos\OOO Knizhnii Klub\test.pdf");
            excelDocument.Close(false, "", false);
            app.Quit();
            return Task.CompletedTask;
        }

        private void RefreshWindow()
        {
            this.CurrentOrder = new Order();
            this.ProductsInOrder = new List<ProductsInOrder>();

            ListOrderProducts.ItemsSource = new List<ProductsInOrder>();
            ListOrderProducts.ItemsSource = ProductsInOrder;

            MainTabControl.SelectedIndex = 0;
            TabItemOrder.IsSelected = false;
            TabItemMakingOrder.IsEnabled = false;
            ButtonCreateOrder.Content = "CREATE ORDER";
            ButtonCreateOrder.Visibility = Visibility.Hidden;
        }

    }
    }


