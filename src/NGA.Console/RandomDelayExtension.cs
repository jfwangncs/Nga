using System;
using System.Threading.Tasks;

namespace NGA.Console
{
    public class RandomDelayExtension
    {
        public static async Task GetRandomDelayAsync()
        {
            // 如果在5:52-5:59之间，暂停30分钟
            var now = DateTime.Now;
            if (now.Hour == 5 && now.Minute >= 52 && now.Minute <= 59)
            {
                await Task.Delay(30 * 60 * 1000); // 30分钟
                return;
            }

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
