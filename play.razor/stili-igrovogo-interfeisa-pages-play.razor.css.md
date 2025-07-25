# Стили игрового интерфейса (Pages/Play.razor.css)

## Содержание

1. [Лобби](stili-igrovogo-interfeisa-pages-play.razor.css.md#лобби)
2. [Игровое поле](stili-igrovogo-interfeisa-pages-play.razor.css.md#игровое-поле)
3. [Карты игроков](stili-igrovogo-interfeisa-pages-play.razor.css.md#карты-игроков)
4. [Панели управления](stili-igrovogo-interfeisa-pages-play.razor.css.md#панели-управления)
5. [Анимации и эффекты](stili-igrovogo-interfeisa-pages-play.razor.css.md#анимации-и-эффекты)

***

## Лобби

### Контейнер лобби

```css
.lobby__container {
    height: 100%;
    color: white;
    padding: 20px;
}
```

### Список лобби

| Класс              | Описание                                          |
| ------------------ | ------------------------------------------------- |
| `.lobbies__list`   | Flex-контейнер с отступами 20px                   |
| `.list__lobby`     | Темный полупрозрачный фон с закругленными углами  |
| `.list__lobby div` | Элементы с белой рамкой и выравниванием по центру |

**Пример кнопки:**

```css
.list__lobby div button {
    background-color: green;
    width: 20%;
    border-radius: 5px;
}
```

### Форма ввода

```css
.input__block {
    position: absolute;
    bottom: 0;
    background: #d9d9d8;
    border-radius: 0.5em 0.5em 0 0;
}

.input__block input {
    background: #4c4c4c;
    color: white;
    font-size: 1.3em;
}
```

***

## Игровое поле

### Расположение элементов

```css
.top__players {
    position: absolute;
    top: 30px;
    display: flex;
    justify-content: space-around;
}

.player__hand {
    position: absolute;
    top: 60%;
    justify-content: center;
}
```

### Эффекты выбора

```css
.player__selected {
    box-shadow: 0 0 10px 5px rgba(255, 255, 255, 0.7);
}

.inHand {
    transform: translateY(-20px);
}
```

***

## Карты игроков

### Стили карт

| Класс             | Описание                                    |
| ----------------- | ------------------------------------------- |
| `.card__back`     | Наложение карт с отрицательным margin-right |
| `.card__back img` | Фиксированная высота 80px                   |
| `.hand__card img` | Высота 150px для карт в руке                |

**Анимация при наведении:**

```css
.hand__card:hover {
    transform: translateY(-20px);
    transition: all 0.2s ease-in-out;
}
```

***

## Панели управления

### Нижняя панель

```css
.bottom__panel {
    position: absolute;
    bottom: 0;
    background: #d9d9d7;
    padding: 20px;
}
```

### Кнопка хода

```css
.playButton__card button {
    background: #1a9d02;
    color: white;
    font-size: 1.2em;
}

.playButton__card button:disabled {
    background: #d30000;
}
```

### Меню комбо

```css
.combo__menu {
    position: absolute;
    top: 20%;
    left: 25%;
    width: 50%;
    height: 60%;
    background-image: url(...);
    z-index: 15;
}
```

***

## Анимации и эффекты

### Состояния элементов

| Класс              | Эффект                                  |
| ------------------ | --------------------------------------- |
| `:disabled`        | Затемнение и курсор `not-allowed`       |
| `.visible/.hidden` | Управление видимостью таймера           |
| `.SeeScore`        | Фиолетовая подсветка с масштабированием |

### Пример анимации

```css
.combo__seeScore {
    transition: all .2s ease-in-out;
}

.combo__seeScore:hover {
    transform: scale(1.05);
}
```

***



