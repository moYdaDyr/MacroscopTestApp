using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace MacroscopTestApp
{
    static internal class Constants // константы
    {
        public static readonly int imagesNumber = 3;
        public static readonly int bufferSize = 8192;
        public static readonly string placeholderImagePath = Environment.CurrentDirectory + "/Resources/placeholder.bmp";
    }

    // класс для отслеживания прогресса загрузки
    internal static class DownloadProgressCounter
    {
        static long[] _imagesBytesLoaded;
        static long[] _imagesBytesTotal;
        static int imagesDownloadingCounter;

        static void UpdateDownloadProgress()
        {
            App.mainWindow.DownloadProgressBar.Value = GetProgressPercent();
        }

        static double GetProgressPercent()
        {
            long totalBytesLoadedCounter = 0;
            long totalBytesToLoadCounter = 0;

            for (int i=0;i<Constants.imagesNumber;i++)
            {
                totalBytesLoadedCounter += _imagesBytesLoaded[i];
                totalBytesToLoadCounter += _imagesBytesTotal[i];
            }

            double percent = 0.0;

            if (totalBytesToLoadCounter != 0)
                percent = (double)totalBytesLoadedCounter / totalBytesToLoadCounter;

            // можно сделать оптимальнее, но я пока н буду

            return percent*100.0;
        }

        public static void InitImageDownloadProgress(int index, long bytesToLoad)
        {
            imagesDownloadingCounter++;

            _imagesBytesLoaded[index] = 0;
            _imagesBytesTotal[index] = bytesToLoad;

            UpdateDownloadProgress();
        }

        /* Если несколько изображений загружаются одновременно, то по окончанию загрузки изображения не убираем его прогресс из прогресс бара
         * Но если пользователь отменил загрузку изображения, то его часть убираем
         * Если активных загрузок нет, то очищаем прогресс бар
         */

        public static void EndImageDownloadProgress(int index)
        {
            imagesDownloadingCounter--;

            if (imagesDownloadingCounter == 0)
            {
                Array.Fill(_imagesBytesLoaded, 0);
                Array.Fill(_imagesBytesTotal, 0);

                UpdateDownloadProgress();
            }
        }

        public static void CancelImageDownloadProgress(int index)
        {
            imagesDownloadingCounter--;

            if (imagesDownloadingCounter == 0)
            {
                Array.Fill(_imagesBytesLoaded, 0);
                Array.Fill(_imagesBytesTotal, 0);

                UpdateDownloadProgress();
                return;
            }

            _imagesBytesLoaded[index] = 0;
            _imagesBytesTotal[index] = 0;

            UpdateDownloadProgress();
        }

        public static void UpdateImageDownloadProgress(int index, long newBytesLoaded)
        {
            _imagesBytesLoaded[index] = newBytesLoaded;

            UpdateDownloadProgress();
        }

        static DownloadProgressCounter()
        {
            _imagesBytesLoaded = new long[Constants.imagesNumber];
            _imagesBytesTotal = new long[Constants.imagesNumber];

            Array.Fill(_imagesBytesLoaded, 0);
            Array.Fill(_imagesBytesTotal, 0);

            imagesDownloadingCounter = 0;
        }
    }

    // класс для хранения методов и вспомогательной информации об изображении
    internal class ImageData : INotifyPropertyChanged
    {
        CancellationTokenSource? _cancellationTokenSource;
        string _imageLink;

        public string ImageLink
        {
            get
            {
                return _imageLink;
            }
            set
            {
                _imageLink = value;
                OnPropertyChanged("ImageLink");
            }
        }

        public int Index
        {
            get; private set;
        }

        public ImageData(int index)
        {
            Index = index;
            ImageLink = string.Empty;
            _cancellationTokenSource = null;
        }

        public async Task<MemoryStream?> DownloadInternetImageAsync()
        {
            if (_cancellationTokenSource != null) return null; // проверяем что загрузка не идёт

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancelToken = _cancellationTokenSource.Token;

            MemoryStream imageStream = new MemoryStream();

            HttpClient client = new HttpClient();

            HttpResponseMessage? response = null;

            Stream? internetStream = null;

            long totalBytesToLoad = 0;

            byte[] buffer = new byte[Constants.bufferSize];

            try
            {
                response = await client.GetAsync(ImageLink, HttpCompletionOption.ResponseHeadersRead, cancelToken);

                response.EnsureSuccessStatusCode();

                if (response.Content.Headers.ContentLength != null)
                {
                    totalBytesToLoad = (long)response.Content.Headers.ContentLength;
                }
                else throw new HttpRequestException();

                DownloadProgressCounter.InitImageDownloadProgress(Index, totalBytesToLoad);

                internetStream = await response.Content.ReadAsStreamAsync(cancelToken);

                for (int bytesLoaded = 0; bytesLoaded < totalBytesToLoad;) // передаём так, чтобы было удобнее отслеживть
                {
                    int bytesLoadedThisStep = await internetStream.ReadAsync(buffer, 0, Constants.bufferSize, cancelToken);

                    await imageStream.WriteAsync(buffer, 0, bytesLoadedThisStep, cancelToken);

                    bytesLoaded += bytesLoadedThisStep;

                    DownloadProgressCounter.UpdateImageDownloadProgress(Index, bytesLoaded);
                }

                DownloadProgressCounter.EndImageDownloadProgress(Index);

                return imageStream;
            }
            catch (TaskCanceledException tcex)
            {
                DownloadProgressCounter.CancelImageDownloadProgress(Index);
            }
            catch
            {
                throw;
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;

                if (response != null)
                    response.Dispose();
                if (client != null)
                    client.Dispose();
                if (internetStream != null)
                    internetStream.Dispose();
            }
            return null;
        }

        public void CancelImageDownloading()
        {
            if (_cancellationTokenSource != null)
                _cancellationTokenSource.Cancel();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}