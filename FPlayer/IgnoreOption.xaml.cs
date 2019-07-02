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

namespace FPlayer
{
    /// <summary>
    /// IgnoreOption.xaml 的互動邏輯
    /// </summary>
    public partial class IgnoreOption : Window
    {
        PauseIgnore ignore;
        public IgnoreOption(PauseIgnore ignore)
        {
            InitializeComponent();
            this.ignore = ignore;
        }
        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            tbPath.Text = ignore.path;
            tbTitle.Text = ignore.title;
        }
        private void btnOK_Clicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            Close();
        }

    }
}
