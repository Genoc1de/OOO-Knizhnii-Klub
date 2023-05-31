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
        public ProductPage()
        {
            InitializeComponent();
            ListProducts.ItemsSource = DE3Entities.GetContext().Product.ToList();

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
                try{
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

        private void Cart_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {

            Manager.MainFrame.Navigate(new EditPage((sender as Button).DataContext as Product));
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                DE3Entities.GetContext().ChangeTracker.Entries().ToList().ForEach(p => p.Reload());
                ListProducts.ItemsSource = DE3Entities.GetContext().Product.ToList();
            }
        }
    }
}
