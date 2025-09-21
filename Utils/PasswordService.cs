using Microsoft.AspNetCore.Identity;

namespace TuneMates_Backend.Utils
{
    public interface IPasswordService
    {
        string Hash(string password);
        bool Verify(string hashedPassword, string password, out bool needsRehash);
    }

    public sealed class PasswordService : IPasswordService
    {
        private readonly PasswordHasher<object> _hasher = new();

        public string Hash(string password)
        {
            return _hasher.HashPassword(user: null!, password);
        }

        public bool Verify(string hashedPassword, string password, out bool needsRehash)
        {
            var result = _hasher.VerifyHashedPassword(user: null!, hashedPassword, password);
            needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
            return result is PasswordVerificationResult.Success
                or PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}