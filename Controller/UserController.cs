using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Utils;

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

        public static async Task<IResult> Register(HttpContext http, AppDbContext db, [FromBody] UserDTO userDto)
        {
            if (string.IsNullOrWhiteSpace(userDto.Username) ||
                string.IsNullOrWhiteSpace(userDto.Email) ||
                string.IsNullOrWhiteSpace(userDto.Password) ||
                string.IsNullOrWhiteSpace(userDto.PasswordConfirm))
                return TypedResults.BadRequest("Username, Email, Password, and PasswordConfirm are required.");

            if (!HelpMethods.IsEmailValid(userDto.Email))
                return TypedResults.BadRequest("Invalid email format.");

            if (!HelpMethods.IsPasswordValid(userDto.Password))
                return TypedResults.BadRequest("Password must be at least 8 characters long and include uppercase, lowercase, digit, and special character.");

            if (!userDto.Password.Equals(userDto.PasswordConfirm))
                return TypedResults.BadRequest("Password and PasswordConfirm do not match.");

            if (await HelpMethods.IsEmailInUse(db, userDto.Email))
                return TypedResults.Conflict("Email is already in use.");

            User user = new()
            {
                Username = userDto.Username,
                Email = userDto.Email,
                PasswordHash = Argon2.Hash(userDto.Password)
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            IConfiguration cfg = http.RequestServices.GetRequiredService<IConfiguration>();
            string token = HelpMethods.GenerateJwtToken(cfg, user.Id);

            return TypedResults.Ok(new {
                User = new UserResponse(user),
                Token = token
            });
        }

        public static async Task<IResult> Login(HttpContext http, AppDbContext db, [FromBody] UserDTO userDto)
        {
            if (string.IsNullOrEmpty(userDto.Email) || string.IsNullOrEmpty(userDto.Password))
                return TypedResults.BadRequest("Email and Password are required.");

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);

            if (user == null || !Argon2.Verify(user.PasswordHash, userDto.Password))
                return TypedResults.Unauthorized();

            IConfiguration cfg = http.RequestServices.GetRequiredService<IConfiguration>();
            string token = HelpMethods.GenerateJwtToken(cfg, user.Id);

            return TypedResults.Ok(new {
                User = new UserResponse(user),
                Token = token
            });
        }

        public static async Task<IResult> CreateUser(AppDbContext db, [FromBody] UserDTO userDto)
        {
            User user = new User()
            {
                Username = userDto.Username,
                Email = userDto.Email
            };

            // Check for null or empty fields
            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(userDto.Password) || string.IsNullOrEmpty(userDto.PasswordConfirm))
                return TypedResults.BadRequest("Username, Email, and Password are required.");

            if (!HelpMethods.IsPasswordValid(userDto.Password))
                return TypedResults.BadRequest("Password must be at least 8 characters long and include uppercase, lowercase, digit, and special character.");

            // Check if the email is already in use
            if (await HelpMethods.IsEmailInUse(db, user.Email))
                return TypedResults.Conflict("Email is already in use.");

            // Validate email format
            if (HelpMethods.IsEmailValid(user.Email))
                return TypedResults.BadRequest("Invalid email format.");

            // Check if passwords match
            if (!userDto.Password.Equals(userDto.PasswordConfirm))
                return TypedResults.BadRequest("Password and the confirmation do not match.");

            // Hash the password before storing it. We do this step last to avoid unnecessary computation.
            user.PasswordHash = Argon2.Hash(userDto.Password);

            db.Users.Add(user);
            await db.SaveChangesAsync();
            return TypedResults.Ok(new UserResponse(user));
        }

        public static async Task<IResult> EditUser(AppDbContext db, int id, [FromBody] UserDTO userDto)
        {
            var user = await db.Users.FindAsync(id);

            if (user == null)
                return TypedResults.NotFound("User not found.");

            // Update fields if they are provided
            if (!string.IsNullOrEmpty(userDto.Username))
                user.Username = userDto.Username;

            if (!string.IsNullOrEmpty(userDto.Email) &&
                !userDto.Email.Equals(user.Email) &&
                HelpMethods.IsEmailValid(userDto.Email) &&
                !await HelpMethods.IsEmailInUse(db, userDto.Email))
            {
                user.Email = userDto.Email;
            }

            await db.SaveChangesAsync();
            return TypedResults.Ok(new UserResponse(user));
        }

        public static async Task<IResult> EditUserPassword(AppDbContext db, int id, [FromBody] UserDTO userDto)
        {
            var user = await db.Users.FindAsync(id);

            if (user == null)
                return TypedResults.NotFound("User not found.");

            if (string.IsNullOrEmpty(userDto.Password) || string.IsNullOrEmpty(userDto.PasswordConfirm))
                return TypedResults.BadRequest("Password and PasswordConfirm are required.");

            if (!HelpMethods.IsPasswordValid(userDto.Password))
                return TypedResults.BadRequest("Password must be at least 8 characters long and include uppercase, lowercase, digit, and special character.");

            if (!userDto.Password.Equals(userDto.PasswordConfirm))
                return TypedResults.BadRequest("Password and PasswordConfirm do not match.");

            user.PasswordHash = Argon2.Hash(userDto.Password);
            await db.SaveChangesAsync();
            return TypedResults.Ok("Password updated successfully.");
        }

        public static async Task<IResult> DeleteUser(AppDbContext db, int id, [FromBody] UserDTO userDto)
        {
            var user = await db.Users.FindAsync(id);

            if (user == null)
                return TypedResults.NotFound("User not found.");

            if (string.IsNullOrEmpty(userDto.Password))
                return TypedResults.BadRequest("Password is required to delete the account.");

            if (!Argon2.Verify(user.PasswordHash, userDto.Password))
                return TypedResults.Unauthorized();

            db.Users.Remove(user);
            await db.SaveChangesAsync();
            return TypedResults.Ok("User deleted successfully.");
        }
    }
}