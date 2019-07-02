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
    /// AutoPauseOption.xaml 的互動邏輯
    /// </summary>
    public partial class AutoPauseOption : Window
    {
        FPlayerDataBase db;
        public AutoPauseOption(FPlayerDataBase db)
        {
            InitializeComponent();
            this.db = db;
        }
        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (PauseIgnore ignore in db.ignores)
            {
                CheckBox checkbox = new CheckBox()
                {
                    Content = ignore.title,
                    IsChecked = ignore.enable
                };
                listIgnore.Items.Add(checkbox);
            }
            foreach (PauseIgnore ignore in db.recentlyIgnores)
            {
                ListViewItem item = new ListViewItem()
                {
                    Content = ignore.title
                };
                listRecently.Items.Add(item);
            }
        }
        private void btnDelete_Clicked(object sender, RoutedEventArgs e)
        {
            if (listIgnore.SelectedIndex >= 0)
            {
                db.ignores.RemoveAt(listIgnore.SelectedIndex);
                listIgnore.Items.RemoveAt(listIgnore.SelectedIndex);
            }
        }
        private void btnOption_Clicked(object sender, RoutedEventArgs e)
        {
            if (listIgnore.SelectedIndex >= 0)
            {
                PauseIgnore ignore = db.recentlyIgnores[listIgnore.SelectedIndex];
                IgnoreOption page = new IgnoreOption(ignore);
                bool? result = page.ShowDialog();
                if (result == true)
                {
                    CheckBox checkbox = (CheckBox)listIgnore.SelectedItem;
                    checkbox.Content = ignore.title;
                }
            }
        }
        private void btnAdd_Clicked(object sender, RoutedEventArgs e)
        {
            if (listRecently.SelectedIndex >= 0)
            {
                PauseIgnore ignore = db.recentlyIgnores[listRecently.SelectedIndex];
                IgnoreOption page = new IgnoreOption(ignore);
                bool? result = page.ShowDialog();
                if (result == true)
                {
                    db.recentlyIgnores.RemoveAt(listRecently.SelectedIndex);
                    listRecently.Items.RemoveAt(listRecently.SelectedIndex);
                    ignore.enable = true;
                    db.ignores.Add(ignore);
                    CheckBox checkbox = new CheckBox()
                    {
                        Content = ignore.title,
                        IsChecked = ignore.enable
                    };
                    listIgnore.Items.Add(checkbox);
                }
            }
        }
        private void window_Closed(object sender, EventArgs e)
        {
            for (int index = 0; index < db.ignores.Count; index++)
            {
                CheckBox checkbox = (CheckBox)listIgnore.Items[index];
                PauseIgnore ignore = db.ignores[index];
                ignore.enable = checkbox.IsChecked == true;
            }
        }
    }
}
