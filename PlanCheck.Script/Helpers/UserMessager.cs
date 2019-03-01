using System.Windows;

namespace PlanCheck
{
    public static class UserMessager
    {
        public static void UserMessaged(object sender, UserMessagedEventArgs e)
        {
            MessageBoxImage icon;

            switch (e.Type)
            {
                case UserMessageType.Information:
                    icon = MessageBoxImage.Information;
                    break;

                case UserMessageType.Warning:
                    icon = MessageBoxImage.Warning;
                    break;

                case UserMessageType.Error:
                    icon = MessageBoxImage.Error;
                    break;

                default:
                    icon = MessageBoxImage.None;
                    break;
            }

            MessageBox.Show(e.Message, e.Type.ToString(), MessageBoxButton.OK, icon);
        }
    }
}
