using System.Configuration;
using System.Data;
using System.Windows;

namespace MacroscopTestApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // для удобства окно лучше выделить в отдельную переменную, чем постоянно получать через свойство
        public static MainWindow mainWindow;
    }

}
