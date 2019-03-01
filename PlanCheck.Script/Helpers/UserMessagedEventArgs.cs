using System;

namespace PlanCheck
{
    public enum UserMessageType
    {
        Information,
        Warning,
        Error
    }

    public class UserMessagedEventArgs : EventArgs
    {
        public string Message { get; set; }
        public UserMessageType Type { get; set; }

        public UserMessagedEventArgs(string message, UserMessageType type)
        {
            Message = message;
            Type = type;
        }
    }
}
