# Jack Henry coding challenge
Coding challenge for Jack Henry.  C# base SubRedit reader.

This project was created in Visual Studio Code on Ubuntu.  It uses a devcontainer to provide the correct .Net environment.
The program can be run in a Linux container.

## ReditChallenge Solution

This solution includes 4 projects.

### ReditChallenge.Client

This is a Blazor application that runs the UI.  For the purposes of this application, I will used Radzen's Blazor library for UI elements.
I will be using one of Radzen's simple templates as a demonstration.

While the client could contact the Redit API directly, I prefer to use the server as the reverse proxy.  This allows the server to maintain the Redit credentials without exposing them to the client.  This removes possible CORS issues, as the client only talks to the server directly.

### ReditChallenge.Server

The server hosts the WASM client.  The server also hosts the API that serves as a reverse proxy to the redit api.

### ReditChallenge.Shared

The benefit of the C# WASM is that it can share the object definitions between the server and the client.  A similar benefit can be achieved with gRPC, however since I am usuing .net for both the client and the server, I can use a shared library.
All buisness rules will be in the Shared


### ReditChallenge.Shared.Tests

This is a MS Test project that will run against the Shared Library for unit testing.