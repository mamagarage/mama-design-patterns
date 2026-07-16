using MaMa.Mediator

public static partial class Program
{
    // Demonstrates interaction between ChatMediator and ChatUser
    public static void Main()
    {
        ChatMediator mediator = new ChatMediator();

        ChatUser user1 = new ChatUser(mediator, "User 1");
        ChatUser user2 = new ChatUser(mediator, "User 2");
        ChatUser user3 = new ChatUser(mediator, "User 3");

        mediator.RegisterUser(user1);
        mediator.RegisterUser(user2);
        mediator.RegisterUser(user3);

        user1.SendMessage("Hello, everyone!");
        
        user2.SendMessage("Hi User 1, good to see you.");
        
        user3.SendMessage("Welcome!");
    }
}

