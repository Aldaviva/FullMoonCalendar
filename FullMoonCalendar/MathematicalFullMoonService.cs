#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously - method must be async from interface's return type

using BenMakesGames.MoonMath;

namespace FullMoonCalendar;

public class MathematicalFullMoonService: FullMoonService {

    public async IAsyncEnumerable<DateTime> getFullMoons(DateTime start, DateTime end) {
        DateTime currentDate = findNextFullMoon(start, false);
        while (currentDate <= end) {
            yield return currentDate;
            currentDate = findNextFullMoon(currentDate, true);
        }
    }

    private static DateTime findNextFullMoon(DateTime start, bool excludeStart) {
        if (excludeStart && isFullMoon(start)) {
            // seek to roughly the next new moon so we'll get a different full moon below
            start = start.AddDays(22);
        }

        while (!isFullMoon(start)) {
            start = start.AddDays(1);
        }

        return start;
    }

    private static bool isFullMoon(DateTime date) => date.GetMoonAge() is >= 14.155 and < 16.61096;

}