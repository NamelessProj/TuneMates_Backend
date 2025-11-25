![.NET version](https://img.shields.io/badge/.NET_9-512BD4?style=for-the-badge)
![Project licence](https://img.shields.io/github/license/NamelessProj/TuneMates_Backend?style=for-the-badge)
![Repo size](https://img.shields.io/github/repo-size/NamelessProj/TuneMates_Backend?style=for-the-badge)

# TuneMates - Backend
TuneMates is a web application that allows users to create and join music listening rooms, 
where they can share and enjoy music together in real-time. The backend is built using .NET 9 in C#.

This repository contains the backend code for the TuneMates application, which handles user authentication, room management, song proposals, and integration with the Spotify API.
You can find the frontend part [here](https://github.com/NamelessProj/TuneMates_Frontend).

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

### Running with .NET
1. Clone the repository:
   ```bash
   git clone https://github.com/NamelessProj/TuneMates_Backend.git 
   dotnet run
   ```
2. Use a tool like Postman to interact with the API endpoints at `https://localhost:7016`.

### Running with Docker
The project includes a Dockerfile for containerized deployment with .NET 9.0, ensuring consistent runtime environments across different hosts.

1. Build the Docker image:
   ```bash
   docker build -t tunemates-backend .
   ```

2. Run the container with your appsettings.json:
   ```bash
   docker run -d \
     -p 8080:8080 \
     -v $(pwd)/appsettings.json:/app/appsettings.json:ro \
     --name tunemates-backend \
     tunemates-backend
   ```

   Or with environment variables:
   ```bash
   docker run -d \
     -p 8080:8080 \
     -e ConnectionStrings__DefaultConnection="your_connection_string" \
     -e Jwt__Key="your_jwt_key" \
     -e Jwt__Issuer="TuneMates" \
     -e Jwt__Audience="TuneMatesUsers" \
     -e Spotify__ClientId="your_spotify_client_id" \
     -e Spotify__ClientSecret="your_spotify_client_secret" \
     --name tunemates-backend \
     tunemates-backend
   ```

3. Access the API at `http://localhost:8080/api`

### Using Docker Compose
Create a `docker-compose.yml` file:
```yaml
version: '3.8'
services:
  backend:
    build: .
    ports:
      - "8080:8080"
    volumes:
      - ./appsettings.json:/app/appsettings.json:ro
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
```

Then run:
```bash
docker-compose up -d
```

### Appsettings
Make sure to configure _(maybe even create)_ your `appsettings.json` file with the necessary settings, such as database connection strings and JWT secret keys.

You can find a sample configuration in [`appsettings.sample.json`](/appsettings.sample.json).

#### Example
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
    "Audience": "TuneMatesUsers",
    "ExpiresInMinutes": 180 // 3 hours
  },
  "CleanupService": {
    "TokenIntervalHours": 3,
    "ProposalIntervalHours": 3,
    "RoomIntervalHours": 24,
    "SpotifyStateIntervalHours": 1
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "https://localhost:5173"
    ],
    "AllowCredentials": true
  },
  "RateLimiting": {
    "GlobalPerMinute": 0,
    "SearchPerMinute": 30,
    "MutationPerMinute":  10
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

The `CleanupService` section contains settings for background services that clean up expired tokens, proposals, etc. You can adjust the intervals as needed (in hours).

The `Cors` section contains settings for Cross-Origin Resource Sharing (CORS). You can specify the allowed origins and whether to allow credentials.

The `RateLimiting` section contains settings for rate limiting API requests. You can specify the number of allowed requests per minute for different categories (global, search, mutation).

The `Spotify` section should contain your Spotify API credentials.

>[!TIP]
> ### Spotify Developer Account
> To use Spotify's API, you need to create a Spotify Developer account and register your application to obtain the `ClientId` and `ClientSecret`. You can do this at [Spotify for Developers](https://developer.spotify.com/dashboard/applications).

## JWT Authentication
The application uses JWT (JSON Web Tokens) for authentication.
After a user logs in, they will receive a JWT token that must be included in the `Authorization` header of subsequent requests to protected endpoints.
The token should be prefixed with `Bearer `.

#### Example
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```
There has to be a space between `Bearer` and the token.

## Database
The application uses Entity Framework Core for database management.

The database schema includes tables for Users, Rooms, Songs, and Tokens.

You can find the queries to create the necessary tables in the [`database.sql`](/database.sql) file.

>[!WARNING]
> ### PostgreSQL and Supabase
> The provided SQL queries are designed for PostgreSQL, which is the database system used by Supabase. If you are using a different database system, you may need to adjust the queries accordingly.


## API Endpoints
> 🔒: Endpoints that require authentication.
### Users
- `POST /api/users/register`: Register a new user.
- `POST /api/users/login`: Login and receive a JWT token.
- 🔒 `POST /api/users/spotify/connect/{code:string}/{state:string}`: Connect the user's Spotify account using the provided authorization code and state.
- 🔒 `POST /api/users/delete/me`: Delete the current user's account.
- 🔒 `GET /api/users/me`: Get the current user's information.
- 🔒 `PUT /api/users/me`: Update the current user's information.
- 🔒 `PUT /api/users/me/password`: Update the current user's password.

### Rooms
- 🔒 `POST /api/rooms`: Create a new room.
- 🔒 `POST /api/rooms/{id:int}`: Delete a room by its id.
- 🔒 `GET /api/rooms`: Get a list of all rooms from the authenticated user.
- `GET /api/rooms/slug/{slug:string}`: Get details of a specific room by its slug. _Requires room password_
- 🔒 `PUT /api/rooms/{id:int}`: Update a room by its id.
- 🔒 `PUT /api/rooms/password/{id:int}`: Update a room's password by its id.

### Songs
- `POST /api/songs/room/{roomId:int}/{songId:string}`: Add a new song to a room using a Spotify song ID. The song will be in "Pending" status by default.
- 🔒 `GET /api/songs/room/{roomId:int}`: Get all songs in a specific room.
- 🔒 `GET /api/songs/room/{roomId:int}/status/{status:int}`: Get songs in a specific room by their status ([SongStatus](/DataBase/SongStatus.cs) Pending: `0`, Approved: `1`, Refused: `2`).

### Spotify
- 🔒 `GET /api/spotify/token`: Get the Spotify access token for the authenticated user.
- `GET /api/spotify/oathlink`: Get the Spotify OAuth link to authorize the application.
- 🔒 `GET /api/spotify/playlist/me`: Get the authenticated user, all their playlists from Spotify.
- `GET /api/spotify/search/{q:string}/{offset:int}/{market:string}`: Search for songs on Spotify by a query string, with pagination and market specification.
- 🔒 `POST /api/spotify/playlist/{roomId:int}/{songId:int}`: Add a new song directly to the room's Spotify playlist using the songId from the database. The song will be in "Approved" status after being added to the playlist.