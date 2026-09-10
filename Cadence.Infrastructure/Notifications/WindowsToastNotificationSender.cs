using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Extensions.Logging;
using Cadence.Core.Interfaces;
using Cadence.Core.Models;

namespace Cadence.Infrastructure.Notifications
{
    public sealed class WindowsToastNotificationSender : INotificationSender, IDisposable
    {
        private readonly ILogger<WindowsToastNotificationSender> _logger;
        private bool _disposed;
        private bool _registered;
        public WindowsToastNotificationSender(ILogger<WindowsToastNotificationSender> logger)
        {
            _logger = logger;
            Register();
        }
        private void Register()
        {
            try
            {
                AppNotificationManager.Default.Register();
                _registered = true;
                _logger.LogInformation("Windows toast notification registered successfully.");
            }
            catch (Exception ex)
            {
                _registered = false;
                _logger.LogError(ex, "Failed to register Windows toast notification.");
            }
        }
        public Task SendAsync(NotificationType notificationType, string message, CancellationToken cancellationToken = default)
        {
            if (!_registered)
            {
                return Task.CompletedTask;
            }
            try
            {
                var notification = new AppNotificationBuilder()
                .AddArgument("action", "viewCadence")
                .AddArgument("notificationType", notificationType.ToString())
                .AddText(message)
                .BuildNotification();

                AppNotificationManager.Default.Show(notification);
                notification.Priority = notificationType == NotificationType.BlockTransition
                ? AppNotificationPriority.High
                : AppNotificationPriority.Default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send Windows toast notification. (Notification Type: {notificationType}, Message: {message})", notificationType, message);

            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            if (_registered)
            {
                try
                {
                    AppNotificationManager.Default.Unregister();
                    _logger.LogInformation("Windows toast notification unregistered successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to unregister Windows toast notification.");
                }
            }
            _disposed = true;
        }



    }
}