---

<details>
<summary>Українська</summary>

# Proximity Pickup

Підходьте до скарбів, грибів або будь-яких інших об'єктів, які можна підібрати, і вони автоматично підбиратимуться - так, ніби ви натиснули клавішу **E** біля них. Ви самі обираєте, на які об'єкти поширюється дія мода та на якій відстані він спрацьовує.

## МОЖЛИВОСТІ

- Автоматична взаємодія з об'єктами, які можна підібрати, у межах налаштованого радіуса.
- Повністю налаштовуваний список префабів (працює зі скарбами в підземеллях, випадковою здобиччю, рослинами тощо).
- Подієва система: об'єкти реєструються під час появи у світі, тому немає потреби в періодичному скануванні світу чи затримок через опитування.
- Враховує стан інтерфейсу: мод не працює, коли відкрито інвентар, меню, чат або консоль, а також коли персонаж мертвий чи телепортується.
- Працює на стороні клієнта: встановлювати мод на сервер не потрібно.
- Необов'язковий журнал налагодження для визначення точних назв префабів.

## ПРЕФАБИ ЗА ЗАМОВЧУВАННЯМ

```
Pickable_DolmenTreasure
Pickable_ForestCryptRandom
Pickable_Item
Pickable_MorkHallaTreasure
Pickable_MountainCaveRandom
Pickable_SunkenCryptRandom
```

Предмети підбираються так само, як каміння або гілки: спочатку вони випадають у світ, після чого їх можна покласти до інвентаря.

## ВСТАНОВЛЕННЯ

1. Встановіть `BepInEx for Valheim`.
2. Скопіюйте `ProximityPickup.dll` до папки `BepInEx/plugins`.
3. Один раз запустіть гру, щоб створити файл конфігурації.

## НАЛАШТУВАННЯ

Файл: `BepInEx/config/BioHazardN7.ProximityPickup.cfg` (зміни застосовуються одразу, перезапуск гри не потрібен).

| Розділ | Параметр | За замовчуванням | Опис |
|---|---|---|---|
| General | `Enabled` | `true` | Увімкнути або вимкнути мод. |
| General | `Radius` | `5` | Горизонтальний радіус дії навколо гравця, у метрах. |
| General | `MaxDistanceBelow` | `0.5` | Максимальна відстань у метрах нижче рівня гравця. Об'єкти, розташовані глибше (наприклад, закопані скарби), ігноруються. Обмеження для об'єктів вище гравця немає. |
| General | `MaxPerFrame` | `3` | Максимальна кількість об'єктів, з якими можна взаємодіяти за один кадр. |
| General | `RetryDelay` | `3` | Кількість секунд перед повторною взаємодією з тим самим об'єктом. |
| General | `Prefabs` | дивись список вище | Назви префабів, розділені комами (без `(Clone)`, регістр літер не має значення). |
| Debug | `DebugLog` | `false` | Запис у журнал зареєстрованих об'єктів, їхніх компонентів і результатів взаємодії. |

## ЯК ЗНАЙТИ НАЗВИ ПРЕФАБІВ

Увімкніть `DebugLog`, станьте поруч із потрібним об'єктом і перевірте файл `BepInEx/LogOutput.log`. Шукайте рядки, що починаються з `Registered '...'` або скористайтеся внутрішньоігровою командою консолі: `printnetobj <radius>`.

## ПРИНЦИП РОБОТИ

Невеликий Harmony-патч для `ZNetView.Awake` перевіряє кожен новий мережевий об'єкт, що з'являється у світі, на відповідність списку префабів і зберігає лише ті об'єкти, які відповідають заданим критеріям. Кожного кадру мод перевіряє відстань до цих небагатьох об'єктів і викликає стандартну взаємодію (`Pickable.Interact` або будь-який інший доступний для взаємодії компонент об'єкта), якщо об'єкт перебуває в межах заданого радіуса.

## СУМІСНІСТЬ

Мод працює виключно на стороні клієнта. Використовується стандартна ігрова система взаємодії, тому керування власністю об'єктів і синхронізація виконуються самою грою.

# СПИСОК ЗМІН

## 1.0.1

- Оновлено `manifest.json` (оновлено посилання на спільноту).

## 1.0.0

- Перший випуск.

</details>

---

<details>
<summary>English</summary>

# Proximity Pickup

Walk near a treasure, a mushroom or any other "pickable" object and it is picked up automatically, exactly as if you had pressed **E** on it. You choose which objects are affected and how close you need to be.

## FEATURES

- Automatic interaction with pickable objects inside a configurable radius
- Fully configurable list of prefabs (works for dungeon treasures, random loot, plants, etc.)
- Event-driven: objects are registered when they spawn, so there is no periodic world scan and no polling delay
- Respects the UI: does nothing while the inventory, menu, chat or console is open, or while you are dead / teleporting
- Client-side: no need to install it on the server
- Optional debug log to find exact prefab names

## DEFAULT PREFABS

```
Pickable_DolmenTreasure
Pickable_ForestCryptRandom
Pickable_Item
Pickable_MorkHallaTreasure
Pickable_MountainCaveRandom
Pickable_SunkenCryptRandom
```

Items are picked the same way as a stone or a branch: they drop and can then be put into your inventory.

## INSTALLATION

1. Install `BepInEx for Valheim`.
2. Put `ProximityPickup.dll` into `BepInEx/plugins`.
3. Start the game once to generate the config.

## CONFIGURATION

File: `BepInEx/config/BioHazardN7.ProximityPickup.cfg` (changes apply live, no restart needed).

| Section | Option | Default | Description |
|---|---|---|---|
| General | `Enabled` | `true` | Enable or disable the mod. |
| General | `Radius` | `5` | Horizontal radius in meters around the player. |
| General | `MaxDistanceBelow` | `0.5` | Max distance in meters below the player. Deeper objects (e.g. buried treasure) are ignored; there is no limit above. |
| General | `MaxPerFrame` | `3` | Max objects interacted with per frame. |
| General | `RetryDelay` | `3` | Seconds before the same object is interacted with again. |
| General | `Prefabs` | see above | Comma-separated prefab names (without `(Clone)`, case ignored). |
| Debug | `DebugLog` | `false` | Log registered objects, their components and interaction results. |

## FINDING PREFAB NAMES

Enable `DebugLog`, stand near the object and check `BepInEx/LogOutput.log` for `Registered '...'` lines. Or use the in-game console command `printnetobj <radius>`.

## HOW IT WORKS

A small Harmony patch on `ZNetView.Awake` checks each newly spawned network object against your prefab list and stores only the matching ones. Every frame the mod checks the distance to those few objects and calls the normal interaction (`Pickable.Interact`, or any other interactable on the object) for those inside the radius.

## COMPATIBILITY

Client-side only. Uses the game's normal interaction, so ownership and syncing are handled by the game itself.

# CHANGELOG

## 1.0.1

- Updated `manifest.json` (updated the link to the community).

## 1.0.0

- Initial release.

</details>

---

| [Discord](https://discord.gg/8hdmpkCB6w) | [Github](https://github.com/BioHazardN7/ValheimModding) |
|----|----|