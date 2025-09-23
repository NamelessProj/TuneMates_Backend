using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Utils;

namespace TuneMates_Backend.Controller
{
    public static class RoomController
    {
        public static async Task<IResult> GetAllRoomsFromUser(AppDbContext db, int id)
        {
            var rooms = await db.Rooms.Where(r => r.UserId == id).Select(r => new RoomResponse(r)).ToListAsync();
            return TypedResults.Ok(rooms);
        }

        public static async Task<IResult> GetRoomById(AppDbContext db, int id)
        {
            var room = await db.Rooms.FindAsync(id);
            if (room == null)
                return TypedResults.NotFound("Room not found.");
            return TypedResults.Ok(new RoomResponse(room));
        }

        public static async Task<IResult> GetRoomBySlug(AppDbContext db, string slug)
        {
            var room = await db.Rooms.FirstOrDefaultAsync(r => r.Slug == slug);
            if (room == null)
                return TypedResults.NotFound("Room not found.");
            return TypedResults.Ok(new RoomResponse(room));
        }

        public static async Task<IResult> CreateRoom(AppDbContext db, [FromBody] RoomDTO roomDto)
        {
            Room room = new()
            {
                Name = roomDto.Name,
                IsActive = roomDto.IsActive
            };

            // Check for null or empty fields
            if (string.IsNullOrWhiteSpace(room.Name) || string.IsNullOrWhiteSpace(roomDto.Password))
                return TypedResults.BadRequest("Name and Password are required.");

            // Check if the user already has a room with the same name
            //if (await db.Rooms.AnyAsync(r => r.Name == room.Name && r.UserId == roomDto.UserId))
            //    return TypedResults.Conflict("You already have a room with this name. Please choose a different name.");

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

        public static async Task<IResult> EditRoom(AppDbContext db, [FromBody] RoomDTO roomDto, int id)
        {
            var room = await db.Rooms.FindAsync(id);

            if (room == null)
                return TypedResults.NotFound("Room not found.");

            // Update only the fields that are provided in the DTO
            if (!string.IsNullOrWhiteSpace(roomDto.SpotifyPlaylistId))
                room.SpotifyPlaylistId = roomDto.SpotifyPlaylistId;

            // If a new Name is provided, update it and regenerate the Slug
            if (!string.IsNullOrWhiteSpace(roomDto.Name) && !roomDto.Name.Equals(room.Name))
            {
                string oldName = room.Name;
                room.Name = roomDto.Name;

                // Check if the user already has a room with the same name
                //if (await db.Rooms.AnyAsync(r => r.Name == room.Name && r.UserId == room.UserId && r.Id != room.Id))
                //    room.Name = oldName; // Revert to old name if duplicate found

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

        public static async Task<IResult> EditRoomPassword(AppDbContext db, [FromBody] RoomDTO roomDto, int id)
        {
            var room = await db.Rooms.FindAsync(id);

            if (room == null)
                return TypedResults.NotFound("Room not found.");

            if (string.IsNullOrWhiteSpace(roomDto.Password) || string.IsNullOrWhiteSpace(roomDto.PasswordConfirm))
                return TypedResults.BadRequest("Password and PasswordConfirm are required.");

            if (!roomDto.Password.Equals(roomDto.PasswordConfirm))
                return TypedResults.BadRequest("Password and PasswordConfirm do not match.");

            // Hash the new password before storing it
            room.PasswordHash = Argon2.Hash(roomDto.Password);
            await db.SaveChangesAsync();
            return TypedResults.Ok(new RoomResponse(room));
        }

        public static async Task<IResult> DeleteRoom(AppDbContext db, int id)
        {
            var room = await db.Rooms.FindAsync(id);

            if (room == null)
                return TypedResults.NotFound("Room not found.");

            db.Rooms.Remove(room);
            await db.SaveChangesAsync();
            return TypedResults.Ok("Room deleted successfully.");
        }
    }
}