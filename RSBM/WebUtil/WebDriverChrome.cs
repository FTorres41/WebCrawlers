﻿﻿﻿using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using RSBM.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.WebUtil
{
    class WebDriverChrome
    {
        private static ChromeDriver webDriver;
        private static WebDriverWait wait;
        private static Tuple<ChromeDriver, WebDriverWait> loadDriver;

        public static Tuple<ChromeDriver, WebDriverWait> LoadWebDriver(string classController)
        {
            try
            {
                if (webDriver != null)
                    webDriver.Quit();
                    

                ChromeOptions op = new ChromeOptions();
                op.AddArgument("disable-extensions");
                op.AddArgument("--start-maximized");
                op.AddArgument("--dns-prefetch-disable");
                op.AddArgument("disable-dev-shm-usage");
                op.AddArgument("--no-sandbox");
                op.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/76.0.3809.132 Safari/537.36");
                op.AddUserProfilePreference("profile.default_content_settings.popups", 0);
                op.AddUserProfilePreference("plugins.always_open_pdf_externally", true);
                op.AddExcludedArguments(new List<string>() { "enable-automation" });

                switch (classController)
                {
                    case "TCEPI":
                        op.AddUserProfilePreference("download.default_directory", TCEPIController.pathEditais);
                        break;
                    case "TCMCE":
                        op.AddUserProfilePreference("download.default_directory", TCMCEController.PathEdital);
                        break;
                    case "BB":
                        op.AddUserProfilePreference("download.default_directory", BBController.PathEditais);
                        break;
                    case "TCERS":
                        op.AddUserProfilePreference("download.default_directory", TCERSController.PathEditais);
                        break;
                    case "CNET":
                        op.AddUserProfilePreference("download.default_directory", CNETController.PathEditais);
                        break;
                    default:
                        break;
                }

                var driver = ChromeDriverService.CreateDefaultService();
                driver.HideCommandPromptWindow = true;

                webDriver = new ChromeDriver(driver, op, TimeSpan.FromSeconds(300));
                webDriver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
                wait = new WebDriverWait(webDriver, TimeSpan.FromSeconds(300));

                loadDriver = new Tuple<ChromeDriver, WebDriverWait>(webDriver, wait);
            }
            catch (Exception e)
            {
                switch (classController)
                {
                    case "BB":
                        RService.Log("Exception (LoadWebDriver) " + BBController.GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + BBController.GetNameRobot() + ".txt");
                        break;
                    case "CNET":
                        RService.Log("Exception (LoadWebDriver) " + CNETController.GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + CNETController.GetNameRobot() + ".txt");
                        break;
                    default:
                        RService.Log("Exception (LoadWebDriver) " + classController + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + classController + ".txt");
                        break;
                }

                if (webDriver != null)
                    webDriver.Quit();
            }

            return loadDriver;
        }
    }
}
