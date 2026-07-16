# Mediator Design Pattern

 <a href="https://github.com/mamagarage/mama-design-patterns/tree/main/src/Mediator"> 
    <img alt="" width="400" src="https://github.com/mamagarage/mama-design-patterns/blob/main/img/mediator.jpeg?raw=true" alt=""/>
 </a>

```mermaid
 classDiagram
    class ChatMediator {
        +RegisterUser()
        +SendMessage()
    }

    class ChatUser {
        +Name
        +SendMessage()
        +ReceiveMessage()
    }

    class IUser {
        <<interface>>
    }

    ChatUser ..|> IUser
    ChatUser --> ChatMediator
    ChatMediator --> IUser
```
