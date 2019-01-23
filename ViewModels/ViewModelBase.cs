using System;
using System.ComponentModel;

namespace PlanCheck
{
    public class ViewModelBase : INotifyPropertyChanged, INotifyUserMessaged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event EventHandler<UserMessagedEventArgs> UserMessaged;

        public void NotifyUserMessaged(string message, UserMessageType type)
        {
            if (UserMessaged != null)
            {
                UserMessaged(this, new UserMessagedEventArgs(message, type));
            }
        }
    }
}
