using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using KingOfTheHill;
using Microsoft.Extensions.DependencyInjection;
using Pages = KingOfTheHill.Components.Pages;

namespace ComponentsTests;

public class GameTests : BunitTestContext
{
    [Test]
    public void SetupGameCorrectRender()
    {
        var cut = RenderComponent<Pages.Game>();

        Assert.DoesNotThrow(() =>
        {
            var block = cut.Find(".setup-screen");
        });
    }

    [Test]
    public void GameBlockCorrectRender()
    {
        var cut = RenderComponent<Pages.Game>();

        var button = cut.Find(".setup-screen button");

        button.Click();

        Assert.DoesNotThrow(() =>
        {
            var block = cut.Find(".game-container");
        });
    }

    [Test]
    public void PlayersCountCheck()
    {
        var cut = RenderComponent<Pages.Game>();

        var button = cut.Find(".setup-screen button");

        button.Click();

        Assert.DoesNotThrow(() => cut.FindAll(".player"));
    }

    [Test]
    public void CurrentPlayerCorrectRender()
    {
        var cut = RenderComponent<Pages.Game>();

        var button = cut.Find(".setup-screen button");

        button.Click();

        Assert.DoesNotThrow(() => cut.FindAll(".player-hand"));
    }

    [Test]
    public void SelectedCardCheck()
    {
        var cut = RenderComponent<Pages.Game>();

        var setupButton = cut.Find(".setup-screen button");
        setupButton.Click();

        var cardButton = cut.Find(".card");
        cardButton.Click();
        Assert.DoesNotThrow(() => cut.Find(".btn, .btn-primary"));
    }

    [TestCase("+", 5)]
    [TestCase("-", 5)]
    public void CorrectedCardDoCheck(string symbol, int value)
    {
        ICard card = symbol switch
        {
            "+" => new PositiveCard(value),
            "-" => new NegativeCard(value),
            _ => throw new NotImplementedException(),
        };

        var cut = RenderComponent<Pages.Game>();

        var setupButton = cut.Find(".setup-screen button");
        setupButton.Click();

        cut.Instance.currentPlayer!.Deck = [card];
        cut.Render();

        var cardButton = cut.Find(".card");
        cardButton.Click();
        var playButton = cut.Find(".btn, .btn-primary");
        playButton.Click();

        Player pastPlayer = cut.Instance.game!.Players[0];

        Assert.That(Math.Abs(pastPlayer.Score), Is.EqualTo(value));
    }
}

