using System;

namespace PlanCheck
{
    public interface INotifyUserMessaged
    {
        event EventHandler<UserMessagedEventArgs> UserMessaged;
    }
}
