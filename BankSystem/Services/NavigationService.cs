using System.Windows.Controls;

namespace BankSystem.Services
{
    public class NavigationService
    {
        private ContentControl _contentControl;

        public void SetContentControl(ContentControl control) => _contentControl = control;

        public void NavigateTo(UserControl page)
        {
            if (_contentControl != null)
                _contentControl.Content = page;
        }
    }
}