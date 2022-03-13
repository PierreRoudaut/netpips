using System.Globalization;
using NUnit.Framework;

namespace Netpips.Tests;

[SetUpFixture]
public class TestsSetupClass
{
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("En-Us");
        CultureInfo.CurrentCulture = new CultureInfo("En-Us");
    }

    [OneTimeTearDown]
    public void GlobalTeardown()
    {
    }
}