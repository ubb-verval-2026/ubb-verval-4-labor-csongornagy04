using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Linq;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class BlazeDemoTests
{
    private IWebDriver driver;
    private const string BaseURL = "https://blazedemo.com";

    [SetUp]
    public void SetupTest()
    {
        driver = new ChromeDriver();
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
    }

    [Test]
    public void FlightSearch_MexicoCityToDublin_ShouldHaveAtLeastThreeFlights()
    {
        // Arrange
        driver.Navigate().GoToUrl(BaseURL);
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        // Find the departure city dropdown
        var departureDropdown = wait.Until(ExpectedConditions.ElementExists(By.Name("fromPort")));
        
        // Find the arrival city dropdown
        var arrivalDropdown = wait.Until(ExpectedConditions.ElementExists(By.Name("toPort")));

        // Act - Select Mexico City as departure
        var departureOptions = departureDropdown.FindElements(By.TagName("option"));
        departureOptions.FirstOrDefault(o => o.Text.Contains("Mexico City"))?.Click();
        
        // Select Dublin as arrival
        var arrivalOptions = arrivalDropdown.FindElements(By.TagName("option"));
        arrivalOptions.FirstOrDefault(o => o.Text.Contains("Dublin"))?.Click();

        // Click the Find Flights button
        var findFlightsButton = wait.Until(ExpectedConditions.ElementExists(By.CssSelector("input[type='submit']")));
        findFlightsButton.Click();

        // Assert - Wait for flight results table and count the flights
        var flightTable = wait.Until(ExpectedConditions.ElementExists(By.TagName("table")));
        var flightRows = driver.FindElements(By.CssSelector("table tbody tr")).ToList();

        // Should have at least 3 flights
        flightRows.Count.Should().BeGreaterThanOrEqualTo(3, 
            because: "there should be at least 3 flights available between Mexico City and Dublin");
    }
}


