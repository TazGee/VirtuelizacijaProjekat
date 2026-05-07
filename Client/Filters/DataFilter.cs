using System;

namespace Client.Filters
{
    public class DataFilter
    {
        public bool IsSelectedDate(DateTime timestampLocal, DateTime selectedDate)
        {
            return timestampLocal.Date == selectedDate.Date;
        }
    }
}