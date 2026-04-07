using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CliWrap;
using Microsoft.Extensions.Logging;

namespace Telemart.Client.Data.Diagnostics
{
    public class NetworkDiagnoser : INetworkDiagnoser
    {
        private readonly Timer _timer;
        private readonly ILogger<NetworkDiagnoser> _logger;
        private readonly NetworkDiagnoserOptions _options;
        private readonly ConcurrentQueue<NetworkState> _networkStatesHistory;

        public NetworkDiagnoser(
            ILogger<NetworkDiagnoser> logger,
            NetworkDiagnoserOptions options)
        {
            _logger = logger;
            _options = options;
            _networkStatesHistory = new ConcurrentQueue<NetworkState>();

            if (!_options.Disabled)
            {
                _timer = new Timer(DiagnosePingAsync, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
            }
        }

        public NetworkState DownloadSpeedNetworkState { get; private set; }

        public string ToolTip { get; private set; }

        public event Action StateChanged;

        public void SetDownloadTime(long bytes, double milliseconds)
        {
            double megabitsPerSecond = (bytes / milliseconds) * ((1000 * 8.0) / (1024.0 * 1024));

            NetworkState downloadSpeedNetworkState = megabitsPerSecond switch
            {
                >= 40 => NetworkState.Excellent,
                < 40 and > 15 => NetworkState.Good,
                <= 15 and >= 5 => NetworkState.Normal,
                < 5 and >= 2 => NetworkState.Bad,
                < 2 and > 0 => NetworkState.Terrible,
                0 => NetworkState.No,
                _ => NetworkState.No
            };

            _networkStatesHistory.Enqueue(downloadSpeedNetworkState);

            if (_networkStatesHistory.Count > _options.RequestAnalizePeriod)
            {
                _networkStatesHistory.TryDequeue(out NetworkState _);
            }

            DownloadSpeedNetworkState = (NetworkState)(int)Math.Round(
                _networkStatesHistory.Select(x => (int)x).Average());

            StateChanged?.Invoke();

            Debug.WriteLine("Bytes length: {0}, Elapsed: {1} ms, Download speed: {2} mbps", bytes, milliseconds, megabitsPerSecond);

            if (downloadSpeedNetworkState < NetworkState.Normal)
            {
                _logger.LogWarning("Low download speed: {MegabitsPerSecond} mbps. Elapsed: {Elapsed} ms, Bytes length: {Bytes}", megabitsPerSecond, milliseconds, bytes);
            }
        }

        private async void DiagnosePingAsync(object _)
        {
            try
            {
                List<string> outputLines = new List<string>();

                await Cli.Wrap("ping")
                    .WithArguments($"{_options.PingAddress} -n {_options.PingRetriesCount}")
                    .WithStandardOutputPipe(PipeTarget.ToDelegate(x => outputLines.Add(x)))
                    .ExecuteAsync();

                foreach (string outputLine in outputLines.Skip(outputLines.Count - 4))
                {
                    _logger.LogInformation(outputLine);
                }

                int lostPercent = int.Parse(outputLines[^3]?.Split("(").Last().Split("%").First() ?? "100");

                string pingMilliseconds =
                        new string(outputLines[^1]
                        .Split("=")
                        .Last()
                        .Trim()
                        .Where(char.IsDigit)
                        .ToArray());

                ToolTip = $"Кол-во потерянных пакетов: {lostPercent}%\nping: {pingMilliseconds} мс";

                StateChanged?.Invoke();
            }
            catch
            {
                // ignored
            }
        }
    }
}