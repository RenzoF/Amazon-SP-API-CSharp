using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FikaAmazonAPI.Utils
{
    internal class RateLimits
    {
        internal decimal Rate { get; set; }
        internal int Burst { get; set; }
        internal DateTime LastRequest { get; set; }
        internal int RequestsSent { get; set; }

        internal RateLimits(decimal Rate, int Burst)
        {
            this.Rate = Rate;
            this.Burst = Burst;
            this.LastRequest = DateTime.UtcNow;
            this.RequestsSent = 0;
        }
        private int GetRatePeriodMs() { return (int)(((1 / Rate) * 1000) / 1); }
        public RateLimits NextRate(RateLimitType rateLimitType)
        {
            if (RequestsSent < 0)
                RequestsSent = 0;
            var rabs = new RateAbstract();
            //try to load a request sent from file
            if (!File.Exists($"{rateLimitType}.json"))
            {
                File.WriteAllText($"{rateLimitType}.json", JsonConvert.SerializeObject(rabs));
            }
            else
            {
                rabs = JsonConvert.DeserializeObject<RateAbstract>(File.ReadAllText($"{rateLimitType}.json"));
                RequestsSent = rabs.RequestSent;
                LastRequest = rabs.LastRun;
            }
            

            int ratePeriodMs = GetRatePeriodMs();

#if DEBUG
            var nextRequestsSent = RequestsSent + 1;
            var nextRequestsSentTxt = (nextRequestsSent > Burst) ? "FULL" : nextRequestsSent.ToString();
            string output = $"[RateLimits ,{rateLimitType,15}]: {DateTime.UtcNow.ToString(),10}\t Request/Burst: {nextRequestsSentTxt}/{Burst}\t Rate: {Rate}/{ratePeriodMs}ms";
            Console.WriteLine(output);
#endif

            if (RequestsSent >= Burst)
            {
                var LastRequestTime = LastRequest;
                while (true)
                {
                    LastRequestTime = LastRequestTime.AddMilliseconds(ratePeriodMs);                    
                    if (LastRequestTime > DateTime.UtcNow)
                        break;
                    else
                        RequestsSent -= 1;

                    if (RequestsSent <= 0)
                    {
                        RequestsSent = 0;
                        break;
                    }
                }
            }


            if (RequestsSent >= Burst)
            {
                LastRequest = LastRequest.AddMilliseconds(ratePeriodMs);
                while (LastRequest >= DateTime.UtcNow) //.AddMilliseconds(-100)
                    Task.Delay(100).Wait();

            }



            if (RequestsSent + 1 <= Burst)
            {
                RequestsSent += 1;
                rabs.RequestSent = RequestsSent;
            }
            LastRequest = DateTime.UtcNow;
            rabs.LastRun = LastRequest;
            File.WriteAllText($"{rateLimitType}.json", JsonConvert.SerializeObject(rabs));

            return this;
        }

        internal void SetRateLimit(decimal rate)
        {
            Rate = rate;
        }

        internal void Delay()
        {
            Task.Delay(GetRatePeriodMs()).Wait();
        }
    }
}
