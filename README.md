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
   git clone fdfdffgsdfsdfs 
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
  }
}
```

The `ConnectionStrings:DefaultConnection` should be your database connection string.

The `Secrets:EncryptKey64` should be a base64-encoded string of 64 characters.

The `Jwt:Key` should be a strong secret key used for signing JWT tokens.

The `Jwt:Issuer` and `Jwt:Audience` should be set to appropriate values for your application.

## API Endpoints
### Users
- `POST /api/users/register`: Register a new user.
- `POST /api/users/login`: Login and receive a JWT token.
- `GET /api/users/me`: Get the current user's information (requires authentication).
- `PUT /api/users/me`: Update the current user's information (requires authentication).
- `PUT /api/users/me/password`: Update the current user's password (requires authentication).
- `DELETE /api/users/me`: Delete the current user's account (requires authentication).

### Rooms
- `POST /api/rooms`: Create a new room (requires authentication).
- `GET /api/rooms`: Get a list of all rooms from the authenticated user (rooms that the user owns).
- `GET /api/rooms/{slug}`: Get details of a specific room by its slug.
- `PUT /api/rooms/{id}`: Update a room by its id (requires authentication and ownership).
- `PUT /api/rooms/password/{id}`: Update a room's password by its id (requires authentication and ownership).
- `DELETE /api/rooms/{slug}`: Delete a room by its slug (requires authentication and ownership).