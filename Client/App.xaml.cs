using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using BaldaServer;
using System.IO;
using System.Windows.Threading;

namespace Balda
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 

    public partial class App : Application
    {
        public static ServiceGame Proxy { get; set; }
        public static string login { get; set; }
        public static Dictionary<string, BitmapImage> ImgDictionary = new Dictionary<string, BitmapImage>();
        public bool GameExit;
        public static bool IsNewGame;
        public static List<Window> myWindows = new List<Window>();
        public static StartScreen stScreen;

        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Критическая ошибка");
            Application.Current.Shutdown();
        }
    }
}
