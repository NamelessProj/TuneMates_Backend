using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Infrastructure.Auth;
using TuneMates_Backend.Utils;

namespace TuneMates_Backend.Controller
{
    public static class UserController
    {
        /// <summary>
        /// Get all users (for testing purposes only, not for production)
        /// </summary>
        /// <param name="db">Database context</param>
        /// <returns>A list of all users in the form of <see cref="UserResponse"/></returns>
        public static async Task<IResult> GetAllUser(AppDbContext db)
        {
            var users = await db.Users.Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                CreatedAt = u.CreatedAt,
                SpotifyId = u.SpotifyId
            }).ToListAsync();

            return TypedResults.Ok(users);
        }

        /// <summary>
        /// Get user by <paramref name="id"/>
        /// </summary>
        /// <param name="db">Database context</param>
        /// <param name="id">User ID</param>
        /// <returns>The user in the form of <see cref="UserResponse"/></returns>
        public static async Task<IResult> GetUserById(AppDbContext db, int id)
        {
            var user = await db.Users.FindAsync(id);

            if (user == null)
                return TypedResults.NotFound("User not found.");

            return TypedResults.Ok(new { user = new UserResponse(user) });
        }

        /// <summary>
        /// Get the currently authenticated user
        /// </summary>
        /// <param name="http">The current HTTP context</param>
        /// <param name="db">The database context</param>
        /// <returns>The current user in the form of <see cref="UserResponse"/></returns>
        public static async Task<IResult> GetCurrentUser(HttpContext http, AppDbContext db)
        {
            var id = HelpMethods.GetUserIdFromJwtClaims(http);
            if (id == null)
                return TypedResults.Unauthorized();

            var user = await db.Users.FindAsync(id);
            if (user == null)
                return TypedResults.NotFound("User not found.");

            return TypedResults.Ok( new { user = new UserResponse(user) });
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="jwt">JWT token service</param>
        /// <param name="db">Database context</param>
        /// <param name="userDto">User data transfer object containing registration details</param>
        /// <returns>The registered user in the form of <see cref="UserResponse"/> and a JWT token</returns>
        public static async Task<IResult> Register(JwtTokenService jwt, AppDbContext db, [FromBody] UserDTO userDto)
        {
            if (string.IsNullOrWhiteSpace(userDto.Username) ||
                string.IsNullOrWhiteSpace(userDto.Email) ||
                string.IsNullOrWhiteSpace(userDto.Password) ||
                string.IsNullOrWhiteSpace(userDto.PasswordConfirm))
                return TypedResults.BadRequest("Username, Email, Password, and PasswordConfirm are required.");

            if (userDto.Username.Trim().Length > Constants.Forms.MaxUsernameLength)
                return TypedResults.BadRequest($"Username cannot exceed {Constants.Forms.MaxUsernameLength} characters.");

            if (!HelpMethods.IsEmailValid(userDto.Email.Trim()))
                return TypedResults.BadRequest("Invalid email format.");

            if (!HelpMethods.IsPasswordValid(userDto.Password))
                return TypedResults.BadRequest("Password must be at least 8 characters long and include uppercase, lowercase, digit, and special character.");

            if (!userDto.Password.Equals(userDto.PasswordConfirm))
                return TypedResults.BadRequest("Password and PasswordConfirm do not match.");

            if (await HelpMethods.IsEmailInUseAsync(db, userDto.Email))
                return TypedResults.Conflict("Email is already in use.");

            User user = new()
            {
                Username = userDto.Username.Trim(),
                Email = userDto.Email.Trim().ToLowerInvariant(),
                PasswordHash = Argon2.Hash(userDto.Password)
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            string token = jwt.GenerateToken(user.Id);

            return TypedResults.Ok(new {
                user = new UserResponse(user),
                token = token
            });
        }

        /// <summary>
        /// Authenticate a user and provide a JWT token
        /// </summary>
        /// <param name="jwt">The JWT token service</param>
        /// <param name="db">The database context</param>
        /// <param name="userDto">User data transfer object containing login details</param>
        /// <returns>The authenticated user in the form of <see cref="UserResponse"/> and a JWT token</returns>
        public static async Task<IResult> Login(JwtTokenService jwt, AppDbContext db, [FromBody] UserDTO userDto)
        {
            if (string.IsNullOrEmpty(userDto.Email) || string.IsNullOrEmpty(userDto.Password))
                return TypedResults.BadRequest("Email and Password are required.");

            string email = userDto.Email.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !Argon2.Verify(user.PasswordHash, userDto.Password))
                return TypedResults.Unauthorized();

            string token = jwt.GenerateToken(user.Id);

            return TypedResults.Ok(new {
                user = new UserResponse(user),
                token = token
            });
        }

        /// <summary>
        /// Edit user details
        /// </summary>
        /// <param name="http">The current HTTP context</param>
        /// <param name="db">The database context</param>
        /// <param name="userDto">The user data transfer object containing updated user details</param>
        /// <returns>The updated user in the form of <see cref="UserResponse"/></returns>
        public static async Task<IResult> EditUser(IConfiguration cfg, HttpContext http, AppDbContext db, [FromBody] UserDTO userDto)
        {
            var id = HelpMethods.GetUserIdFromJwtClaims(http);
            if (id == null)
                return TypedResults.Unauthorized();

            var user = await db.Users.FindAsync(id);

            if (user == null)
                return TypedResults.NotFound("User not found.");

            // Update fields if they are provided
            if (!string.IsNullOrWhiteSpace(userDto.Username) && userDto.Username.Trim().Length < Constants.Forms.MaxUsernameLength)
                user.Username = userDto.Username.Trim();

            if (!string.IsNullOrWhiteSpace(userDto.Email) && // Check if email is provided
                !userDto.Email.Equals(user.Email) && // Check if email is different from current
                HelpMethods.IsEmailValid(userDto.Email) && // Validate email format
                !await HelpMethods.IsEmailInUseAsync(db, userDto.Email)) // Check if email is not already in use
            {
                user.Email = userDto.Email.Trim();
            }

            EncryptionService encryptionService = new(cfg);

            if (!string.IsNullOrWhiteSpace(userDto.Token))
                user.Token = encryptionService.Encrypt(userDto.Token.Trim());

            if (!string.IsNullOrWhiteSpace(userDto.RefreshToken))
                user.RefreshToken = encryptionService.Encrypt(userDto.RefreshToken.Trim());

            if (userDto.TokenExpiresIn > 0)
                user.TokenExpiresAt = DateTime.UtcNow.AddSeconds(userDto.TokenExpiresIn);

            await db.SaveChangesAsync();
            return TypedResults.Ok(new { user = new UserResponse(user) });
        }

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="http">The current HTTP context</param>
        /// <param name="db">The database context</param>
        /// <param name="userDto">The user data transfer object containing the new password details</param>
        /// <returns>A success message if the password was updated successfully</returns>
        public static async Task<IResult> EditUserPassword(HttpContext http, AppDbContext db, [FromBody] UserDTO userDto)
        {
            var id = HelpMethods.GetUserIdFromJwtClaims(http);
            if (id == null)
                return TypedResults.Unauthorized();

            var user = await db.Users.FindAsync(id);

            if (user == null)
                return TypedResults.NotFound("User not found.");

            if (string.IsNullOrWhiteSpace(userDto.Password) || string.IsNullOrWhiteSpace(userDto.PasswordConfirm) || string.IsNullOrWhiteSpace(userDto.NewPassword))
                return TypedResults.BadRequest("NewPassword, Password and PasswordConfirm are required.");

            if (!Argon2.Verify(user.PasswordHash, userDto.Password))
                return TypedResults.Unauthorized();

            if (!HelpMethods.IsPasswordValid(userDto.NewPassword))
                return TypedResults.BadRequest("Password must be at least 8 characters long and include uppercase, lowercase, digit, and special character.");

            if (!userDto.Password.Equals(userDto.PasswordConfirm))
                return TypedResults.BadRequest("Password and PasswordConfirm do not match.");

            user.PasswordHash = Argon2.Hash(userDto.NewPassword);
            await db.SaveChangesAsync();

            return TypedResults.Ok("Password updated successfully.");
        }

        /// <summary>
        /// Delete user account
        /// </summary>
        /// <param name="http">The current HTTP context</param>
        /// <param name="db">The database context</param>
        /// <param name="userDto">The user data transfer object containing the password for verification</param>
        /// <returns>A success message if the account was deleted successfully</returns>
        public static async Task<IResult> DeleteUser(HttpContext http, AppDbContext db, [FromBody] UserDTO userDto)
        {
            var id = HelpMethods.GetUserIdFromJwtClaims(http);
            if (id == null)
                return TypedResults.Unauthorized();

            var user = await db.Users.FindAsync(id);

            if (user == null)
                return TypedResults.NotFound("User not found.");

            if (string.IsNullOrWhiteSpace(userDto.Password))
                return TypedResults.BadRequest("Password is required to delete the account.");

            if (!Argon2.Verify(user.PasswordHash, userDto.Password))
                return TypedResults.Unauthorized();

            db.Users.Remove(user);
            await db.SaveChangesAsync();

            return TypedResults.Ok("User deleted successfully.");
        }
    }
}