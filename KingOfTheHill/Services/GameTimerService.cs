using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

namespace KingOfTheHill.Services
{
    public class GameTimerService
    {

        public int _secondsRemaining;

        public Func<Guid, Task> OnTimerStarted { get; set; } = null!;
        public Func<Guid, int, Task> OnTimerUpdate { get; set; } = null!;
        public Func<Guid, Task> OnTimerCompleted { get; set; } = null!;
        public Func<Guid, Task> OnTimerStopped { get; set; } = null!;

        public Predicate<Game> TimerStopCondition { get; set; } = null!;
        public Predicate<Game> TimerCompletedCondition { get; set; } = null!;

        [Required]
        public Game game { get; set; } = null!;

        private Timer? _timer;

        private readonly ILogger _logger;


        public GameTimerService(ILogger logger)
        {
            _logger = logger;
        }

        public async Task StartTimer(int duration)
        {
            _logger.LogInformation("Timer was started");

                if (_timer == null)
                {
                    await OnTimerStarted.Invoke(game.GameID);
                    _secondsRemaining = duration;
                    _timer = new Timer(TimerCallback, null, 1000, 1000);
                }
        }

        private async void TimerCallback(object? state)
        {
            try
            {
                if (TimerStopCondition.Invoke(game))
                {
                    _logger.LogInformation("Timer was stopped");

                    await OnTimerStopped.Invoke(game.GameID);
                    _timer?.Dispose();
                    _timer = null;
                }

                if (_secondsRemaining > 0 && TimerCompletedCondition.Invoke(game))
                {
                    _secondsRemaining--;

                    _logger.LogInformation($"Time before proccessing the game is {_secondsRemaining}");

                    await OnTimerUpdate.Invoke(game.GameID, _secondsRemaining);
                }

                else
                {
                    _logger.LogInformation("Timer was completed, starting game...");

                    _timer?.Dispose();
                    _timer = null;
                    await OnTimerCompleted.Invoke(game.GameID);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GameTimeProvider : {ex}");
            }
        }
    }
}

