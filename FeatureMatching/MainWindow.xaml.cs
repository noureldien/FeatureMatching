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

namespace FeatureMatching
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Variables
        
        private Tracker tracker;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
        }
        
        #endregion

        #region Event Handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tracker = new Tracker(labelFrameCounter);
            tracker.InitilizeCamera();
            tracker.StartProcessing();
        }

        #endregion

        #region Private Methods

        #endregion
    }
}
