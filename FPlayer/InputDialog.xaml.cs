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
    public enum DialogType
    {
        Create,
        Edit
    }
    /// <summary>
    /// InputDialog.xaml 的互動邏輯
    /// </summary>
    public partial class InputDialog : Window
    {
        public InputDialog(DialogType type, string originName)
        {
            InitializeComponent();
            switch(type)
            {
                case DialogType.Create:
                    {
                        this.Title = "請輸入清單標題";
                    }
                    break;
                case DialogType.Edit:
                    {
                        this.Title = "請修改清單標題";
                    }
                    break;
            }
            this.type = type;
            if (originName != null && originName.Length > 0)
            {
                tbInput1.Text = originName;
            }
            tbInput1.Focus();
        }
        DialogType type;

        private void btnOK_Clicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
