# Jack Henry coding challenge
Coding challenge for Jack Henry.  C# base SubReddit reader.

This project was created in Visual Studio Code on Ubuntu.  It uses a devcontainer to provide the correct .Net environment.
The program can be run in a Linux container.

## RedditChallenge Solution

This solution includes 4 projects.

### RedditChallenge.Client

This is a Blazor application that runs the UI.  For the purposes of this application, I will used Radzen's Blazor library for UI elements.
I will be using one of Radzen's simple templates as a demonstration.

While the client could contact the Reddit API directly, I prefer to use the server as the reverse proxy.  This allows the server to maintain the Reddit credentials without exposing them to the client.  This removes possible CORS issues, as the client only talks to the server directly.

### RedditChallenge.Server

The server hosts the WASM client.  The server also hosts the API that serves as a reverse proxy to the Reddit api.

### RedditChallenge.Shared

The benefit of the C# WASM is that it can share the object definitions between the server and the client.  A similar benefit can be achieved with gRPC, however since I am usuing .net for both the client and the server, I can use a shared library.
All buisness rules will be in the Shared


### RedditChallenge.Shared.Tests

This is a MS Test project that will run against the Shared Library for unit testing.


### Reddit OAUTH Credentials

For this application, the OATH credentials are found in the .devcontainer folder in a .env file.

You can use make to create an example file.  Then fill the correct values for your account.