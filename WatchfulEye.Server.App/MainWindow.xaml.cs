using System;
using System.Collections.Generic;
using System.IO;
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
using WatchfulEye.Server.App.Components;
using WatchfulEye.Server.Eyes;
using WatchfulEye.Shared.Utility;
using Path = System.IO.Path;

namespace WatchfulEye.Server.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Dictionary<EyeSocket, EyeSocketDisplay> _pages;
        private readonly Queue<EyeSocketDisplay> _free;
        
        public MainWindow()
        {
            Logging.Info("Starting main window");
            InitializeComponent();
            
            _pages = new Dictionary<EyeSocket, EyeSocketDisplay>();
            _free = new Queue<EyeSocketDisplay>();
            _free.Enqueue(Socket1);
            _free.Enqueue(Socket2);
            _free.Enqueue(Socket3);
            _free.Enqueue(Socket4);
            
            Logging.Info("Starting EyeManager");
            EyeManager.OnEyeSocketAdded += HandleSocketAdd;
            EyeManager.OnEyeSocketRemoved += HandleSocketRemoved;
            EyeManager.StartNetworkDiscovery();
        }

        private void HandleSocketAdd(EyeSocket eye)
        {
            if (_pages.ContainsKey(eye))
            {
                Logging.Error($"EyeSocket with name {eye.Name} already is registered");
                return;
            }

            if (_free.Count == 0)
            {
                Logging.Error("At max capacitiy of EyeSockets");
                return;
            }

            EyeSocketDisplay page = _free.Dequeue();
            page.AssignEye(eye);
            _pages.Add(eye, page);
            Logging.Info("Added eye socket to page");
        }

        private void HandleSocketRemoved(EyeSocket remove)
        {
            if (!_pages.ContainsKey(remove))
            {
                Logging.Info("Removed socket was not registered here");
                return;
            }

            EyeSocketDisplay page = _pages[remove];
            page.UnassignEye();
            _free.Enqueue(page);
            _pages.Remove(remove);
        }
    }
}