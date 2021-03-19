using Npgsql;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Telegram.Bot;
using System.Threading;
using Telegram.Bot.Args;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Stress_checker
{
    class Program
    {
        static void Main(string[] args)
        {
            var bot = new StressChecker(filename: Path.Combine(Environment.CurrentDirectory, "config.json" ) );
            bot.Start().Wait();
        }
    }
}
