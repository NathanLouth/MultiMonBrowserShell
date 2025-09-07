using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

class Program
{
    static void Main()
    {
        Screen[] allScreens = Screen.AllScreens;

        List<IWebDriver> drivers = new List<IWebDriver>();

        for (int i = 0; i < 3; i++)
        {
            Rectangle bounds = allScreens[i].Bounds;

            ChromeOptions options = new ChromeOptions();
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);

            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            IWebDriver driver = new ChromeDriver(service, options);

            System.Threading.Thread.Sleep(1000);

            driver.Manage().Window.Position = new Point(bounds.X, bounds.Y);
            driver.Manage().Window.Size = new Size(bounds.Width, bounds.Height);
            driver.Manage().Window.Maximize();

            drivers.Add(driver);
        }

        foreach (var driver in drivers)
        {
            driver.Navigate().GoToUrl("https://example.com");
            driver.Manage().Window.FullScreen();
        }

        while (true)
        {
            bool allClosed = true;

            foreach (var driver in drivers.ToList())
            {
                try
                {
                    if (driver.WindowHandles.Count > 0)
                    {
                        allClosed = false;
                        break;
                    }
                }
                catch (WebDriverException)
                {
                    drivers.Remove(driver);
                }
            }

            if (allClosed)
            {
                break;
            }

            Thread.Sleep(1000);
        }

        foreach (var driver in drivers)
        {
            try { driver.Quit(); } catch { }
        }
    }
}
