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
using System.Windows.Shapes;

namespace FaceTutorial
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public static int count;
        public Window1()
        {
            this.InitializeComponent();
            count = 0;
            ChangeLabel();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            count++;
            ChangeLabel();
        }

        private void ChangeLabel()
        {
            switch (count)
            {
                case 0:
                    Command.Content = "Welcome! Click the button once you are ready to calibrate your facial features!";
                    break;
                case 1:
                    Command.Content = "Take a picture with your eyes open.";
                    break;
                case 2:
                    Command.Content = "Squint your eyes and take a picture.";
                    break;
                case 3:
                    Command.Content = "Thank you! Your facial pictures are calibrated.";
                    //CameraVideoDeviceControl.Visibility = Visibility.Hidden;
                    btnOk.Visibility = Visibility.Hidden;
                    break;
            }
        }

        private void Window_IsStylusCapturedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }
    }
}
