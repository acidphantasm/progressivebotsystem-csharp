﻿using System.Collections.Concurrent;
using System.Reflection;
using _progressiveBotSystem.Constants;
using _progressiveBotSystem.Globals;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace _progressiveBotSystem.Utils;

[Injectable(InjectionType.Singleton)]
public class ApbsLogger
{
    private readonly ConcurrentQueue<LogMessage> _queue = new ConcurrentQueue<LogMessage>();
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly Task _task;
    private string _logFilePath;
    private readonly string _pathToModFolder;
    private readonly ISptLogger<ApbsLogger> _logger;

    public ApbsLogger(ISptLogger<ApbsLogger> logger)
    {
        _pathToModFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        _logger = logger;
        _task = Task.Run(() => ProcessLogQueue(_cts.Token));
    }

    public void Debug(string message1, string message2 = "", string message3 = "", string message4 = "",
        string message5 = "", string message6 = "", string message7 = "", string message8 = "")
    {
        if (ModConfig.Config.EnableDebugLog)
        {
            EnqueueLog(new LogMessage
            {
                Timestamp = DateTime.Now, Level = "DEBUG",
                Message = CreateMessage(LoggingFolders.Debug, message1, message2, message3, message4, message5, message6,
                    message7, message8)
            });
        }
    }
        
    public void Warning(string message1, string message2 = "", string message3 = "", string message4 = "", string message5 = "", string message6 = "", string message7 = "", string message8 = "") => EnqueueLog(new LogMessage { Timestamp = DateTime.Now, Level = "WARNING", Message = CreateMessage(Logging.Warning, message1, message2, message3, message4, message5, message6, message7, message8) });
    public void Error(string message1, string message2 = "", string message3 = "", string message4 = "", string message5 = "", string message6 = "", string message7 = "", string message8 = "") => EnqueueLog(new LogMessage { Timestamp = DateTime.Now, Level = "ERROR", Message = CreateMessage(Logging.Error, message1, message2, message3, message4, message5, message6, message7, message8) });
    
    private void EnqueueLog(LogMessage message)
    {
        _queue.Enqueue(message);
    }

    private async Task ProcessLogQueue(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var message))
            {
                try
                {
                    await File.AppendAllTextAsync(_logFilePath,
                        $"{message.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{message.Level}] {message.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[APBS] Error writing log: {ex.Message}");
                }
            }
            else
            {
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    public void Shutdown()
    {
        _cts.Cancel();
        _task.Wait();
    }
    
    private string CreateMessage(string logType, string message1, string message2 = "", string message3 = "", string message4 = "", string message5 = "", string message6 = "", string message7 = "", string message8 = "")
    {
        var messageList = new List<string>()
        {
            message1,
            message2,
            message3,
            message4,
            message5,
            message6,
            message7,
            message8
        };

        var messages = string.Empty;
        var textFlag = string.Empty;
        var showInConsole = false;
        var consoleMessage = string.Empty;
        _logFilePath = $"{_pathToModFolder}/logs/{logType}.txt";

        foreach (var message in messageList)
        {
            if (!string.IsNullOrEmpty(message))
            {
                switch (logType)
                {
                    case Logging.Debug:
                        showInConsole = ModConfig.Config.EnableDebugLog;
                        break;
                    case Logging.Warning:
                        showInConsole = true;
                        break;
                    case Logging.Error:
                        showInConsole = true;
                        break;
                    default:
                        textFlag = "- ";
                        showInConsole = false;
                        break;
                }

                if (showInConsole)
                {
                    consoleMessage += $"{message}";
                }
                messages += $"{textFlag}{message}\n";
            }
        }

        if (!showInConsole) return messages;
        
        switch (logType)
        {
            case Logging.Debug:
                _logger.LogWithColor($"[APBS] {consoleMessage}", LogTextColor.Blue, LogBackgroundColor.White);
                break;
            case Logging.Warning:
                _logger.LogWithColor($"[APBS] {consoleMessage}", LogTextColor.Black, LogBackgroundColor.Yellow);
                break;
            case Logging.Error:
                _logger.LogWithColor($"[APBS] {consoleMessage}", LogTextColor.White, LogBackgroundColor.Red);
                break;
        }

        return messages;
    }
}

public class LogMessage
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; }
    public string Message { get; set; }
}