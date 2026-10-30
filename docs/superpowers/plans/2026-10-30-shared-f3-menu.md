# Shared F3 Menu Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Одно расширяемое меню F3 для Minimap и HR с независимой установкой модов, свободным курсором и корректным владением игровым вводом.

**Architecture:** Общий runtime встроен исходниками в каждый мод, V1-панели обнаруживаются по CLR manifest, единственный хозяин выбирается детерминированно. Он использует штатный MonoState/StateProperties и отвечает за F3/окно/lease ввода; каждый мод отвечает за свои preferences и панель. Телефон подключается необязательным reflection-адаптером.

**Tech Stack:** C#11 / net6.0 / MelonLoader0.7.3 / Unity IMGUI / IL2CPP game interop, существующие MelonPreferences. SDK10.0.204 доступен локально.

**Spec:** [2026-10-30-shared-f3-menu-design.md](../specs/2026-10-30-shared-f3-menu-design.md).

## Global Constraints

- Игра Schedule I0.4.7f12 IL2CPP; исходные stable HR1.2.0 (26c0932) и Minimap2.2.8 (c7e1692). Перед реализацией проверить фактические HEAD и рабочие изменения; не затирать чужую работу.
- Каждый мод работает отдельно, без Phone App, другого нашего мода или дополнительной обязательной DLL.
- F3 открывает одно полноценное меню; F4 сохраняет диагностику Minimap. Один кадр/одно нажатие — одна операция переключения.
- Добавление совместимого V1-provider не требует обновления прежних DLL. Только CLR BCL-типы в межсборочном контракте; одинаковые имена собственных типов не дают общей идентичности.
- Существующие категории/ключи MelonPreferences и семантика HR1.2.0 сохраняются. Игровое время сохраняет штатное поведение; меню само не меняет Time.timeScale.
- Новые моды устанавливаются с перезапуском; hot-loading, автоматический hot-failover открытого окна, gamepad-навигация нового меню и кооператив не входят в первый выпуск.
- Только собственное верхнее native-состояние можно снять. Не очищать чужой стек, не вызывать безусловный Pop/LockMouse(true), не перезаписывать чужие CanMove/CanLook.
- До пользовательского теста — парные локальные кандидаты, без публикации stable или изменения установленных DLL при запущенной игре.

## Review Focus

1. Изменение порядка загрузки и старый runtime рядом с новым: ровно один хозяин, старые панели по-прежнему доступны — Task2.
2. Телефон/пауза ниже или чужой экран выше F3: закрывается только своё состояние, курсор и чужое меню не ломаются — Tasks1/3/7.
3. Одновременные F3+клик/Escape, удерживаемые WASD/мышь при закрытии: ни действия в мир, ни двойного Escape, lease держится до release-barrier — Tasks1/3/7.
4. Отсутствующий Phone App и его несовместимые события: оба мода загружаются, настройка/сохранение работают; отключается только адаптер — Tasks4/6/7.
5. Ошибка provider/layout или смена сцены во время ввода: идемпотентное завершение, отсутствие невидимой блокировки/повторных подписок, повреждённый GUI не продолжает layout — Tasks3/7.

## Карта файлов и репозиториев

Все пути ниже относительны к соответствующему checkout в S:/--- Codex/- Schedule 1.

**HR / schedule1-Skoofidons-HR-Department**

- SharedSettings/V1/EntryPointV1.cs — публичный V1 endpoint, manifest и callback-регистрация своего provider.
- SharedSettings/V1/ProviderManifest.cs, ProviderDiscovery.cs, HostElection.cs — чистые CLR-валидация, поиск и выбор хозяина.
- SharedSettings/V1/MenuSession.cs, InputReleaseBarrier.cs — управляемая политика сессии/закрытия без Unity.
- SharedSettings/V1/SharedMenuRuntime.cs, SharedMenuWindow.cs — bootstrap и IMGUI-оболочка.
- SharedSettings/V1/GameUiInputLease.cs — единственная граница native UI-состояния и native exit-маршрута.
- HrSettingsPanel.cs, HrSettingsDraft.cs, HrSettingsService.cs — редактирование/валидация/применение HR.
- OptionalPhoneIntegration.cs — обнаружение телефона и подписки без прямых ссылок; существующий ModManagerIntegration.cs заменить тонким адаптером/удалить после переноса ответственности.
- Core.cs, PreferenceCategoryOrderIntegration.cs, SkoofidonsHRDepartment.csproj, scripts/Build.ps1 — подключение lifecycle, optional phone и IMGUI references.
- scripts/Sync-SharedSettingsSource.ps1 — явное копирование версии исходного компонента в указанный checkout Minimap + проверка хешей; не build-time зависимость от соседнего репозитория.
- tests/SharedSettingsChecks, tests/SharedSettingsFixtures, tests/HrSettingsChecks, tests/SmokeChecks — политика, межсборочная совместимость, применение, фактические DLL.

**Minimap / schedule1-Skoofidons-Minimap**

- MinimapMod/SharedSettings/V1/*.cs — собственная vendor-копия компонента той же версии; собирается обычным MinimapMod/**/*.cs.
- MinimapMod/MinimapSettingsPanel.cs — извлечённая панель и callbacks; не управляет вводом.
- MinimapMod/OptionalPhoneIntegration.cs — необязательные reflection-подписки.
- MinimapMod/MainMod.cs, SniccupMiniMap.csproj — отказ от старого локального F3/наружного окна и прямой reference Phone App; F4/карта/маркерная логика сохранены.
- tests/SharedSettingsChecks, tools/Test-GameApi.ps1 и docs/testing/shared-f3-menu.md — независимая сборка/проверка и игровой сценарий.

Папки создаются при реализации соответствующей задачи, не заранее. Общий namespace и endpoint из spec одинаковы, но каждый runtime скомпилирован внутрь своего DLL. Между repo нет ProjectReference или обращения к соседним файлам при сборке.

## Task1: Доказать штатное владение вводом

**Files:** read-only game metadata/GameAssembly; create scripts/Inspect-SharedMenuInput.ps1 и docs/research/2026-10-30-shared-menu-input.md в HR; proof artifacts вне Git. Небольшой наблюдаемый fixture под tests/SharedSettingsFixtures/InputLeaseProbe, не в release DLL.

**Consumes:** SceneState.Current/InGame; MonoState.InitializeDefaultParent/PushToDefaultParent/PopFromDefaultParent, StateProperties.GetPreset, GameInput.RegisterExitListener и ExitAction.Use.
**Produces:** проверенный алгоритм GameUiInputLease.TryAcquire(string ownerId, out GameUiInputLease lease, out string error): bool; ReleaseWhenSafe(); AbortForSceneUnload(). Указать Main/Menu parent, порядок подготовки компонента, exit-маршрут/приоритет и обработку чужого верхнего состояния.

- [ ] Воспроизвести текущую ошибку Minimap: F3 показывает панель, камера следует за мышью. Зафиксировать требуемое отличие, не менять stable DLL.
- [ ] Проверить metadata/live signatures и native тела для создания/удаления собственного MonoState, применения StateProperties, уведомлений OnBecomeTopSibling/OnRemovedFromStack, Main/Menu и Escape. Не считать один ActiveUIElementCount полной блокировкой.
- [ ] В fixture проверить F3 из обычной игры, поверх телефона/паузы, одновременные клик+открытие, Escape, WASD/клик удерживаются при закрытии. Задать Mouse.Free, Movement/CameraLook.Locked, Equipping/Inventory.Disabled на собственном экземпляре свойств; глобальные пресеты не менять.
- [ ] Проверить, когда TransitionHandler применяет свойства относительно input callbacks. Release-barrier держит lease после скрытия окна до отпускания закрывающего/удерживаемого ввода; управление не восстанавливается в середине кадра. При чужом состоянии сверху — ClosingDeferred до возвращения своего наверх; ни одного чужого Pop.
- [ ] Зафиксировать результат и конкретные bindings/причины вызовов в research-документе; commit исследования. **Blocking gate:** если собственное состояние не изолирует все нужные действия в этих кадрах, до Tasks3–6 исследовать минимальный штатный input-gate и описать его. Не подменять доказательство одним Cursor.visible=true или общим Harmony-подавлением всех Input/UI actions.

При отсутствии возможности запустить fixture агентом выполнить все офлайн-части и подготовить минимальную проверяемую probe-сборку для пользователя; не объявлять gate пройденным по metadata/JIT. Tasks2 и4 можно делать независимо, но пользовательская сборка с меню требует этого подтверждения.

## Task2: V1-контракт, обнаружение и единственный хозяин

**Files:** create SharedSettings/V1/{EntryPointV1,ProviderManifest,ProviderDiscovery,HostElection}.cs, tests/SharedSettingsChecks/{SharedSettingsChecks.csproj,Program.cs}, tests/SharedSettingsFixtures/{OldHost,NewHost,ThirdProvider}; scripts/Sync-SharedSettingsSource.ps1. Только CLR-часть на этой задаче.

**Interfaces:** EntryPointV1.Initialize(string id, string title, Version providerVersion, Action draw, Action opened, Action closed): void; GetManifestV1(): IDictionary<string,object>. ProviderDiscovery.Discover(IEnumerable<Assembly> assemblies): IReadOnlyList<ProviderManifest>. HostElection.Elect(IReadOnlyList<ProviderManifest> providers): ProviderManifest. Формат/версии/ID и tie-break — spec; initial hostVersion/minimumHostVersion1.0.0. Manifest без побочных игровых вызовов.

- [ ] Написать проверки: один участник; оба порядка загрузки; три участника; ничья версий; дубли ID; неизвестное поле; неверный callback/версия/contract; минимальная версия; два типа с одинаковым namespace из разных сборок.
- [ ] Построить fixtures как разные сборки. OldHost1.0 остаётся неизменным, добавить ThirdProvider и NewHost1.1 с поддержкой V1; оба старых endpoint должны выбрать одного нового хозяина. Обработчики CLR Action реально вызываются через найденные manifests.
- [ ] Запустить проверки до реализации и зафиксировать ожидаемый FAIL. Реализовать только контракт/валидацию/детерминированный выбор, без Unity, курсора, файлов Mods, per-frame сканирования или глобальной отдельной DLL.
- [ ] Выполнить dotnet build tests/SharedSettingsChecks -c Release -p:UseAppHost=false -o <fresh-artifact-output> и dotnet <fresh-artifact-output>/SharedSettingsChecks.dll. Ожидается0failures. Sync-script сверяет файловые SHA256 и explicit runtime version, не трогает другие файлы Minimap.
- [ ] Commit V1-протокола и тестов. Опубликовать в design точный V1-контракт до написания adapters.

## Task3: Общая оболочка и InputLease

**Files:** create SharedSettings/V1/{MenuSession,InputReleaseBarrier,SharedMenuRuntime,SharedMenuWindow,GameUiInputLease}.cs; extend tests/SharedSettingsChecks и input fixture; update HR csproj Compile Include для SharedSettings/V1/*.cs и Unity IMGUI/InputLegacy/UIModule/TextRendering references.

**Interfaces:** SharedMenuRuntime.Initialize(); LateInitialize(IEnumerable<Assembly> registeredMelonAssemblies); Tick(); Draw(); RequestClose(); Shutdown(); OnSceneUnload(). MenuSession получает управляемый IInputLease, события собственного/чужого state и снимок ввода; native implementation только GameUiInputLease. Не передавать IInputLease между сборками.

- [ ] Написать failing cases: двойной Tick того же frame; acquire partial failure; F3/Escape общий путь; foreign state выше; deferred close; release-barrier; потеря фокуса; scene unload; исключение draw/closed; повтор Shutdown; failed provider можно отключить и открыть оставшиеся.
- [ ] Реализовать машину состояний Closed/Opening/Open/Closing/Suspended/ClosingDeferred с fake lease до PASS. Callback closed всегда защищён finally; реальные lease держатся до разрешённого снятия. На уничтоженной сцене сбрасывать ссылки, не трогая исчезнувший стек.
- [ ] После Task1-gate подключить подтверждённый native адаптер; исключить прямые глобальные Cursor/CanMove/CanLook и Time.timeScale writes. Не снимать верхнее состояние другого интерфейса даже при аварийном закрытии.
- [ ] Нарисовать список модов слева и прокручиваемую выбранную панель справа, закрытие всегда доступно. При одном участнике сразу его панель. Сохранить GUI state вокруг provider; повреждённый Begin/End завершает текущую сессию, а не продолжает чужой layout. На640×480 и широком экране кнопка закрытия доступна, обе области прокручиваются. opened вызывается только при первом посещении provider за сессию; смена раздела сохраняет HR draft. closed вызывается один раз для каждого посещённого provider при окончательном закрытии, не при смене раздела.
- [ ] Проверить pure-suite + fixture: один F3/одно окно; UI-клики не стреляют/не взаимодействуют; мышь не вращает камеру, WASD не двигает героя; Escape только F3; подлежащие телефон/пауза остаются. Commit оболочки и владения вводом.

## Task4: Независимый HR и optional Phone App

**Files:** create HrSettingsService.cs, OptionalPhoneIntegration.cs, tests/HrSettingsChecks; modify Core.cs, ModManagerIntegration.cs, PreferenceCategoryOrderIntegration.cs, csproj/Build.ps1 и SmokeChecks.

**Interfaces:** HrSettingsService.ApplyFromPreferences(): void — перенос существующего пути Cleaner.RefreshRegistration → Employee.ApplyToPrefabs/Spawned → route heading refresh. OptionalPhoneIntegration.Initialize(IEnumerable<MelonBase> melons); Shutdown(). Reflection-bridge вызывает ApplyFromPreferences; category-order Resolve получает найденный Assembly вместо typeof(ModSettingsEvents).

- [ ] Зафиксировать failing checks: HR без Phone App имеет0metadata references на его assembly, загружается/инициализирует Config; несовместимое событие отключает только optional adapter; повторная инициализация/отписка не дублирует delegates.
- [ ] Перенести применение настроек без изменений порядка/семантики в service. Собственный UI и Phone App используют его. Не вызывать MachineTimerController.ClearState при сохранении: коэффициенты машинных партий сохраняются.
- [ ] Удалить compile Reference Phone App и все типизированные ссылки; ValidateGameReferences не требует установки телефона. Reflection связывает только проверенные события уже зарегистрированного мода; optional sorting сохраняет старые identifiers. Хранить реальные handler delegates для отписки.
- [ ] Проверить сборку без пути к Phone App, metadata отсутствие его reference, отдельный load-test в окружении без DLL; затем с2.2.4. Smoke-тесты различают0optional targets при отсутствии и3selector targets при наличии, основные36native targets не меняются.
- [ ] Выполнить актуальные HR regression suites (baseline176), обновляя только утверждения о старом обязательном телефонном UI. Commit автономности HR.

## Task5: Панель HR с существующими настройками

**Files:** create HrSettingsPanel.cs/HrSettingsDraft.cs, extend HrSettingsService и HrSettingsChecks; modify Core.cs lifecycle и SmokeChecks.

**Interfaces:** HrSettingsPanel.Draw(); OnOpened(); OnClosed(). HrSettingsDraft.TryValidate(out string error): bool. HrSettingsService.TryApply(HrSettingsDraft draft, out string error): bool — сначала validate всех значений, затем Config/Save/ApplyFromPreferences. Endpoint ID skoofidon.hr, title Skoofidon's HR Department, version фактического кандидата.

- [ ] Написать failing tests: все существующие Config entries представлены ровно один раз; порядок Botanist/Chemist/Cleaner/Handler/Fixer/Machines; исходные keys/defaults не меняются;1/7/0.5 означают прежнее; целые лимиты не теряются; NaN/Infinity/незавершённый числовой ввод не применяются частично; окончательное закрытие отбрасывает неприменённый draft, переключение на Minimap и обратно его сохраняет.
- [ ] Создать прокручиваемые разделы, поля чисел с понятными подписями и кнопкой «Применить». Не вводить slider-only потолок для станций/маршрутов и скоростей. Действующие допустимые значения не сбрасывать к defaults.
- [ ] OnOpened читает текущий Config; Apply сохраняет/применяет один раз и показывает результат/ошибку; OnClosed убирает draft. Подписать, что неприменённые изменения не сохраняются. Меню остаётся закрываемым при ошибке записи.
- [ ] Проверить реальные callbacks с Phone App и без него; редактирование не меняет машинные captured rates уже идущих партий, growth читает новые значения по прежнему правилу. Commit панели HR.

## Task6: Перенос Minimap и первая совместная пара

**Files:** explicit sync канонического SharedSettings/V1 в MinimapMod/SharedSettings/V1; create MinimapSettingsPanel.cs/OptionalPhoneIntegration.cs; modify MainMod.cs/SniccupMiniMap.csproj; add tests/SharedSettingsChecks в Minimap и docs/testing/shared-f3-menu.md.

**Interfaces:** MinimapSettingsPanel.Draw(); OnOpened(); OnClosed(). OnClosed вызывает существующий FlushPendingSettings; SaveSettingsFromF3/HandleSettingsUpdate или их переименованные equivalents сохраняют прежнее применение/debounce0.5с. Endpoint ID skoofidon.minimap. MainMod.OnUpdate: shared Tick плюс существующая диагностика/карта; OnGUI: shared Draw, без старого независимого guiVisible/F3.

- [ ] Зафиксировать failing assertions: отдельный F3-toggle удалён, F4 scanner/stats сохранены, все прежние controls доступны, запись не учащается до каждого OnGUI-event, значения prefs сохраняются при standalone и совместном запуске.
- [ ] Перенести только наружную рамку/содержимое панели; игровую мини-карту/маркеры не переписывать. Разделить рамку/внутренний scroll так, чтобы общая оболочка могла размещать панель. Закрытие/сцена/выход flush сохраняют текущую debounce-семантику.
- [ ] Убрать прямой compile Reference Phone App, подписывать его необязательно reflection с теми же проверками и точной отпиской. Настройка через телефон и F3 читает одни prefs.
- [ ] Собрать Minimap отдельно без checkout HR и без Phone App; проверить хеши vendor-копии и mixed-version fixtures. Выполнить существующие TrackingPolicy.Tests и tools/Test-GameApi.ps1 против кандидата. Commit Minimap-интеграции; записать парные commits/версии компонента.
- [ ] Создать локальные кандидаты HR1.3.0-beta.1 и Minimap2.3.0-beta.1 (предлагаемая нумерация; stable теги прежние). Исключить game/reference DLL из пакетов; установка только при закрытой игре с backups вне Mods.

## Task7: Проверка расширяемости и игровая приёмка

**Files:** HR tests/SharedSettingsFixtures/ThirdProvider, docs/testing/shared-f3-menu.md в обоих repo, README/CHANGELOG/requirements; build artifacts/manifest/SHA256 вне Git.

- [ ] Зафиксировать хеши двух кандидатных DLL. Добавить только тестовый третий Melon-provider V1, не пересобирать первые два: F3 должен показать третий раздел и одно окно. Затем добавить fixture с runtime1.1/V1 — прежние DLL выбирают новый host и сохраняют панели. Dummy-provider не входит в release-assets.
- [ ] Матрица: HR alone / Minimap alone / оба; каждый вариант с Phone App2.2.4 и без него. Проверить реальное отсутствие его DLL, а не флаг disable в уже загруженном процессе. После удаления одного мода оставшийся сам открывает F3.
- [ ] Матрица UI: Main Menu, обычная игра, телефон, пауза, свой state ниже чужого, Escape/F3/кнопка закрытия, потеря фокуса, переход сцены, repeated-open/close, ввод чисел, удерживаемые WASD/мышь, same-frame click+toggle. После закрытия камера/движение соответствуют оставшемуся native UI.
- [ ] Fault-cases: malformed manifest, duplicate ID, throwing callback, запись cfg недоступна, сломанный layout; закрытие доступно, лог ограничен, процесс игры и остальные providers продолжают работу. Никакого per-frame assembly scan.
- [ ] Выполнить полный regression HR (актуализированная176-база), Minimap tracking/API и shared suites. Сохранить точные DLL hashes, исходные commits, список фактически проверенных игровых сценариев и ограничения; офлайн-проверки не выдавать за Unity-тест.
- [ ] Подготовить парную инструкцию теста пользователю. После его результата — отдельное решение о стабильных релизах, публикации в GitHub и совместимом диапазоне версий. Одного успеха компиляции недостаточно.

## Первый результат реализации

Сначала получить доказанный InputLease и управляемый V1-контракт. Затем автономный HR и общая оболочка, после них перенос Minimap. Первый полный пользовательский тест — только парой кандидатов с уже исправленной мышью/камерой, а не промежуточным окном без блокировки.

Рекомендуемый способ исполнения — последовательно в текущем чате с проверкой контрактов на границах задач и независимым финальным ревью: Tasks3/5/6 сильно зависят от одних и тех же lifecycle-интерфейсов. Это рекомендация автора плана, а не уже данное разрешение на изменение исполняемого кода.
