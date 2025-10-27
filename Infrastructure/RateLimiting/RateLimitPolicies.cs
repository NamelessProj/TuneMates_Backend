namespace TuneMates_Backend.Infrastructure.RateLimiting
{
    public static class RateLimitPolicies
    {
        public const string SearchTight = "search-tight";
        public const string Mutations = "mutations";
        public const string Global = "global";
    }
}