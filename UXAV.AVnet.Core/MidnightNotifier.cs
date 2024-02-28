using System;

namespace UXAV.AVnet.Core
{
    
    public static class MidnightNotifier
    {
        static MidnightNotifier()
        {
        }

        [Obsolete("Use a cronjob instead")]
        public static event EventHandler<EventArgs> DayChanged;
    }
}