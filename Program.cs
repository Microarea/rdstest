using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using EntryPoint;
using System.Diagnostics;
using System.Threading;
using System.Data.SqlClient;
using static Prelude.Functional;
using static System.Linq.Enumerable;
using System.Data;

namespace rdstest
{
    internal static partial class Program
    {
        private static void Main(string[] args)
        {
            var arguments = Cli.Parse<Args>(args);
            var cfg = new ConfigurationBuilder()
               .AddJsonFile(arguments.ConfigPath, true, true)
               .AddEnvironmentVariables()
               .Build()
               .Get<Cfg>();

            if (arguments.Processes > 1)
            {
                Parallel.For(0, arguments.Processes, (i, _) =>
                {
                    var psi = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName, "-c " + arguments.ConfigPath)
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };
                    using var p = Process.Start(psi);
                    p.OutputDataReceived += (s, e) => Console.WriteLine($"{i} - {p.Id} - {e.Data}");
                    p.BeginOutputReadLine();
                    p.WaitForExit();
                });
                return;
            }

            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(cfg));

            var tasks = Range(0, cfg.Threads).Select(i =>
                Task.Run(() =>
                {
                    var rnd = new Random();
                    for (var y = 0; ; y++)
                    {
                        var query = cfg.Queries[y % cfg.Queries.Length];
                        try
                        {
                            var count = cfg.DbType switch
                            {
                                "postgres" => DoPostgres(i, y, query, cfg),
                                _ => DoSqlServer(i, y, query, cfg),
                            };
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[{DateTime.UtcNow:HHmmssfff},{i},{y},{query}] ERROR : " + ex.ToString());
                        }
                        Thread.Sleep(cfg.DelayMs * rnd.Next(cfg.MinDelayRange, cfg.MaxDelayRange));
                    }
                })
            ).ToList();

            Console.WriteLine("running sync...");

            Console.Read();
        }

        private static int DoPostgres(int id, int idx, string query, Cfg cfg)
        {
            var sw = new Stopwatch().apply(x => x.Start());
            using var conn = new NpgsqlConnection(cfg.ConnectionString).apply(x => x.Open());
            var elapsedOpen = sw.ElapsedMilliseconds;
            using var cmd = new NpgsqlCommand(query, conn);
            sw.Restart();
            using var reader = cmd.ExecuteReader();
            var elapsedQuery = sw.ElapsedMilliseconds;
            int count = 0;
            if (cfg.CountRows) while (reader.Read()) count++;
            if (!cfg.Verbose) return count;
            Console.WriteLine($"[{DateTime.UtcNow:HHmmssfff},{id},{idx}] open: {elapsedOpen} ms, {elapsedQuery} ms, count {count}");
            return count;
        }

        private static int DoSqlServer(int id, int idx, string query, Cfg cfg)
        {
            var sw = new Stopwatch().apply(x => x.Start());
            using var conn = new SqlConnection(cfg.ConnectionString).apply(x => x.Open());
            var elapsedOpen = sw.ElapsedMilliseconds;
            using var cmd = new SqlCommand(query, conn);
            sw.Restart();
            using var reader = cmd.ExecuteReader();
            var elapsedQuery = sw.ElapsedMilliseconds;
            int count = 0;
            if (cfg.CountRows) while (reader.Read()) count++;
            if (!cfg.Verbose) return count;
            Console.WriteLine($"[{DateTime.UtcNow:HHmmssfff},{id},{idx}] open: {elapsedOpen} ms, {elapsedQuery} ms, count {count}");
            return count;
        }

        private static async Task DoPostgresAsync(Cfg cfg)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(cfg));

            var tasks = Range(0, cfg.Threads).Select(async i =>
            {
                var rnd = new Random();
                for (var y = 0; ; y++)
                {
                    var query = cfg.Queries[y % cfg.Queries.Length];
                    try
                    {
                        using var conn = await new NpgsqlConnection(cfg.ConnectionString).applyAsync(x => x.OpenAsync());
                        using var cmd = new NpgsqlCommand(query, conn);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{i},{y},{query}] ERROR : " + ex.ToString());
                    }
                    await Task.Delay(cfg.DelayMs * rnd.Next(cfg.MinDelayRange, cfg.MaxDelayRange));
                }
            });

            Console.WriteLine("running...");

            await Task.WhenAll(tasks);
        }
    }
}
