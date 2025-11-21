namespace TuneMates_Backend.Utils
{
    public static class Constants
    {
        public static class Cleanup
        {
            public const int DefaultBackgroundServiceIntervalHours = 3;
            public const int MaxHoursForAProposalBeforeCleanup = 5;
            public const int MaxHoursForProposalBeforeRefused = 1;
            public const int MaxHoursForARoomBeforeInactive = 24;
            public const int MaxDaysForARoomBeforeCleanup = 32;
            public const int MaxMinutesForSpotifyStateBeforeInvalidity = 10;
        }

        public static class Forms
        {
            public const int MaxUsernameLength = 30;
            public const int MinPasswordLength = 8;
            public const int MaxPasswordLength = 20;
            public const int MaxEmailLength = 254;
            public const int MaxRoomNameLength = 50;
        }

        public const int MaxRoomPerUser = 10;
        public const int JwtTokenValidityHours = 3;
    }
}