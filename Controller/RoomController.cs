using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Utils;

namespace TuneMates_Backend.Controller
{
    public static class RoomController
    {
        /// <summary>
        /// Get all rooms belonging to the authenticated user.
        /// </summary>
        /// <param name="http">The HTTP context, used to get the user ID from JWT claims.</param>
        /// <param name="db">The database context.</param>
        /// <returns>A list of <see cref="RoomResponse"/> objects if the user is authenticated, otherwise an unauthorized response.</returns>
        public static async Task<IResult> GetAllRoomsFromUser(HttpContext http, AppDbContext db)
        {
            var id = HelpMethods.GetUserIdFromJwtClaims(http);
            if (id == null)
                return TypedResults.Unauthorized();
            var rooms = await db.Rooms.Where(r => r.UserId == id).Select(r => new RoomResponse(r)).ToListAsync();
            return TypedResults.Ok(rooms);
        }

        ///<summary>
        /// Get a room by its <paramref name="id"/>. Only the owner of the room can access it.
        ///</summary>
        ///<param name="http">The HTTP context, used to get the user ID from JWT claims.</param>
        ///<param name="db">The database context.</param>
        ///<param name="id">The ID of the room to retrieve.</param>
        ///<returns>A <see cref="RoomResponse"/> if found and authorized, otherwise an appropriate error response.</returns>
        public static async Task<IResult> GetRoomById(HttpContext http, AppDbContext db, int id)
        {
            var room = await db.Rooms.FindAsync(id);
            if (room == null)
                return TypedResults.NotFound("Room not found.");

            var userId = HelpMethods.GetUserIdFromJwtClaims(http);
            if (userId == null || room.UserId != userId)
                return TypedResults.Unauthorized();

            return TypedResults.Ok(new RoomResponse(room));
        }

        /// <summary>
        /// Get a room by its slug. This endpoint is public and does not require authentication.
        /// </summary>
        /// <param name="db">The database context.</param>
        /// <param name="slug">The slug of the room to retrieve.</param>
        /// <param name="roomDto">The room data transfer object containing the password for the room.</param>
        /// <returns>A <see cref="RoomResponse"/> if found, otherwise a not found response.</returns>
        public static async Task<IResult> GetRoomBySlug(AppDbContext db, string slug, [FromBody] RoomDTO roomDto)
        {
            var password = roomDto.Password;
            if (string.IsNullOrWhiteSpace(password))
                return TypedResults.BadRequest("Password is required.");

            var room = await db.Rooms.FirstOrDefaultAsync(r => r.Slug == slug);
            if (room == null)
                return TypedResults.NotFound("Room not found.");

            // Verify the provided password against the stored hash
            if (!Argon2.Verify(room.PasswordHash, password))
                return TypedResults.Unauthorized();

            return TypedResults.Ok(new RoomResponse(room));
        }

        /// <summary>
        /// Create a new room for the authenticated user.
        /// </summary>
        /// <param name="http">The HTTP context, used to get the user ID from JWT claims.</param>
        /// <param name="db">The database context.</param>
        /// <param name="roomDto">The room data transfer object containing the details of the room to be created.</param>
        /// <returns>A <see cref="RoomResponse"/> if the room is created successfully, otherwise an appropriate error response.</returns>
        public static async Task<IResult> CreateRoom(HttpContext http, AppDbContext db, [FromBody] RoomDTO roomDto)
        {

            var userId = HelpMethods.GetUserIdFromJwtClaims(http);
            if (userId == null || !await db.Users.AnyAsync(u => u.Id == userId))
                return TypedResults.Unauthorized();

            if (await db.Rooms.CountAsync(r => r.UserId == userId) >= Constants.MaxRoomPerUser)
                return TypedResults.BadRequest($"You have reached the maximum number of rooms allowed ({Constants.MaxRoomPerUser}). Please delete an existing room before creating a new one.");

            Room room = new()
            {
                Name = roomDto.Name.Trim(),
                IsActive = roomDto.IsActive,
                UserId = userId.Value,
            };

            // Check for null or empty fields
            if (string.IsNullOrWhiteSpace(room.Name) || string.IsNullOrWhiteSpace(roomDto.Password))
                return TypedResults.BadRequest("Name and Password are required.");

            // Check if the user already has a room with the same name
            if (await db.Rooms.AnyAsync(r => r.Name == room.Name && r.UserId == userId))
                return TypedResults.Conflict("You already have a room with this name. Please choose a different name.");

            // Create a URL-friendly slug from the room name
            room.Slug = HelpMethods.GenerateSlug(room.Name);

            if (string.IsNullOrWhiteSpace(room.Slug))
                return TypedResults.BadRequest("The provided Name results in an invalid Slug. Please choose a different Name.");

            // Check if the slug is already in use, if so, append the user ID to make it unique
            if (await db.Rooms.AnyAsync(r => r.Slug == room.Slug))
                room.Slug += $"-{room.UserId}";

            // Hash the password before storing it
            room.PasswordHash = Argon2.Hash(roomDto.Password);

            db.Rooms.Add(room);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/room/{room.Id}", new RoomResponse(room));
        }

        /// <summary>
        /// Edit an existing room. Only the owner of the room can edit it.
        /// </summary>
        /// <param name="http">The HTTP context, used to get the user ID from JWT claims.</param>
        /// <param name="db">The database context.</param>
        /// <param name="roomDto">The room data transfer object containing the updated details of the room.</param>
        /// <param name="roomId">The ID of the room to be edited.</param>
        /// <returns>A <see cref="RoomResponse"/> if the room is updated successfully, otherwise an appropriate error response.</returns>
        public static async Task<IResult> EditRoom(HttpContext http, AppDbContext db, [FromBody] RoomDTO roomDto, int roomId)
        {
            var id = HelpMethods.GetUserIdFromJwtClaims(http);
            if (id == null || !await db.Users.AnyAsync(u => u.Id == id))
                return TypedResults.Unauthorized();

            // Get the room by the UserId and roomId to ensure the user owns the room
            var room = await db.Rooms.AnyAsync(r => r.Id == roomId && r.UserId == id) 
                ? await db.Rooms.FindAsync(roomId) 
                : null;

            if (room == null)
                return TypedResults.NotFound("Room not found.");

            // Update only the fields that are provided in the DTO
            if (!string.IsNullOrWhiteSpace(roomDto.SpotifyPlaylistId))
                room.SpotifyPlaylistId = roomDto.SpotifyPlaylistId;

            // If a new Name is provided, update it and regenerate the Slug
            if (!string.IsNullOrWhiteSpace(roomDto.Name) && !roomDto.Name.Trim().Equals(room.Name))
            {
                string oldName = room.Name;
                room.Name = roomDto.Name.Trim();

                // Check if the user already has a room with the same name
                if (await db.Rooms.AnyAsync(r => r.Name == room.Name && r.UserId == room.UserId && r.Id != room.Id))
                    room.Name = oldName; // Revert to old name if duplicate found

                // Regenerate the slug only if the name has changed (case-insensitive)
                if (!oldName.ToLowerInvariant().Equals(room.Name.ToLowerInvariant()))
                {
                    string newSlug = HelpMethods.GenerateSlug(room.Name);

                    // Ensure the new slug is valid or keep the old one
                    if (!string.IsNullOrWhiteSpace(newSlug))
                        room.Slug = newSlug;

                    // Check if the slug is already in use, if so, append the user ID to make it unique
                    if (!newSlug.Equals(room.Slug) && await db.Rooms.AnyAsync(r => r.Slug == room.Slug))
                        room.Slug += $"-{room.UserId}";
                }
            }

            if (roomDto.IsActive != room.IsActive)
                room.IsActive = roomDto.IsActive;

            await db.SaveChangesAsync();
            return TypedResults.Ok(new RoomResponse(room));
        }

        /// <summary>
        /// Edit the password of an existing room. Only the owner of the room can edit it.
        /// </summary>
        /// <param name="http">The HTTP context, used to get the user ID from JWT claims.</param>
        /// <param name="db">The database context.</param>
        /// <param name="roomDto">THe room data transfer object containing the new password details.</param>
        /// <param name="id">The ID of the room to be edited.</param>
        /// <returns>A <see cref="RoomResponse"/> if the password is updated successfully, otherwise an appropriate error response.</returns>
        public static async Task<IResult> EditRoomPassword(HttpContext http, AppDbContext db, [FromBody] RoomDTO roomDto, int id)
        {
            var userId = HelpMethods.GetUserIdFromJwtClaims(http);
            if (userId == null || !await db.Users.AnyAsync(u => u.Id == userId))
                return TypedResults.Unauthorized();

            var room = await db.Rooms.FindAsync(id);

            // Ensure the room exists and belongs to the authenticated user
            if (room == null || room.UserId != userId)
                return TypedResults.Unauthorized();

            if (string.IsNullOrWhiteSpace(roomDto.Password) || string.IsNullOrWhiteSpace(roomDto.PasswordConfirm))
                return TypedResults.BadRequest("Password and PasswordConfirm are required.");

            if (!roomDto.Password.Equals(roomDto.PasswordConfirm))
                return TypedResults.BadRequest("Password and PasswordConfirm do not match.");

            // Hash the new password before storing it
            room.PasswordHash = Argon2.Hash(roomDto.Password);
            await db.SaveChangesAsync();
            return TypedResults.Ok(new RoomResponse(room));
        }

        /// <summary>
        /// Delete a room by its ID. Only the owner of the room can delete it.
        /// </summary>
        /// <param name="http">The HTTP context, used to get the user ID from JWT claims.</param>
        /// <param name="db">The database context.</param>
        /// <param name="id">The ID of the room to be deleted.</param>
        /// <returns>A success message if the room is deleted successfully, otherwise an appropriate error response.</returns>
        public static async Task<IResult> DeleteRoom(HttpContext http, AppDbContext db, int id)
        {
            var room = await db.Rooms.FindAsync(id);

            if (room == null)
                return TypedResults.NotFound("Room not found.");

            var userId = HelpMethods.GetUserIdFromJwtClaims(http);
            if (userId == null || room.UserId != userId)
                return TypedResults.Unauthorized();

            db.Rooms.Remove(room);
            await db.SaveChangesAsync();
            return TypedResults.Ok("Room deleted successfully.");
        }
    }
}