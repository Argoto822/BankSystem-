using System.Windows;
using System.Windows.Controls;

namespace BankSystem.Views
{
    public partial class OpenAccountDialog : Window
    {
        public string SelectedAccountType { get; private set; }

        public OpenAccountDialog()
        {
            InitializeComponent();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (cmbAccountType.SelectedItem is ComboBoxItem item)
            {
                SelectedAccountType = item.Tag.ToString();
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}