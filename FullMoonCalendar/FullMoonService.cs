namespace FullMoonCalendar;

public interface FullMoonService {

    IAsyncEnumerable<DateTime> getFullMoons(DateTime start, DateTime end);

}