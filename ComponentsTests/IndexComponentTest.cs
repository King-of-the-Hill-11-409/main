using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Pages = KingOfTheHill.Components.Pages;

namespace ComponentsTests;

public class IndexTests : BunitTestContext
{
    [Test]
    public void BlockWithButtonsCorrectRender()
    {
        var cut = RenderComponent<Pages.Index>();

        Assert.DoesNotThrow(() =>
        {
            var block = cut.Find(".index-container");
        });
    }

    [TestCase(".nav-button .btn-game", "http://localhost/game")]
    [TestCase(".nav-button .btn-rules", "http://localhost/rules")]
    public void NavigationCorrectWork(string cssSelector, string expectedPath)
    {
        var cut = RenderComponent<Pages.Index>();

        var button = cut.Find(cssSelector);

        button.Click();

        var navMan = Services.GetRequiredService<FakeNavigationManager>();
        Assert.That(navMan.Uri, Is.EqualTo(expectedPath));
    }
}
