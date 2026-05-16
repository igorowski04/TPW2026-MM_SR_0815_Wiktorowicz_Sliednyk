using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DataBall = TP.ConcurrentProgramming.Data.IBall;

namespace TP.ConcurentPrograming.BusinessLogic
{
    public interface IDiagnosticLogger : IDisposable
    {
        void LogCollision(DataBall ball, string hitObject);
    }

    internal class DiagnosticLogger : IDiagnosticLogger
    {
        // |===============|
        // |-=- ZMIENNE -=-|
        // |===============|
        private readonly string _filePath;
        private readonly Task _loggingTask;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        // JEDEN Z WYMOGÓW: Zabezpieczenie przed brakiem przepustowości
        private readonly BlockingCollection<string> _logBuffer = new BlockingCollection<string>(1000);


        public void LogCollision(DataBall ball, string hitObject)
        {
            if (_cts.IsCancellationRequested) return;

            var logEntry = new
            {
                Timestamp = DateTime.Now.ToString("O"),
                BallId = ball.Id,
                CollideWith = hitObject,
                PosX = ball.Position.X,
                PosY = ball.Position.Y
            };

            string json = JsonSerializer.Serialize(logEntry);
            _logBuffer.TryAdd(json);
        }

        private async Task WriteLogsToFileAsync()
        {
            using StreamWriter writer = new StreamWriter(_filePath, append: false, Encoding.ASCII);
            try
            {
                foreach (var log in _logBuffer.GetConsumingEnumerable(_cts.Token))
                {
                    await writer.WriteLineAsync(log);
                }
            }
            catch (OperationCanceledException) { }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _logBuffer.CompleteAdding();
            try
            {
                _loggingTask.Wait();
            }
            catch { }

            _cts.Dispose();
            _logBuffer.Dispose();
        }
    }
}
