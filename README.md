# Checkers Game – .NET Client-Server Project

A .NET-based checkers game project built with a Windows Forms client, ASP.NET Core server, and SQL Server database.

---

## About the Project

This project is a client-server checkers game system developed as a final .NET project.

The system is divided into three main parts:

* **GameClient** – a Windows Forms desktop application used by the player.
* **GameServer** – an ASP.NET Core server that manages players, games, moves, and database operations.
* **DBFiles** – SQL Server database files and scripts used to create and manage the project database.

The goal of the project is to demonstrate practical software development using .NET, database integration, client-server communication, and a clear multi-project structure.

---

## Main Features

* Player login using Player ID
* Interactive checkers game board
* Client-server communication
* Game and move tracking
* Local and server-side data handling
* SQL Server database integration
* Player management pages
* Database queries and game records
* Replay game option

---

## Project Structure

```text
.NETFinalProject
│
├── GameClient
│   └── Windows Forms client application
│
├── GameServer
│   └── ASP.NET Core server application
│
├── DBFiles
│   └── SQL Server database scripts and files
│
└── Known Issues
    └── Notes about current limitations or problems
```

---

## Tech Stack

### Languages & Frameworks

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge\&logo=csharp\&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge\&logo=dotnet\&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-512BD4?style=for-the-badge\&logo=dotnet\&logoColor=white)

### Database

![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge\&logo=microsoftsqlserver\&logoColor=white)
![Entity Framework](https://img.shields.io/badge/Entity_Framework-68217A?style=for-the-badge\&logo=dotnet\&logoColor=white)

### Tools

![Visual Studio](https://img.shields.io/badge/Visual_Studio-5C2D91?style=for-the-badge\&logo=visualstudio\&logoColor=white)
![GitHub](https://img.shields.io/badge/GitHub-181717?style=for-the-badge\&logo=github\&logoColor=white)

---

## Development Focus

This project focuses on building a complete software system, not just a single application.

It combines desktop development, backend server logic, database design, and communication between different parts of the system.

The project helped me practice:

* Object-oriented programming in C#
* Building a Windows Forms application
* Creating an ASP.NET Core server
* Working with Entity Framework
* Designing and using a SQL Server database
* Organizing a multi-project solution
* Managing real project files using GitHub

---

## How to Run

1. Open the **GameServer** project in Visual Studio.
2. Make sure the SQL Server connection string is configured correctly.
3. Create or restore the database using the files inside `DBFiles`.
4. Run the server.
5. Open the **GameClient** project.
6. Run the client and connect to the local server.

> Note: The server should be running before starting the client.

---

## Project Status

The project is functional and was developed as part of a final .NET course project.

Some parts may still be improved in the future, such as better UI design, stronger validation, cleaner deployment setup, and improved error handling.

---

## Author

**Jolian Habib**
Software Engineering Student at Afeka College

Crafting clean, practical, and real-world software through thoughtful design and continuous learning.
