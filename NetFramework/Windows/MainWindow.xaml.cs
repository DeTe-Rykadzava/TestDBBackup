using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using NetFramework.Data;
using MessageBox = System.Windows.MessageBox;

namespace NetFramework.Windows
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Product _selectedProduct = null;

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set => SetField(ref _selectedProduct, value);
        }

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            LoadData();
        }

        private async void LoadData()
        {
            var products = await Model1.Context.Products.ToListAsync();
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                Products.Clear();
                foreach (var product in products)
                {
                    Products.Add(product);
                }
            });
        }

        public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();

        private async void AddProduct()
        {
            try
            {
                var product = new Product()
                {
                    Name = "",
                    Details = "",
                    Cost = 0.0M
                };
                Model1.Context.Products.Add(product);
                await Model1.Context.SaveChangesAsync();
                MessageBox.Show("Запись добавлена в базу", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                MessageBox.Show("Произошла ошибка при создании продукта", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void DeleteProduct()
        {
            if (SelectedProduct == null)
            {
                MessageBox.Show("Вы не выбрали запись для удаления", "Info", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            try
            {
                var productInBase =
                    await Model1.Context.Products.FirstOrDefaultAsync(x => x.Id == SelectedProduct.Id);
                if (productInBase == null)
                {
                    MessageBox.Show("Данная запись не найдена в базе данных, удалять нечего", "Info",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                Model1.Context.Entry(productInBase).State = EntityState.Deleted;
                MessageBox.Show("Запись помечена на удаление, сохраните изменения чтобы они вступили в силу", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                MessageBox.Show("Произошла ошибка при удалении записи", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void SaveChanges()
        {
            try
            {
                await Model1.Context.SaveChangesAsync();
                MessageBox.Show("Данные были сохранены", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                MessageBox.Show("Произошла ошибка при сохранении данных", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void BackupDatabase()
        {
            try
            {
                MessageBox.Show(
                    "Выберите путь где необходимо сохранить файл резервной копии базы данных (работает только если база данных - локальная)",
                    "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                using (var dialog = new FolderBrowserDialog())
                {
                    var result = dialog.ShowDialog();
                    if (result == null || result == System.Windows.Forms.DialogResult.Cancel)
                    {
                        MessageBox.Show("Вы закрыли окно выбора пути по которому следует сохранить файл", "Closed",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var path = new SqlParameter("@path", dialog.SelectedPath);
                    var backup =
                        await Model1.Context.Database.ExecuteSqlCommandAsync("execute BackUpDatabase @path", path); // не хочет адекватно работать почему-то
                    MessageBox.Show("Успешно", "Success", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                MessageBox.Show("Произошла ошибка при создания резервной копии базы данных", "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void UndoChanges()
        {
            try
            {
                var changesEntries = Model1.Context.ChangeTracker.Entries()
                    .Where(x => x.State != EntityState.Unchanged).ToList();
                foreach (var entry in changesEntries)
                {
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            entry.CurrentValues.SetValues(entry.OriginalValues);
                            entry.State = EntityState.Unchanged;
                            break;
                        case EntityState.Added:
                            entry.State = EntityState.Detached;
                            break;
                        case EntityState.Deleted:
                            entry.State = EntityState.Unchanged;
                            break;
                    }
                }

                await Model1.Context.SaveChangesAsync();
                LoadData();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                MessageBox.Show("Произошла ошибка при откате данных", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            AddProduct();
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            DeleteProduct();
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();
        }

        private void UndoChanges_Click(object sender, RoutedEventArgs e)
        {
            UndoChanges();
        }

        private void BackupDataBase_Click(object sender, RoutedEventArgs e)
        {
            BackupDatabase();
        }
    }
}