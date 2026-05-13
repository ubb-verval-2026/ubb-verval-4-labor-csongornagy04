using FluentAssertions;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Globalization;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class PersonPageTests
{
    private IWebDriver driver;
    private StringBuilder verificationErrors;
    private const string BaseURL = "http://localhost:5091";
    private bool acceptNextAlert = true;

    private Process? _blazorProcess;

    [OneTimeSetUp]
    public void StartBlazorServer()
    {
        var webProjectPath = Path.GetFullPath(Path.Combine(
            Assembly.GetExecutingAssembly().Location,
            "../../../../../../src/DatesAndStuff.Web/DatesAndStuff.Web.csproj"
            ));

        var webProjFolderPath = Path.GetDirectoryName(webProjectPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            //Arguments = $"run --project \"{webProjectPath}\"",
            Arguments = "dotnet run --no-build",
            WorkingDirectory = webProjFolderPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        _blazorProcess = Process.Start(startInfo);

        // Wait for the app to become available
        var client = new HttpClient();
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.Now;

        while (DateTime.Now - start < timeout)
        {
            try
            {
                var result = client.GetAsync(BaseURL).Result;
                if (result.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Thread.Sleep(1000);
            }
        }
    }

    [OneTimeTearDown]
    public void StopBlazorServer()
    {
        if (_blazorProcess != null && !_blazorProcess.HasExited)
        {
            _blazorProcess.Kill(true);
            _blazorProcess.Dispose();
        }
    }

    [SetUp]
    public void SetupTest()
    {
        driver = new ChromeDriver();
        verificationErrors = new StringBuilder();
    }

    [TearDown]
    public void TeardownTest()
    {
        try
        {
            driver.Quit();
            driver.Dispose();
        }
        catch (Exception)
        {
            // Ignore errors if unable to close the browser
        }
        Assert.That(verificationErrors.ToString(), Is.EqualTo(""));
    }

    [TestCase(5)]
    [TestCase(10)]
    [TestCase(0)]
    [TestCase(2.5)]
    public void Person_SalaryIncrease_ShouldIncrease(double percent)
    {
        // Arrange
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        
        var salaryLabelBefore = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='DisplayedSalary']")));
        var initialSalary = double.Parse(salaryLabelBefore.Text, CultureInfo.InvariantCulture);

        ClearAndTypeWithRetry(
            wait,
            By.XPath("//*[@data-test='SalaryIncreasePercentageInput']"),
            percent.ToString(CultureInfo.InvariantCulture));
        // Act
        var submitButton = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']")));
        submitButton.Click();


        // Assert
        var salaryLabel = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='DisplayedSalary']")));
        var salaryAfterSubmission = double.Parse(salaryLabel.Text, CultureInfo.InvariantCulture);

        var expected = initialSalary * (1.0 + percent / 100.0);
        salaryAfterSubmission.Should().BeApproximately(expected, 0.001);
    }
    
    
    [TestCase(-11)]
    [TestCase(-10)]
    [TestCase(-25)]
    public void Person_SalaryIncrease_BelowMinimum_ShouldShowValidationMessages(double percent)
    {
        // Arrange
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

        var salaryLabelBefore = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='DisplayedSalary']")));
        var initialSalary = double.Parse(salaryLabelBefore.Text, CultureInfo.InvariantCulture);

        ClearAndTypeWithRetry(
            wait,
            By.XPath("//*[@data-test='SalaryIncreasePercentageInput']"),
            percent.ToString(CultureInfo.InvariantCulture));

        // Act
        var submitButton = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']")));
        submitButton.Click();

        // Assert
        var salaryLabelAfter = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='DisplayedSalary']")));
        var salaryAfterSubmission = double.Parse(salaryLabelAfter.Text, CultureInfo.InvariantCulture);
        salaryAfterSubmission.Should().BeApproximately(initialSalary, 0.001);

        var errorTextFragment = "between -10 and infinity.";

        var summary = wait.Until(_ =>
        {
            var summaryElement = driver.FindElements(By.CssSelector(".validation-errors, .validation-summary-errors")).FirstOrDefault();
            return summaryElement != null && summaryElement.Text.Contains(errorTextFragment, StringComparison.OrdinalIgnoreCase)
                ? summaryElement
                : null;
        });
        summary.Text.Should().Contain(errorTextFragment);

        var inlineMessage = wait.Until(_ =>
            driver.FindElements(By.CssSelector(".validation-message")).FirstOrDefault(x => x.Text.Contains(errorTextFragment, StringComparison.OrdinalIgnoreCase)));
        inlineMessage.Text.Should().Contain(errorTextFragment);
    }

    private void ClearAndTypeWithRetry(WebDriverWait wait, By locator, string value)
    {
        wait.Until(_ =>
        {
            try
            {
                var element = driver.FindElement(locator);
                element.Clear();
                element.SendKeys(value);
                return true;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });
    }
    private bool IsElementPresent(By by)
    {
        try
        {
            driver.FindElement(by);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    private bool IsAlertPresent()
    {
        try
        {
            driver.SwitchTo().Alert();
            return true;
        }
        catch (NoAlertPresentException)
        {
            return false;
        }
    }

    private string CloseAlertAndGetItsText()
    {
        try
        {
            IAlert alert = driver.SwitchTo().Alert();
            string alertText = alert.Text;
            if (acceptNextAlert)
            {
                alert.Accept();
            }
            else
            {
                alert.Dismiss();
            }
            return alertText;
        }
        finally
        {
            acceptNextAlert = true;
        }
    }
}