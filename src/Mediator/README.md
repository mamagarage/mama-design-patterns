# Mediator Design Pattern
Define an object that encapsulates how a set of objects interact. 
Mediator promotes loose coupling by keeping objects from referring to each other
explicitly, and it lets you vary their interaction independently.


```mermaid
---
config:
  showSequenceNumbers: true
  theme: neutral
---

%%{init: {"flowchart": {"diagramPadding": 150}}}%%
flowchart LR
    A[ChatUser Alice]
    B[ChatUser Bob]
    C[ChatUser Charlie]
    M[ChatMediator]

    A -->|SendMessage| M
    M -->|ReceiveMessage| B
    M -->|ReceiveMessage| C

    B -->|SendMessage| M
    M -->|ReceiveMessage| A
    M -->|ReceiveMessage| C

    C -->|SendMessage| M
    M -->|ReceiveMessage| A
    M -->|ReceiveMessage| B
```
