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
    /// Interaction logic for Notification.xaml
    /// </summary>
    public partial class Notification : Window
    {
        public Notification()
        {
            InitializeComponent();
            //notification.Content = "Watch your proximity to the screen";
        }

        public Notification(int emotion)
        {
            InitializeComponent();
            switch (emotion)
            {
                case 0:
                    notification.Content = "You're both tired and frustrated!";
                    break;
                case 1:
                    notification.Content = "Time to sleep, you are tired!";
                    break;
                case 2:
                    notification.Content = "You are frustrated, take a breather!";
                    break;
                           
            }
        }
    }
}
