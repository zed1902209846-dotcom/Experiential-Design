namespace SolarSystemAR
{
    public static class SessionState
    {
        public static string UserName { get; private set; } = "Guest";
        public static bool IsGuest { get; private set; } = true;

        public static void SetUser(string name)
        {
            UserName = name;
            IsGuest = false;
        }

        public static void SetGuest()
        {
            UserName = "Guest";
            IsGuest = true;
        }

        public static void Reset()
        {
            SetGuest();
        }
    }
}
