![.NET version](https://img.shields.io/badge/.NET_9-512BD4?style=for-the-badge)
![Project licence](https://img.shields.io/github/license/NamelessProj/TuneMates_Backend?style=for-the-badge)
![Repo size](https://img.shields.io/github/repo-size/NamelessProj/TuneMates_Backend?style=for-the-badge)

# TuneMates_Backend
TuneMates is a web application that allows users to create and join music listening rooms, 
where they can share and enjoy music together in real-time. The backend is built using .NET 9 in C#.

## Features
- User authentication and authorization
- Create and join music listening rooms
- JWT token generation for secure communication

## Running the Application
1. Clone the repository:
   ```bash
   git clone https://github.com/NamelessProj/TuneMates_Backend.git 
   dotnet run
   ```
1. Navigate to `http://localhost:7016` in your web browser.
1. Use a tool like Postman to interact with the API endpoints.

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

The `Spotify` section should contain your Spotify API credentials.

>[!TIP]
> ### Spotify Developer Account
> To use Spotify's API, you need to create a Spotify Developer account and register your application to obtain the `ClientId` and `ClientSecret`. You can do this at [Spotify for Developers](https://developer.spotify.com/dashboard/applications).

## JWT Authentication
The application uses JWT (JSON Web Tokens) for authentication. After a user logs in, they will receive a JWT token that must be included in the `Authorization` header of subsequent requests to protected endpoints. The token should be prefixed with `Bearer `.

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
- `GET /api/rooms/{slug}`: Get details of a specific room by its slug.
- 🔒 `PUT /api/rooms/{id}`: Update a room by its id.
- 🔒 `PUT /api/rooms/password/{id}`: Update a room's password by its id.
- 🔒 `DELETE /api/rooms/{id}`: Delete a room by its slug.

### Songs
- 🔒 `GET /api/songs/room/{roomId}`: Get all songs in a specific room.
- 🔒 `GET /api/songs/room/{roomId}/status/{status}`: Get songs in a specific room by their status ([SongStatus](/DataBase/SongStatus.cs) Pending, Approved, Refused).
- `POST /api/songs/room/{roomId}`: Add a new song to a room. The song will be in "Pending" status by default.