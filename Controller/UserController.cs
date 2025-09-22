using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;

namespace TuneMates_Backend.Controller
{
    public static class UserController
    {
        public static async Task<IResult> GetAllUser(AppDbContext db)
        {
            var users = await db.Users.Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                CreatedAt = u.CreatedAt
            }).ToListAsync();

            return TypedResults.Ok(users);
        }

        public static async Task<IResult> GetUserById(AppDbContext db, int id)
        {
            var user = await db.Users.FindAsync(id);

            if (user == null)
                return TypedResults.NotFound("User not found.");

            return TypedResults.Ok(new UserResponse(user));
        }

        public static async Task<IResult> CreateUser(AppDbContext db, UserDTO userDto)
        {
            User user = new User()
            {
                Username = userDto.Username,
                Email = userDto.Email
            };

            // Check for null or empty fields
            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(userDto.Password))
                return TypedResults.BadRequest("Username, Email, and Password are required.");

            // Check if the email is already in use
            if (await db.Users.AnyAsync(u => u.Email == user.Email))
                return TypedResults.Conflict("Email is already in use.");

            // Hash the password before storing it. We do this step last to avoid unnecessary computation.
            user.PasswordHash = Argon2.Hash(userDto.Password);

            db.Users.Add(user);
            await db.SaveChangesAsync();
            return TypedResults.Ok(new UserResponse(user));
        }

        public static async Task<IResult> EditUser()
        {
            return TypedResults.Ok();
        }

        public static async Task<IResult> DeleteUser()
        {
            return TypedResults.Ok();
        }
    }
}