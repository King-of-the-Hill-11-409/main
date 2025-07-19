using Bunit;
using Bunit.TestDoubles;
using KingOfTheHill;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Pages = KingOfTheHill.Components.Pages;

namespace ComponentsTests;

public class RulesTests : BunitTestContext
{
    [Test]
    public void BlockRulesCorrectRender()
    {
        var cut = RenderComponent<Pages.Rules>();

        Assert.DoesNotThrow(() =>
        {
            var block = cut.Find(".rules-container");
        });
    }

    [Test]
    public void NavigationCorrectWork()
    {
        var cut = RenderComponent<Pages.Rules>();

        var button = cut.Find(".back-button");

        button.Click();

        var navMan = Services.GetRequiredService<FakeNavigationManager>();
        Assert.That(navMan.Uri, Is.EqualTo("http://localhost/"));
    }
}
