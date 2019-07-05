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

namespace DAQ
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindowView : Window
    {
        public MainWindowView()
        {
            InitializeComponent();
        }

        private void Bell_Click(object sender, RoutedEventArgs e)
        {
            if (G_MSG.Width < 250)
            {
                G_MSG.Width = 250;
            }
            else
            {
                G_MSG.Width = 0;
            }
        }

        private void Power_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
       
        private void Grid_MouseMove(object sender, MouseEventArgs e)
          {        
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        if( e.GetPosition(this).Y<50)
                        DragMove();
                    }
                }
                ));
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {      
            var index = ListMenu.SelectedIndex;     
            transrect.OnApplyTemplate();
            rect.Margin = new Thickness(0, 100 + 60 * index, 0, 0);
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ClickCount==2)
            {
                if (this.WindowState == WindowState.Normal)
                    WindowState = WindowState.Maximized;
                else
                    WindowState = WindowState.Normal;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(gdleft.Width<200)
            {
                gdleft.Width = 220;
            }
            else
            {
                gdleft.Width = 79;
            }
        }

    }
}
