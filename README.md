# Главная страница (Index.razor)

Компонент является стартовой точкой приложения. Отображает заголовок и две кнопки для навигации.

Файл: `Pages/index.razor`.

## Код

```razor
@page "/"
@inject NavigationManager Navigation
@rendermode InteractiveServer

<div class="index-container">
    <h1 class="index-title">Царь Горы</h1>
    <div class="buttons-column">
        <button @onclick="@(() => Navigation.NavigateTo("/game"))" 
                class="nav-button btn-game">
            <span class="button-icon">🎮</span> Начать игру
        </button>
        <button @onclick="@(() => Navigation.NavigateTo("/rules"))" 
                class="nav-button btn-rules">
            <span class="button-icon">📖</span> Правила игры
        </button>
    </div>
</div>
```

## Разметка и функциональность

| Элемент               | Описание                                          |
| --------------------- | ------------------------------------------------- |
| `@page "/"`           | Делает компонент корневой страницей приложения.   |
| `NavigationManager`   | Позволяет программно переходить между страницами. |
| Кнопка `Начать игру`  | Переход на `/game` (игровой режим).               |
| Кнопка `Правила игры` | Переход на `/rules` (страница с правилами).       |

## Стили

Компонент использует следующие CSS-классы:

* `.index-title` — стиль заголовка,
* `.nav-button` — базовая стилизация кнопок,
* `.btn-game`, `.btn-rules` — модификаторы для кнопок.

> 💡 **Подсказка**: Полные стили можно посмотреть в файле pages`/index.razor.css`.
