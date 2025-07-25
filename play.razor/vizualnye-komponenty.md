# Визуальные компоненты

## 1. Лобби

```html
<div class="lobby__container">
    <input @bind="@Username" placeholder="Никнейм"/>
    <div class="lobbies__list">
        @foreach (var lobby in Lobbies) {
            <button @onclick="(() => JoinLobby(lobby.Key))">
                Игроков: @lobby.Value.Players.Count/4
            </button>
        }
    </div>
</div>
```

## 2. Игровое поле

```html
<div class="game__container">
    <!-- Другие игроки -->
    <div class="top__players">
        @foreach (var player in _currentGame.Players) {
            <div class="player__block">
                <div class="player__avatar">👨🏿‍🦽</div>
                <div class="player__cards">
                    @player.Deck.Count карт
                </div>
            </div>
        }
    </div>

    <!-- Карты игрока -->
    <div class="player__hand">
        @foreach (var card in _currentPlayer.Deck) {
            <button class="hand__card">
                <img src="images/@(GetCardImage(card))"/>
            </button>
        }
    </div>
</div>
```

## Стилизация

Основные CSS-классы:

* `.lobby__container` - контейнер лобби
* `.game__container` - игровое поле
* `.player__block` - блок игрока
* `.hand__card` - карта в руке
