using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MacroscopTestApp
{
    internal class ViewModel : INotifyPropertyChanged
    {
        List<ImageData> internetImagesData;
        List<Image> internetImages;

        BitmapImage GetPlaceholderImage()
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(Constants.placeholderImagePath, UriKind.Absolute);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        BitmapImage LoadImageFromStream(MemoryStream ms)
        {
            ms.Seek(0, SeekOrigin.Begin);

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        async void DownloadImage(int index)
        {
            try
            {
                var memoryStream = await internetImagesData[index].DownloadInternetImageAsync();
                if (memoryStream != null)
                    internetImages[index].Source = LoadImageFromStream(memoryStream);
            }
            catch (InvalidOperationException ioex)
            {
                MessageBox.Show("Некорректный URL для изображения №" + (index+1).ToString() + "!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (HttpRequestException hrex)
            {
                MessageBox.Show("Ошибка при загрузке изображения №" + (index + 1).ToString() + "!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Неизвестная ошибка (изображение №"+ (index + 1).ToString()+")!\n" +ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public ViewModel()
        {
            internetImagesData = new List<ImageData>();
            internetImages = new List<Image>();

            for (int i=0;i<Constants.imagesNumber;i++)
            {
                ImageData imageData = new ImageData(i);
                Image image = new Image();

                App.mainWindow.ImagesGrid.ColumnDefinitions.Add(new ColumnDefinition());

                // чтобы предотвратить дублирование xaml (а заодно добавить возможность работы с произвольным количеством иозображений),
                // элементы интерфейса создаются прямо в коде (радикальное решение)

                TextBlock imageNameTextBlock = new TextBlock();
                imageNameTextBlock.Text = "Изображение №"+(i+1).ToString();
                imageNameTextBlock.Style = App.mainWindow.FindResource("BigTextBlockStyle") as Style;
                Grid.SetColumn(imageNameTextBlock, i);
                Grid.SetRow(imageNameTextBlock, 0);
                App.mainWindow.ImagesGrid.Children.Add(imageNameTextBlock);

                image.Style = App.mainWindow.FindResource("CommonImageStyle") as Style;

                Grid.SetColumn(image, i);
                Grid.SetRow(image, 1);
                App.mainWindow.ImagesGrid.Children.Add(image);

                TextBlock urlHintTextBlock = new TextBlock();
                urlHintTextBlock.Text = "Введите URL изображения №" + (i + 1).ToString()+":";
                urlHintTextBlock.Style = App.mainWindow.FindResource("CommonTextBlockStyle") as Style;
                Grid.SetColumn(urlHintTextBlock, i);
                Grid.SetRow(urlHintTextBlock, 2);
                App.mainWindow.ImagesGrid.Children.Add(urlHintTextBlock);

                TextBox imageLinkTextBox = new TextBox();
                imageLinkTextBox.Style = App.mainWindow.FindResource("CommonTextBoxStyle") as Style;

                Binding imageLinkBinding = new Binding();
                imageLinkBinding.Source = imageData;
                imageLinkBinding.Mode = BindingMode.TwoWay;
                imageLinkBinding.Path = new PropertyPath("ImageLink");
                imageLinkTextBox.SetBinding(TextBox.TextProperty, imageLinkBinding);
                
                Grid.SetColumn(imageLinkTextBox, i);
                Grid.SetRow(imageLinkTextBox, 3);
                App.mainWindow.ImagesGrid.Children.Add(imageLinkTextBox);

                Button StartDownloadButton = new Button();
                StartDownloadButton.Content = "Загрузить изображение";
                StartDownloadButton.Style = App.mainWindow.FindResource("CommonButtonStyle") as Style;
                StartDownloadButton.CommandParameter = i;
                StartDownloadButton.Click += DownloadImageButton_Click;
                Grid.SetColumn(StartDownloadButton, i);
                Grid.SetRow(StartDownloadButton, 4);
                App.mainWindow.ImagesGrid.Children.Add(StartDownloadButton);

                Button CancelDownloadButton = new Button();
                CancelDownloadButton.Content = "Остановить загрузку";
                CancelDownloadButton.Style = App.mainWindow.FindResource("CommonButtonStyle") as Style;
                CancelDownloadButton.CommandParameter = i;
                CancelDownloadButton.Click += CancelImageButton_Click;
                Grid.SetColumn(CancelDownloadButton, i);
                Grid.SetRow(CancelDownloadButton, 5);
                App.mainWindow.ImagesGrid.Children.Add(CancelDownloadButton);

                internetImagesData.Add(imageData);
                internetImages.Add(image);
            }

            App.mainWindow.DownloadAllButton.Click += DownloadAllButton_Click;
        }

        // обработка пользовательских действий

        public void SetStartPlaceholderImages()
        {
            for (int i=0; i<Constants.imagesNumber; i++)
            {
                internetImages[i].Source = GetPlaceholderImage();
            }
        }

        async void DownloadImageButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadImage((int)(sender as Button).CommandParameter);
        }

        private void CancelImageButton_Click(object sender, RoutedEventArgs e)
        {
            internetImagesData[(int)(sender as Button).CommandParameter].CancelImageDownloading();
        }

        void DownloadAllImages()
        {
            for (int i = 0; i < Constants.imagesNumber; i++)
            {
                DownloadImage(i);
            }
        }

        private void DownloadAllButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadAllImages();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
