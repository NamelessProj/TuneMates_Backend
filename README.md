﻿![.NET version](https://img.shields.io/badge/.NET_9-512BD4?style=for-the-badge)
![Project licence](https://img.shields.io/github/license/NamelessProj/TuneMates_Backend?style=for-the-badge)
![Repo size](https://img.shields.io/github/repo-size/NamelessProj/TuneMates_Backend?style=for-the-badge)

# TuneMates - Backend
TuneMates is a web application that allows users to create and join music listening rooms, 
where they can share and enjoy music together in real-time. The backend is built using .NET 9 in C#.

## Features
- User authentication and authorization
- Create and join rooms to add songs to a shared Spotify playlist
- JWT token generation for secure communication
- Integration with Spotify API for song search and playlist management
- Background services for cleaning up expired tokens and proposals
- Database management using Entity Framework Core with Supabase as the backend service
- Password-protected rooms for added security

## Libraries and Tools
- [.NET 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0): The framework used to build the backend.
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/): ORM for database interactions.
- [JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-9.0): For handling JWT authentication.
- [Supabase](https://supabase.com/): Backend as a service platform used for database and authentication.
- [Spotify API](https://developer.spotify.com/documentation/web-api/): For integrating Spotify functionalities.

## Running the Application
1. Clone the repository:
   ```bash
   git clone https://github.com/NamelessProj/TuneMates_Backend.git 
   dotnet run
   ```
1. Use a tool like Postman to interact with the API endpoints at `https://localhost:7016`.

### Appsettings
Make sure to configure _(maybe even create)_ your `appsettings.json` file with the necessary settings, such as database connection strings and JWT secret keys.

You can find a sample configuration in [`appsettings.sample.json`](/appsettings.sample.json).

Example:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "supabase connection string here"
  },
  "Secrets": {
    "EncryptKey64": "abcdefghijklmnopqrstuvwxyz1234567890ABCDEFG="
  },
  "Jwt": {
    "Key": "don't worry about it, this is a good key at least i hope so",
    "Issuer": "TuneMates",
    "Audience": "TuneMatesUsers"
  },
  "CleanupService": {
    "TokenIntervalHours": 3,
    "ProposalIntervalHours": 3
  },
  "Spotify": {
    "ClientId": "your_spotify_client_id",
    "ClientSecret": "your_spotify_client_secret",
    "RedirectUri": "your_redirect_uri"
  }
}
```

The `ConnectionStrings:DefaultConnection` should be your database connection string.

The `Secrets:EncryptKey64` should be a base64-encoded string of 64 characters.

The `Jwt:Key` should be a strong secret key used for signing JWT tokens.

The `Jwt:Issuer` and `Jwt:Audience` should be set to appropriate values for your application.

The `CleanupService` section contains settings for background services that clean up expired tokens and proposals. You can adjust the intervals as needed (in hours).

The `Spotify` section should contain your Spotify API credentials.

>[!TIP]
> ### Spotify Developer Account
> To use Spotify's API, you need to create a Spotify Developer account and register your application to obtain the `ClientId` and `ClientSecret`. You can do this at [Spotify for Developers](https://developer.spotify.com/dashboard/applications).

## JWT Authentication
The application uses JWT (JSON Web Tokens) for authentication.
After a user logs in, they will receive a JWT token that must be included in the `Authorization` header of subsequent requests to protected endpoints.
The token should be prefixed with `Bearer `.

### Example
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```
There has to be a space between `Bearer` and the token.

## API Endpoints
> 🔒: Endpoints that require authentication.
### Users
- `POST /api/users/register`: Register a new user.
- `POST /api/users/login`: Login and receive a JWT token.
- 🔒 `GET /api/users/me`: Get the current user's information.
- 🔒 `PUT /api/users/me`: Update the current user's information.
- 🔒 `PUT /api/users/me/password`: Update the current user's password.
- 🔒 `DELETE /api/users/me`: Delete the current user's account.

### Rooms
- 🔒 `POST /api/rooms`: Create a new room.
- 🔒 `GET /api/rooms`: Get a list of all rooms from the authenticated user.
- `GET /api/rooms/slug/{slug:string}`: Get details of a specific room by its slug. _Requires room password_
- 🔒 `PUT /api/rooms/{id:int}`: Update a room by its id.
- 🔒 `PUT /api/rooms/password/{id:int}`: Update a room's password by its id.
- 🔒 `DELETE /api/rooms/{id:int}`: Delete a room by its id.

### Songs
- `POST /api/songs/room/{roomId:int}/{songId:string}`: Add a new song to a room using a Spotify song ID. The song will be in "Pending" status by default.
- 🔒 `GET /api/songs/room/{roomId:int}`: Get all songs in a specific room.
- 🔒 `GET /api/songs/room/{roomId:int}/status/{status:int}`: Get songs in a specific room by their status ([SongStatus](/DataBase/SongStatus.cs) Pending: `0`, Approved: `1`, Refused: `2`).

### Spotify
- `GET /api/spotify/oathlink`: Get the Spotify OAuth link to authorize the application.
- `GET /api/spotify/search/{q:string}/{offset:int}/{market:string}`: Search for songs on Spotify by a query string, with pagination and market specification.
- 🔒 `POST /api/spotify/playlist/{roomId:int}/{songId:int}`: Add a new song directly to the room's Spotify playlist using the songId from the database. The song will be in "Approved" status after being added to the playlist.