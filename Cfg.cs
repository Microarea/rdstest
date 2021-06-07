using EntryPoint;

namespace rdstest
{
    internal static partial class Program
    {
        public class Cfg
        {
            public string ConnectionString { get; set; }
            public string DbType { get; set; }
            public int Threads { get; set; } = 1;
            public string[] Queries { get; set; }
            public int DelayMs { get; set; } = 500;
            public int MinDelayRange { get; set; } = 1;
            public int MaxDelayRange { get; set; } = 5;
            public bool CountRows { get; set; }
            public bool Verbose { get; set; }
        }

        public class Args : BaseCliArguments
        {
            public Args() : base("rdstest") { }

            [OptionParameter(ShortName: 'p', LongName: "processes")]
            public int Processes { get; set; } = 1;

            [OptionParameter(ShortName: 'c', LongName: "config")]
            public string ConfigPath  { get; set; } = "rdstest.json";

            [OptionParameter(ShortName: 'd', LongName: "startdelay")]
            public int ProcessStartDelay {get; set;} = 1000;
        }
    }
}
