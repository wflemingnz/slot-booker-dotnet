﻿using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using SlotBooker.Services.Utils;
using System;
using System.Linq;
using System.Threading;

namespace SlotBooker.Services
{
    public class SlotFinder
    {
        public void FindSlot(DateTime date)
        {
            var dateFormatter = new DateFormatter(date);

            var driver = CreateUndetectableDriver();

            PrepareLoginPage(driver);
            WaitForUserToLogin(driver);
            NavigateToManageFamilyRegistrationPage(driver);

            var secureButton = driver.FindElementByCssSelector("[aria-label='Secure your allocation']");
            ScrollToAndClick(driver, secureButton);

            // select No accessibility needs
            var accessibilityOption = driver.FindElementByCssSelector("[for='form_rooms_0_accessibilityRequirement_1']");
            ScrollToAndClick(driver, accessibilityOption);

            for (int retry = 0; ; retry++)
            {
                if (BookDate(driver, dateFormatter))
                    break;

                Thread.Sleep(2 * 1000);             // delay between retries
                driver.Navigate().Refresh();
            }

            Thread.Sleep(10 * 1000);

            driver.Close();
        }

        private ChromeDriver CreateUndetectableDriver()
        {
            var options = new ChromeOptions();

            // prevent automation detection by recaptcha
            options.AddArgument("--disable-blink-features=AutomationControlled");

            return new ChromeDriver(options);
        }

        private void PrepareLoginPage(ChromeDriver driver)
        {
            driver.Url = "https://allocation.miq.govt.nz/portal/login";

            driver.FindElementByCssSelector("#gtm-acceptAllCookieButton").Click();
            driver.FindElementByCssSelector("#username").SendKeys("wadefleming@yahoo.com");
            driver.FindElementByCssSelector("#password").SendKeys("Dembava12345");
        }

        private void WaitForUserToLogin(ChromeDriver driver)
        {
            // wait for manual solve of recaptcha and login
            var selectBookingText = "Select the booking to manage";
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
            wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.XPath($"//*[contains(text(), '{selectBookingText}')]")));
        }

        private void NavigateToManageFamilyRegistrationPage(ChromeDriver driver)
        {
            var viewButton = driver.FindElementByCssSelector("[aria-label=\"View Passengers\' details\"]");
            ScrollToAndClick(driver, viewButton);
        }

        private bool BookDate(ChromeDriver driver, DateFormatter dateFormatter)
        {
            NavigateToMonth(driver, dateFormatter.MonthString);
            return BookDay(driver, dateFormatter.DateString);
        }

        private void NavigateToMonth(ChromeDriver driver, string month)
        {
            var nextMonthButton = driver.FindElementByClassName("flatpickr-next-month");
            var currentMonth = driver.FindElementByClassName("cur-month");

            ScrollTo(driver, nextMonthButton);      // must be in view to be clickable  

            while (currentMonth.Text != month)
            {
                nextMonthButton.Click();
            }
        }

        private bool BookDay(ChromeDriver driver, string date)
        {
            var dayElement = driver
                .FindElementsByCssSelector($":not(.flatpickr-disabled)[aria-label='{date}']")
                .FirstOrDefault();

            if (dayElement != null)
            {
                dayElement.Click();
                driver.FindElementById("form_next").Click();

                var successText = "Managed isolation allocation is held pending flight confirmation";
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(180));
                wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.XPath($"//*[contains(text(), '{successText}')]")));

                Thread.Sleep(1 * 1000);     //wait for in built auto scroll

                // take screenshot
                var screenshot = driver.GetScreenshot();
                screenshot.SaveAsFile(@"C:\temp\miq\miq-booked.png", ScreenshotImageFormat.Png);

                Thread.Sleep(5 * 1000);
                return true;
            }

            return false;
        }

        private void ScrollTo(ChromeDriver driver, IWebElement element)
        {
            const int scrollWait = 1 * 1000;

            int elementPosition = element.Location.Y;
            string javascript = $"window.scroll(0, { elementPosition })";
            driver.ExecuteScript(javascript);
            Thread.Sleep(scrollWait);
        }

        private void ScrollToAndClick(ChromeDriver driver, IWebElement element)
        {
            ScrollTo(driver, element);
            element.Click();
        }
    }
}
