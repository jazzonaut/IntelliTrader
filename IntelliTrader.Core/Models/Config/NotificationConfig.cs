using System;
using System.Collections.Generic;
using System.Text;

namespace IntelliTrader.Core
{
    internal class NotificationConfig : INotificationConfig
    {
        public bool Enabled { get; set; }
        public bool TelegramEnabled { get; set; }
        public string TelegramBotToken { get; set; }
        public long TelegramChatId { get; set; }
        public bool TelegramAlertsEnabled { get; set; }
    }
}
