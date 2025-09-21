using TuneMates_Backend.DataBase;

namespace TuneMates_Backend.Controller
{
    public class UserController
    {
        public async Task<IResult> CreateUser(AppDbContext db)
        {
            var user = new User
            {
                Username = "testuser",
                Email = "test@mail.com"
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return TypedResults.Ok();
        }

        public async Task<IResult> EditUser()
        {
            return TypedResults.Ok();
        }

        public async Task<IResult> DeleteUser()
        {
            return TypedResults.Ok();
        }
    }
}