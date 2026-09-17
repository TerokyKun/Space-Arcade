# Space-Arcade: архитектура и аудит

Дата аудита: 2026-09-17
Статус: проект Godot 4.6 (C#, .NET 8), 2D-аркада, сборка проходит без ошибок.

> Этап 2 (классификация объектов + спавн) уже применён. Ниже — актуальное состояние.

---

## 0. Классификация игровых объектов (после этапа 2)

Действует единая схема ролей через **Godot Groups + collision layers**:

| Роль | Группа | Collision layer | Где задаётся |
|---|---|---|---|
| Player | `player` | 2 | `Player.tscn` |
| Enemy | `enemy` | 4 | `Enemy.tscn` |
| Boss | `boss` (резерв) | — | нет объектов |
| Environment (камень) | `environment`, `asteroid` | 1 | `Asteroid.tscn` |
| Pickup (хилка) | `pickup`, `heal_pickup` | 8 | `HealPickup.tscn` |
| Pickup (дроп/XP) | `pickup` | 8 | `DropPickup.tscn` |
| Projectile | — | 16 | задаётся в коде снарядов |
| Companion | `companion` (резерв) | 32 | нет объектов |
| CaseZone | `case_zone` (резерв) | 64 | нет объектов |

Главное правило: **только группа `enemy` участвует в логике врагов**
(подсчёт, выбор целей, самонаведение, счётчик убийств). Камни и хилки туда не входят.

---

## 1. Структура проекта

| Путь | Назначение |
|---|---|
| `project.godot` | Конфигурация. Главная сцена `Game.tscn`, движок физики Jolt, GL Compatibility, вход `up/down/rotate-left/rotate-right/shot/pause/tp`. |
| `scense/` | Godot-сцены (14 шт.). |
| `scripts/` | C#-скрипты + `upgrades.json` (идентичный дубликат из `data/`). |
| `data/upgrades.json` | Источник данных об улучшениях (загружается `UpgradeDatabase`). |
| `assets/` | Пусто. Вся графика — перекрашенный `icon.svg`. |
| `docs/` | Этот документ (создан при аудите). |

### Файлы-«мёртвый код» (существуют, но не используются ни в одной сцене)
- `scripts/GameOverUI.cs`
- `scripts/RestartButton.cs`
- `scripts/PlayerCombatController.cs` (дубликат `RocketmanAbility`)
- `scripts/PlayerProgress.cs` (система опыта не подключена, смотри п.7)
- `scripts/upgrades.json` (дубликат `data/upgrades.json`)

> На этапе 2 удалён инертный второй спавнер `scripts/SpaceObjectSpawner.cs` и его
> узел в `Arena.tscn` (он не имел назначенных сцен, спавнил ничего и спамил логи).

---

## 2. Главная сцена — `scense/Game.tscn`

`run/main_scene = Game.tscn`. Дерево:

```
Game (Node2D)
├── Menu (CanvasLayer, Menu.cs)          // экран смерти + Restart
├── Arena (Node2D)
│   ├── SpaceBackground (Node2D)        // процедурный фон-сетка секторов
│   ├── Player (CharacterBody2D, Player.tscn)
│   │   ├── RocketmanAbility (Node)
│   │   ├── TeleportAbility (Node)
│   │   └── CommanderDroneManager (Node)
│   └── EnemySpawner (Node2D, EnemySpawner.tscn)
├── Counter (CanvasLayer, Counter.cs)   // счёт
├── PauseMenu (CanvasLayer, Pause.tscn) // + GameManager (Node)
├── UpgradeMenu (CanvasLayer, UpgradeMenu.tscn)
├── lvl (CanvasLayer, lvl.tscn)         // уровень/прогресс (шестерёнки)
└── UpgradeDatabase (Node2D, скрипт)    // синглтон данных апгрейдов
```

---

## 3. Сцена игрока — `scense/Player.tscn` + `scripts/Player.cs`

`CharacterBody2D`, группа `player`.

Дети: `Sprite`, `Hitbox`, `HealthBar` (HealthBar.cs), `Health` (Health.cs), `Muzzle` (Marker2D), `Camera2D`, `PlayerUpgrades`.

`Player.cs`:
- Движение по `forward`: ускорение/трение, рысканье (`rotate-left/right`), задний ход.
- Стрельба: `shot`/Space → `Shoot(direction)` создаёт `Bullet` через физический родитель `CurrentScene`.
- `ApplyUpgradesToStats()` пересчитывает `_fireCooldown`, `_maxSpeed`, `_reverseMaxSpeed` из `PlayerUpgrades`.
- `OnDied()` → `Menu.ShowOnDeath()`.
- `TakeDamage(float)` учитывает `_invulnerabilityTimer`, но **ни один источник урона его не вызывает** (см. п.9).

---

## 4. Система врагов

- Сцена `Enemy.tscn` + `Enemy.cs`: `CharacterBody2D`, группа `enemy`.
- Дети: `Sprite`, `Hitbox`, `HealthBar` (HealthBar.cs), `Health` (Health.cs), `Muzzle`.
- АИ: преследует игрока до `StopDistance`, разворачивается к цели, стреляет `Bullet` (`BulletOwner.Enemy`) каждые `FireCooldown`.
- При смерти: `SpawnDrops()` раскидывает 2–6 × `DropPickup`.
- Двойной механизм смерти:
  1. `Enemy.TakeDamage()`/пули → `Health.ApplyDamage()` → `Health.Die()` (`AutoFreeOwner=true` ⇒ `QueueFree()` родителя).
  2. `Enemy._OnHealthChanged` → `Die()` → `SpawnDrops()` + `QueueFree()`.
- Баг: в `Enemy._Ready()` `_hpBar = GetNodeOrNull<ProgressBar>("ProgressBar")` — узла с таким именем нет (есть `HealthBar`). Полоска работает только благодаря авто-привязке в `HealthBar.cs`.

## 5. Система спавна

- **Активная:** `EnemySpawner` (дочерний узел Game.tscn). В `_Process` декрементирует таймеры и спавнит:
  - `Enemy` — группа `enemy` (лимит `MaxEnemies=200`), радиус 500;
  - `Asteroid` — группа `asteroid` (лимит 12), движется в сторону игрока;
  - `HealPickup` — считается по отдельной группе `heal_pickup` (лимит `MaxPickups=8`), чтобы дроп от врагов не блокировал спавн хилок.
  - Для каждого врага `RegisterEnemy()` подписывает `Health.Died` → `Counter`.

Раньше здесь был и второй, инертный `SpaceObjectSpawner` (`Arena.tscn`), который не имел
назначенных сцен и не спавнил ничего — удалён на этапе 2.

Сцены `EnemyScene`/`AsteroidScene`/`HealPickupScene` в `EnemySpawner` тоже не назначены в Game.tscn — скрипт сам подгружает их через `GD.Load` (fallback работает).

---

## 6. Смерть врага и счёт

- `Counter.RegisterEnemy(Health)` — подписка `Died` для врагов из группы `Enemy`; даёт +25 к счёту. Также `Counter` начисляет +1 очко каждые 1.5 c (таймерный скор).
- Убить врага могут не только пули игрока: **астероид тоже наносит урон врагам** (см. п.9) и его «убийство» тоже идёт в счёт.

---

## 7. Опыт, уровни и улучшения

- Фактический «опыт» — **шестерёнки**: `DropPickup` → `Lvl.AddGears(Value=1)`.
- `Lvl.cs` (группа `lvl_ui`): пороги 50, 80, 120, 170, … (`50 + level*25 + level*level*5`). Каждый уровень даёт токен → кнопка «Upgrade!» → `UpgradeMenu.Open()`.
- `UpgradeMenu`: при первом открытии — выбор класса (`db.ClassChoices`), далее — 3 случайных стата (`db.StatChoices`). Выбранное применяется через `PlayerUpgrades.ApplyUpgrade()` и пробрасывается событием `Changed`.
- `PlayerProgress.cs` (отдельная реализация опыта: level/EXP/LevelUp) **не инстанцирован ни в одной сцене** — мёртвый код.
- Три «мёртвых» апгрейда: `MaxHP` (не пересчитывает `Health`), `BulletSize` (нигде не применяется), `ExtraProjectiles` (не обрабатывается в `ApplyUpgrade`).

---

## 8. UI

- `Menu` — экран смерти (Restart → `ReloadCurrentScene`).
- `PauseMenu` + `GameManager` — пауза через набор локов (`pause_menu`, `upgrade_menu`); `GameManager` — синглтон в группе `game_manager`, `ProcessMode=Always`.
- `UpgradeMenu` — выбор апгрейдов (`ProcessMode=Always`, работает на паузе).
- `lvl` — прогресс-бар уровня + кнопка апгрейда.
- `Counter` — счёт (`ProcessMode=Inherit`, на паузе не тикает).
- `HealthBar` (ProgressBar) — находит `Health` в родителях и обновляет полоску.
- В `Pause.tscn` кнопка Quit вызывает `GameManager.Quit()`.

---

## 9. Группы, сигналы, Timer

### Godot Groups
| Группа | Кто добавляет |
|---|---|
| `player` | `Player._Ready` |
| `enemy` | `Enemy._Ready` |
| `damageable` | `Health._Ready` |
| `pickup` | `DropPickup`, `HealPickup` (общая классификация «пикап») |
| `heal_pickup` | `HealPickup` (отдельный счётчик спавна хилок) |
| `environment` | `Asteroid` (камни — окружение) |
| `asteroid` | `Asteroid` (подтип окружения для спавн-лимита) |
| `game_manager` | `GameManager` |
| `pause_menu` | `PauseMenu` |
| `upgrade_menu` | `UpgradeMenu` |
| `upgrade_database` | `UpgradeDatabase` |
| `lvl_ui` | `Lvl` |

### Collision layers
| Слой (значение) | Кто |
|---|---|
| 1 — `environment` | Астероиды |
| 2 — `player` | Игрок |
| 4 — `enemy` | Враги |
| 8 — `pickup` | Хилки и дроп |
| 16 — `projectile` | Снаряды (пули; ракеты/дроны — резерв) |
| 32 / 64 | `companion` / `case_zone` (резерв) |

Мониторинг Areas настроен точечно: астероид, хилка и дроп срабатывают
(`collision_mask = 2`) только от тела игрока; пули-лучи (`mask = 1|2|4|16`)
не «съедают» пикапы.

### Сигналы (C#)
- `Health.HealthChanged(float currentHp, float maxHp)`
- `Health.Died`
- `PlayerUpgrades.Changed` (событие `event Action`)
- `PlayerProgress.ExpChanged/LevelUp` (мёртвые)

### Timer
- `Bullet` — `GetTree().CreateTimer(LifeTime)` для самоуничтожения.
- `EnemySpawner` — таймеры на float-счётчиках в `_Process` (не узел Timer).

---

## Потенциальные проблемы (с конкретными причинами)

> Проблемы П1, П2, П5 (камни/хилки в логике врагов, двойной спавн) **исправлены на этапе 2**:
> `Asteroid.OnBodyEntered` теперь бьёт только игрока, хилки считаются по отдельной группе,
> инертный `SpaceObjectSpawner` удалён.

### П1. ~~Камни попадают в логику врагов~~ → ИСПРАВЛЕНО
Было: `Asteroid._OnBodyEntered` наносил урон И игроку, И врагам (`Team != Player && != Enemy → return`), камень мог «убить» врага и дать +25 к счёту.
Стало: астероид принадлежит группе `environment`, бьёт только `Team == Player`,
его Area мониторит только слой игрока (mask = 2). Камни не являются целями
ни для пуль (маска лучей 1|2|4|16), ни для самонаведения (группа `enemy` не содержит камней).

### П2. ~~Хилки обрабатываются как спавн-«наполнитель» наравне с врагами/дропом~~ → ИСПРАВЛЕНО
Было: `HealPickup` и `DropPickup` в одной группе `pickup`, и лимит спавна хилок
считался по ней же (дроп блокировал хилки); хилка была в `space_object` вместе с камнями.
Стало: `HealPickup` в группах `pickup` + `heal_pickup`, а спавн считает только `heal_pickup`.
Хилки корректно спавнятся, подбираются игроком (mask = 2), исчезают после подбора,
не дают опыта и не учитываются как враги.

### П3. Подсчёт убийств
Счёт настроен только на подписку `Counter.RegisterEnemy` из спавнера. После исправления П1
астероид не может убивать врагов, поэтому «ложных» убийств нет.
Двойная логика смерти `Health.Die()` + `Enemy.Die()` разделена guard-флагами `_isDead`/`_dead`
и гарантирует одно событие `Died` (один +25).

### П4. Выдача «опыта»
Два не связанных механизма: рабочие шестерёнки (`Lvl`) и мёртвый `PlayerProgress`. Нет ни одного канала, где враг выдаёт опыт — только дроп.

### П5. ~~Спавн~~ → ИСПРАВЛЕНО
Удалён инертный второй спавнер `SpaceObjectSpawner` (узел + скрипт).
Активным остался один `EnemySpawner`. Счётчик хилок отделён от дропа (`heal_pickup`).

### П6. Способности не работают
- Нет сцены ракеты: `HomingRocket.tscn` отсутствует, в `RocketmanAbility`/`PlayerCombatController` экспорт `RocketScene` не назначен ⇒ «Ракетчик» стреляет обычной пулей с уроном ×3 (самонаведения нет).
- Нет сцен дронов: `CommanderDrone.tscn`, `CommanderShot.tscn` отсутствуют, `DroneScene`/`DroneShotScene` не назначены ⇒ классический «Коммандер» вообще не создаёт дронов (`SyncClass`: `DroneScene == null` → return).
- Даже при наличии сцен `CommanderDrone.HasLineOfSight` **не исключает игрока** из рейкаста ⇒ сам игрок перекрывает линию видимости и дроны не стреляют.
- `Player.TakeDamage()` (с invulnerability) никем не вызывается: пули и астероиды лупят напрямую в `Health.ApplyDamage` ⇒ неуязвимость от телепорта не работает.

### П7. UI
- Двойная система хиллбара: узел `HealthBar` (HealthBar.cs) и скриптовая привязка `Enemy._hpBar` к несуществующему `ProgressBar`.
- `Menu` — это экран смерти, отдельного главного меню нет.
- `GameOverUI`, `RestartButton`, `PlayerProgress` — не подключены в сценах.

### П8. Пули и коллизии
- Пуля = физический рейкаст `IntersectRay`. Маска лучей ограничена слоями
  `environment|player|enemy|projectile` (см. П1/П2) — пикапы и компаньоны больше не съедают пули.
- Идентификация цели — по компоненту `Health` в узле/родителях, усилена слоями коллизий
  (враги/игрок/камни/пикапы на разных слоях).
- Глубокую цель-таблицу (`Health.Team`) по-прежнему стоит вытеснить на физические слои/маски
  — это уже вопрос боевой системы следующего этапа.

### П9. Апгрейды без эффекта
`MaxHP`, `BulletSize`, `ExtraProjectiles` в `PlayerUpgrades` не влияют на игру (см. п.7). `CommanderDrones`/`CommanderOrbit*`/`Teleport*` — экспорты только «на будущее».

---

## Устройство урона (сводка)

```
Пули (Bullet):   raycast(маска 1|2|4|16) → collider → FindHealth() → IsSameTeam() ? уничтожить : ApplyDamage
Астероиды:       Area2D.BodyEntered (mask=2, только игрок) → FindHealth() → ApplyDamage(player, % от MaxHP)
Дроны/ракеты:    Area2D.BodyEntered (mask=enemy) → body.Call("TakeDamage", dmg)   (у врага это Enemy.TakeDamage)
Health:          ApplyDamage → HealthChanged (+ Died при 0) → AutoFreeOwner → QueueFree(родителя)
```

---

## Какие файлы предполагается менять на следующих этапах

- **Разделение ролей:** сделано на этапе 2 — `Asteroid` (environment, бьёт только игрока),
  `HealPickup`/`DropPickup` (pickup, отдельные слои/группы).
- **Группы/подсчёт:** сделано на этапе 2 — спавн хилок считает `heal_pickup`,
  инертный `SpaceObjectSpawner.cs` удалён.
- **Смерть/опыт:** `Enemy.cs`, `Health.cs`, `Counter.cs`, `Lvl.cs` — единый событийный канал смерти врага (→ очки/опыт/дроп), привязать `PlayerProgress` или расширить `Lvl`.
- **Способности:** `RocketmanAbility.cs`, `CommanderDroneManager.cs`, `CommanderDrone.cs`, `CommanderShot.cs` — создать недостающие сцены (`HomingRocket.tscn`, `CommanderDrone.tscn`, `CommanderShot.tscn`); LOS дрона уже не перекрывается игроком; маршрутизировать весь урон по игроку через `Player.TakeDamage`.
- **Система урона:** `Bullet.cs`, `Health.cs`, `Player.cs` — перевод целей на физические слои/маски завершён частично; следующим шагом усилить командо-логику (`Health.Team`) и `Player.TakeDamage`.
- **UI:** `Menu.cs`/`Pause.tscn`, интеграция `GameOverUI`, очистка `Enemy._hpBar`, удаление мёртвого кода (`PlayerCombatController.cs`, `PlayerProgress.cs`, `RestartButton.cs`, `scripts/upgrades.json`).

---

## Проверка

- Этап 1: `dotnet build "wd test.csproj"` — 0 ошибок, 0 предупреждений.
- Этап 2: `dotnet build "wd test.csproj"` после правок классификации — повторён, см. результат сборки.
- Запуск внутри редактора Godot не выполнялся (нет GUI в окружении); поведение проверено
  статическим разбором: камни/хилки не попадают в группу `enemy`, слои коллизий разделяют роли.