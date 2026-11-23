using System.Windows;
using System.Windows.Controls;

namespace LearnLanguages.Views.Controls
{
    public partial class CreateProfileMenu : UserControl
    {
        public CreateProfileMenu()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            TxtProfileName.Focus();
        }
    }
}