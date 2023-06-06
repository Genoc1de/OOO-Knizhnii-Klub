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

namespace OOO_Knizhnii_Klub
{
    /// <summary>
    /// Логика взаимодействия для ProductPage.xaml
    /// </summary>
    public partial class ProductPage : Page
    {
        List<Product> products;
        List<ProductsInOrder> productsInOrder;
        Order order;

        public Visibility ShowOrderVisivle
        {
            get
            {
                if (products != null || products.Count != 0)
                {
                    return Visibility.Visible;
                }
                return Visibility.Hidden;
            }
            set { }
        }

        public ProductPage()
        {
            InitializeComponent();
            ListProducts.ItemsSource = DE3Entities.GetContext().Product.ToList();

        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        private void AddInOrder_Click(object sender, RoutedEventArgs e)
        {
            if (products == null)
            {
                products = new List<Product>();
            }
            if ((sender as MenuItem).DataContext is Product product)
            {
                products.Add(product);
            }
            MessageBox.Show("Продукт добавлен в корзину.");

            ShowOrder.Visibility = Visibility.Visible;
        }



        private void ShowOrder_Click(object sender, RoutedEventArgs e)
        {
            Pages.OrderView orderView = new Pages.OrderView(products);
            Manager.MainFrame.Navigate(new Pages.OrderView(products));
        }

        private void BtnAddInOrder_Click(object sender, RoutedEventArgs e)
        {
            if (products == null)
            {
                products = new List<Product>();
            }
            Product selectedProduct = (Product)ListProducts.SelectedItem;
            products.Add(selectedProduct);
            MessageBox.Show("Продукт добавлен в корзину.");

            ShowOrder.Visibility = Visibility.Visible;

        }
    }
}





