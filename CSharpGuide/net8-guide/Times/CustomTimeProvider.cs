namespace net8_guide.Times
{
    internal class ZoneTimeProvider : TimeProvider
    {
        private TimeZoneInfo _timeZone;

        public ZoneTimeProvider(TimeZoneInfo timeZone) : base()
        {
            _timeZone = timeZone ?? TimeZoneInfo.Local;
        }

        public override TimeZoneInfo LocalTimeZone => _timeZone;

        public static TimeProvider FromLocalTimeZone(TimeZoneInfo timeZone) => new ZoneTimeProvider(timeZone);
    }
}
