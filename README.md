# Jack Henry coding challenge
Coding challenge for Jack Henry.  C# base SubReddit reader.

## Programming Assignment: 

Reddit, much like other social media platforms, provides a way for users to communicate their interests etc. For this exercise, we would like to see you build an application that listens to your choice of subreddits (best to choose one with a good amount of posts). You can use this link to help identify one that interests you.  We'd like to see this as a .NET/C# 6/7 application, and you are free to use any 3rd party libraries you would like.

Your app should consume the posts from your chosen subreddit in near real time and keep track of the following statistics between the time your application starts until it ends:

* Posts with most up votes
* Users with most posts

Your app should also provide some way to report these values to a user (periodically log to terminal, return from RESTful web service, etc.). If there are other interesting statistics you’d like to collect, that would be great. There is no need to store this data in a database; keeping everything in-memory is fine. That said, you should think about how you would persist data if that was a requirement. 


To acquire near real time statistics from Reddit, you will need to continuously request data from Reddit's rest APIs.  Reddit implements rate limiting and provides details regarding rate limit used, rate limit remaining, and rate limit reset period via response headers.  Your application should use these values to control throughput in an even and consistent manner while utilizing a high percentage of the available request rate.


It’s very important that the various application processes do not block each other as Reddit can have a high volume on many of their subreddits.  The app should process posts as concurrently as possible to take advantage of available computing resources. While we are only asking to track a single subreddit, you should be thinking about his you could scale up your app to handle multiple subreddits.

While designing and developing this application, you should keep SOLID principles in mind. Although this is a code challenge, we are looking for patterns that could scale and are loosely coupled to external systems / dependencies. In that same theme, there should be some level of error handling and unit testing. The submission should contain code that you would consider production ready.

When you're finished, please put your project in a repository on either GitHub or Bitbucket and send your recruiter link. Please be sure to provide guidance as to where the Reddit API Token values are located so that the team reviewing the code can replace/configure the value. After review, we may follow-up with an interview session with questions for you about your code and the choices made in design/implementation.

 
While the coding exercise is intended to be an interesting and fun challenge, we are interested in seeing your best work - aspects that go beyond merely functional code, that demonstrate professionalism and pride in your work.  We look forward to your submission!




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

