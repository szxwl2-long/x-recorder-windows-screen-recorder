using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using WindosRecorder.Models;
using WindosRecorder.Services;

namespace WindosRecorder;

public partial class App : System.Windows.Application
{
    private readonly AppSettingsService _settingsService = new();
    private bool _mainWindowReady;

    protected override async void OnStartup(StartupEventArgs e)
    {
        DebugLog.Write($"Startup args: {string.Join(" | ", e.Args)}");
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            EnsureBundledDependency("WindosRecorder.ThirdParty.ScreenRecorderLib.x64.dll", "ScreenRecorderLib.dll");
            DebugLog.Write("Bundled dependency ready.");
        }
        catch (Exception ex)
        {
            DebugLog.Write($"Dependency extraction failed: {ex}");
            System.Windows.MessageBox.Show(
                string.Format(LanguageCatalog.Get(LanguageCatalog.Chinese, "ReleaseDependencyFailed"), ex.Message),
                LanguageCatalog.Get(LanguageCatalog.Chinese, "StartupFailed"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
            return;
        }

        base.OnStartup(e);

        var args = e.Args;
        if (TryParseAutomationOptions(args, out var options))
        {
            DebugLog.Write($"Automation mode detected. Window={options.WindowTitleContains}, Output={options.OutputDirectory}, Duration={options.DurationSeconds}, Lang={options.Language}");
            try
            {
                await RecordingAutomationRunner.RunAsync(options);
                DebugLog.Write("Automation mode finished successfully.");
                Shutdown(0);
            }
            catch (Exception ex)
            {
                DebugLog.Write($"Automation mode failed: {ex}");
                System.Windows.MessageBox.Show(
                    ex.Message,
                    LanguageCatalog.Get(options.Language, "StartupFailed"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(-1);
            }

            return;
        }

        var settings = _settingsService.Load();
        DebugLog.Write($"Interactive mode. Preferred language before prompt: {settings.PreferredLanguage}");
        var languageWindow = new LanguageSelectionWindow();
        if (languageWindow.ShowDialog() != true)
        {
            Shutdown(0);
            return;
        }

        settings.PreferredLanguage = languageWindow.SelectedLanguage;
        _settingsService.Save(settings);
        DebugLog.Write($"Language selected: {settings.PreferredLanguage}");

        StartStartupWatchdog(settings.PreferredLanguage);
        DebugLog.Write("Creating MainWindow instance.");
        var window = new MainWindow(settings.PreferredLanguage);
        DebugLog.Write("MainWindow instance created.");
        window.ContentRendered += (_, _) =>
        {
            _mainWindowReady = true;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            DebugLog.Write("Main window rendered successfully.");
        };
        MainWindow = window;
        DebugLog.Write("Showing MainWindow.");
        window.Show();
    }

    private void StartStartupWatchdog(string language)
    {
        _mainWindowReady = false;
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            if (_mainWindowReady)
            {
                return;
            }

            DebugLog.Write("Startup timeout triggered.");
            var message = LanguageCatalog.Get(language, "StartupTimeoutMessage");
            var title = LanguageCatalog.Get(language, "StartupFailed");
            var timeoutThread = new Thread(() =>
            {
                System.Windows.MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
            timeoutThread.SetApartmentState(ApartmentState.STA);
            timeoutThread.Start();
            timeoutThread.Join();
            ShutdownApplication(-1);
        });
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        DebugLog.Write($"Dispatcher unhandled exception: {e.Exception}");
        System.Windows.MessageBox.Show(
            e.Exception.Message,
            LanguageCatalog.Get(LanguageCatalog.Chinese, "StartupFailed"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
        ShutdownApplication(-1);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        DebugLog.Write($"AppDomain unhandled exception: {e.ExceptionObject}");
    }

    private void ShutdownApplication(int exitCode)
    {
        try
        {
            Shutdown(exitCode);
        }
        finally
        {
            Environment.Exit(exitCode);
        }
    }

    private static bool TryParseAutomationOptions(string[] args, out AutomationOptions options)
    {
        options = null!;
        if (args.Length == 0 || !args.Contains("--automation-record-window"))
        {
            return false;
        }

        string? windowTitle = null;
        string? outputDir = null;
        var language = LanguageCatalog.Chinese;
        var duration = 10;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--automation-record-window":
                    if (i + 1 < args.Length)
                    {
                        windowTitle = args[++i];
                    }
                    break;
                case "--output-dir":
                    if (i + 1 < args.Length)
                    {
                        outputDir = args[++i];
                    }
                    break;
                case "--duration":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var parsedDuration))
                    {
                        duration = parsedDuration;
                    }
                    break;
                case "--lang":
                    if (i + 1 < args.Length)
                    {
                        language = LanguageCatalog.Normalize(args[++i]);
                    }
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(windowTitle) || string.IsNullOrWhiteSpace(outputDir))
        {
            return false;
        }

        options = new AutomationOptions
        {
            Language = language,
            WindowTitleContains = windowTitle,
            OutputDirectory = outputDir,
            DurationSeconds = duration
        };
        return true;
    }

    private static void EnsureBundledDependency(string resourceName, string outputFileName)
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, outputFileName);
        using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Missing embedded resource: {resourceName}");
        using var memoryStream = new MemoryStream();
        resourceStream.CopyTo(memoryStream);
        var bytes = memoryStream.ToArray();

        if (File.Exists(outputPath))
        {
            var existing = File.ReadAllBytes(outputPath);
            if (existing.SequenceEqual(bytes))
            {
                return;
            }
        }

        File.WriteAllBytes(outputPath, bytes);
    }
}
