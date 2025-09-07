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
        var drivers = new List<IWebDriver>();

        for (int i = 0; i < config.UrlActions.Count && i < allScreens.Length; i++)
        {
            var action = config.UrlActions[i];
            var bounds = allScreens[i].Bounds;

            var options = new ChromeOptions();
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);

            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            var driver = new ChromeDriver(service, options);
            Thread.Sleep(1000);

            driver.Manage().Window.Position = new Point(bounds.X, bounds.Y);
            driver.Manage().Window.Size = new Size(bounds.Width, bounds.Height);
            driver.Manage().Window.Maximize();

            driver.Navigate().GoToUrl(action.Url);
            drivers.Add(driver);
        }

        for (int i = 0; i < config.UrlActions.Count && i < drivers.Count; i++)
        {
            if (config.UrlActions[i].Fullscreen == 1)
            {
                try
                {
                    drivers[i].Manage().Window.FullScreen();
                }
                catch { }
            }
        }

        while (drivers.Count > 0)
        {
            for (int i = drivers.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (drivers[i].WindowHandles.Count == 0)
                    {
                        drivers[i].Quit();
                        drivers.RemoveAt(i);
                    }
                }
                catch
                {
                    try { drivers[i].Quit(); } catch { }
                    drivers.RemoveAt(i);
                }
            }

            Thread.Sleep(1000);
        }
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
