# Логика игрового компонента

## SignalR подключение

```csharp
// Инициализация подключения
_hubConnection = new HubConnectionBuilder()
    .WithUrl(NavigationManager.ToAbsoluteUri("/gamehub"))
    .AddNewtonsoftJsonProtocol(...)
    .Build();

// Основные обработчики:
_hubConnection.On<Game>("StartGame", game => {
    _currentGame = game;
    StateHasChanged();
});
```

## Ключевые методы

### Управление лобби

```csharp
private async Task CreateLobby() {
    await _hubConnection.SendAsync("CreateGameAsync", _currentPlayerId, Username);
}

private async Task JoinLobby(Guid gameId) {
    await _hubConnection.SendAsync("JoinGameAsync", _currentPlayerId, Username, gameId);
}
```

### Игровые действия

```csharp
private async Task DoMovePlayer() {
    if (_comboHand.Count == 1) {
        await _hubConnection.SendAsync("UseCardAttachedToPlayer", cardIndex, _selectedPlayer);
    }
    else if (_comboHand.Count == 3) {
        await _hubConnection.SendAsync("UseComboAttachedToPlayer", _comboHand, _selectedPlayer);
    }
}
```
