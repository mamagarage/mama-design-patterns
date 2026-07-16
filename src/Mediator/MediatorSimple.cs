using System;
using System.Collections.Generic;
using System.Text;

namespace MaMa.Mediator
{
    public class ChatMediator
    {
        private readonly List<IUser> _users;

        public ChatMediator()
        {
            _users = new List<IUser>();
        }

        public void RegisterUser(IUser user)
        {
            _users.Add(user);
        }

        public void SendMessage(string senderName, string message)
        {
            foreach (var user in _users)
            {
                if (!user.Name.Equals(senderName))
                {
                    user.ReceiveMessage(senderName, message);
                }
            }
        }
    }

    public class ChatUser : IUser
    {
        private readonly ChatMediator _mediator;

        public ChatUser(ChatMediator mediator, string name)
        {
            _mediator = mediator;
            Name = name;
        }

        public string Name { get; }

        public void SendMessage(string message)
        {
            Console.WriteLine($"{Name}: Sending message: {message}");
            _mediator.SendMessage(senderName: Name, message: message);
        }

        public void ReceiveMessage(string senderName, string message)
        {
            Console.WriteLine($"{Name}: Received message '{message}' from {senderName}!");
        }
    }

    public interface IUser
    {
        public string Name { get; }

        void SendMessage(string message);

        void ReceiveMessage(string senderName, string message);
    }
}
