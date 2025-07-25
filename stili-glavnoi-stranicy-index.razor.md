# Стили главной страницы (Index.razor)

CSS-стили для контейнера, заголовка и кнопок навигации.\
Файл: `Pages/index.razor.css`.

## Основные стили

```css
.index-container {
    display: flex;
    height: 100%;
    max-width: 600px;
    margin: 2rem auto;
    padding: 2rem;
    border-radius: 12px;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
    background: white;
    text-align: center;
    align-items: center;
    flex-direction: column;
}
```

| Свойство                 | Описание                                           |
| ------------------------ | -------------------------------------------------- |
| `max-width: 600px`       | Ограничение ширины контейнера для удобства чтения. |
| `box-shadow`             | Тень для эффекта "карточки".                       |
| `flex-direction: column` | Вертикальное расположение дочерних элементов.      |

## Заголовок

```css
.index-title {
    color: #333;
    margin-bottom: 30px;
}
```

## Кнопки навигации

### Общие стили

```css
.buttons-column {
    display: flex;
    flex-direction: column;
    align-items: stretch;
    gap: 15px;
}

.nav-button {
    width: 100%;
    padding: 12px 0;
    border: none;
    border-radius: 5px;
    font-size: 16px;
    cursor: pointer;
    transition: background 0.2s;
}
```

### Специфичные стили кнопок

```css
.btn-game {
    background: #4285f4; /* Синий */
    color: white;
}

.btn-rules {
    background: #34a853; /* Зеленый */
    color: white;
}

.nav-button:hover {
    opacity: 0.9;
}
```

## Адаптивность

```css
@media (max-width: 400px) {
    .index-container {
        width: 90%;
        padding: 20px;
    }
}
```

> **Примечание**:
>
> * `gap: 15px` — отступ между кнопками гарантирован даже в старых браузерах (благодаря `flex-direction: column`).
> * `width: 100%` для кнопок делает их одинаковой ширины.
> * Цвета кнопок соответствуют семантике (синий = действие, зеленый = информация).

## Визуальный пример

\
&#xNAN;_(Замените на реальный скриншот)_
