using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

class Program
{
    public class Config
    {
        public int DelayInSeconds { get; set; } = 0;
        public List<UrlAction> UrlActions { get; set; } = new();
    }

    public class UrlAction
    {
        public string Url { get; set; }
        public int Fullscreen { get; set; }
    }

    static void Main(string[] args)
    {
        string configPath = args.Length > 0
            ? args[0]
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

        if (!File.Exists(configPath))
            return;

        Config config;
        try
        {
            config = ParseIniConfig(configPath);
        }
        catch
        {
            return;
        }

        if (config?.UrlActions == null || config.DelayInSeconds < 0)
            return;

        Thread.Sleep(config.DelayInSeconds * 1000);

        var allScreens = Screen.AllScreens;
        if (config.UrlActions.Count == 0 || allScreens.Length == 0)
            return;

        var options = new ChromeOptions();
        options.AddExcludedArgument("enable-automation");
        options.AddAdditionalOption("useAutomationExtension", false);

        var service = ChromeDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;

        using var driver = new ChromeDriver(service, options);
        var windowHandles = new List<string>();

        var firstAction = config.UrlActions[0];
        driver.Navigate().GoToUrl(firstAction.Url);
        Thread.Sleep(1000);

        var firstBounds = allScreens[0].Bounds;
        driver.Manage().Window.Position = new Point(firstBounds.X, firstBounds.Y);
        driver.Manage().Window.Size = new Size(firstBounds.Width, firstBounds.Height);
        driver.Manage().Window.Maximize();

        windowHandles.Add(driver.CurrentWindowHandle);

        for (int i = 1; i < config.UrlActions.Count && i < allScreens.Length; i++)
        {
            var action = config.UrlActions[i];

            driver.SwitchTo().NewWindow(WindowType.Window);
            Thread.Sleep(500);

            var handles = driver.WindowHandles;
            var newHandle = "";
            foreach (var h in handles)
            {
                if (!windowHandles.Contains(h))
                {
                    newHandle = h;
                    break;
                }
            }
            driver.SwitchTo().Window(newHandle);

            driver.Navigate().GoToUrl(action.Url);
            Thread.Sleep(1000);

            var bounds = allScreens[i].Bounds;
            driver.Manage().Window.Position = new Point(bounds.X, bounds.Y);
            driver.Manage().Window.Size = new Size(bounds.Width, bounds.Height);
            driver.Manage().Window.Maximize();

            windowHandles.Add(newHandle);
        }

        for (int i = 0; i < config.UrlActions.Count && i < windowHandles.Count; i++)
        {
            if (config.UrlActions[i].Fullscreen == 1)
            {
                try
                {
                    driver.SwitchTo().Window(windowHandles[i]);
                    driver.Manage().Window.FullScreen();
                }
                catch
                {
                  
                }
            }
        }

        while (true)
        {
            try
            {
                var handles = driver.WindowHandles;
                if (handles.Count == 0)
                    break;
            }
            catch
            {
                break;
            }

            Thread.Sleep(1000);
        }

        driver.Quit();
    }

    static Config ParseIniConfig(string path)
    {
        var config = new Config();
        var lines = File.ReadAllLines(path);
        UrlAction currentAction = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith(";"))
                continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentAction = line.Equals("[Settings]", StringComparison.OrdinalIgnoreCase) ? null : new UrlAction();
                if (currentAction != null)
                    config.UrlActions.Add(currentAction);
                continue;
            }

            var parts = line.Split('=', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            if (currentAction == null)
            {
                if (key.Equals("DelayInSeconds", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var delay))
                    config.DelayInSeconds = delay;
            }
            else
            {
                if (key.Equals("Url", StringComparison.OrdinalIgnoreCase))
                    currentAction.Url = value;
                else if (key.Equals("Fullscreen", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var fullscreen))
                    currentAction.Fullscreen = fullscreen;
            }
        }

        return config;
    }
}
