using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FikaAmazonAPI.Utils
{
    public class RateAbstract
    {
        public DateTime LastRun { get; set; } = DateTime.UtcNow;
        public int RequestSent { get; set; } = 0;
    }
}
