using System;
using System.Threading.Tasks;

namespace NGA.Console
{
    public class RandomDelayExtension
    {
        public static async Task GetRandomDelayAsync()
        {
            string speed = Environment.GetEnvironmentVariable("SPEED") ?? "slowly";
            int workmin = speed == "slowly" ? 5 : 3;
            int workmax = speed == "slowly" ? 30 : 10;
            int restmin = speed == "slowly" ? 30 : 10;
            int restmax = speed == "slowly" ? 120 : 40;
            int delay;
            if (DateTime.Now.Hour > 7 && DateTime.Now.Hour < 24)
                delay = new Random(DateTime.Now.Millisecond).Next(1000 * workmin, 1000 * workmax);
            else
                delay = new Random(DateTime.Now.Millisecond).Next(1000 * restmin, 1000 * restmax);
            await Task.Delay(delay);
        }
    }
}
