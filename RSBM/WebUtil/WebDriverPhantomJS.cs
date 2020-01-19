using OpenQA.Selenium.PhantomJS;
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
    class WebDriverPhantomJS
    {
        private static WebDriverWait wait;
        private static Tuple<PhantomJSDriver, WebDriverWait> loadDriver;

        public static Tuple<PhantomJSDriver, WebDriverWait> LoadWebDriver(string classController, PhantomJSDriver web)
        {
            try
            {
                if (web != null)
                    web.Quit();

                var driver = PhantomJSDriverService.CreateDefaultService();
                driver.HideCommandPromptWindow = true;

                web = new PhantomJSDriver(driver, new PhantomJSOptions(), TimeSpan.FromSeconds(180));
                web.Manage().Window.Maximize();
                web.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(180);

                wait = new WebDriverWait(web, TimeSpan.FromSeconds(180));

                loadDriver = new Tuple<PhantomJSDriver, WebDriverWait>(web, wait);
            }
            catch (Exception e)
            {
                switch (classController.ToString())
                {
                    case "BB":
                        RService.Log("Exception (LoadWebDriver) " + BBController.GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + BBController.GetNameRobot() + ".txt");
                        break;
                    case "CNET":
                        RService.Log("Exception (LoadWebDriver) " + CNETController.GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + CNETController.GetNameRobot() + ".txt");
                        break;
                    default:
                        RService.Log("Exception (LoadWebDriver) " + classController.GetType().Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + classController.GetType().Name + ".txt");
                        break;
                }

                if (web != null)
                    web.Quit();
            }

            return loadDriver;
        }
    }
}
