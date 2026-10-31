# Исследование native input для общего меню

2026-10-31. GameAssembly0.4.7f12, SHA512872DFB646C9B866C53DAE12D7A34E1F9DF21A3E16FAC6B7C6CD21BF5FC8E4B3F99932163C5DE7E3798D37641712E82B914D46E1F023D56E77970BE84FB74EC3A. Скрипт scripts/Inspect-SharedMenuInput.ps1 читает Cpp2IL metadata и native тела, не исполняет игру. Дизассемблирование/логи вне Git: artifacts/shared-f3-2026-10-31.

## Подтверждено офлайн

- SceneState — корневой MonoStateMachine, имеет Current/ActiveState/InGame. Управление описано StateProperties: Mouse.Free, Movement/CameraLook.Locked, Equipping/Inventory.Disabled.
- StateProperties — value type в live interop. Изменение локальной копии GetPreset(UIDefault) не меняет глобальный пресет.
- MonoState.PopFromDefaultParent AC1210 проверяет Peek()==this; при чужом верхнем состоянии не снимает его. Обычное закрытие должно ждать своего верхнего состояния.
- MonoState.RemoveFromDefaultParent AC1440 и MonoStateMachine.Remove(IState) AC0600 отдельно проверяют наличие именно переданного состояния: верхнее снимается штатным Pop; невeрхнее удаляется по идентичности и уведомляет Removed. Это допустимо при аварийной очистке собственного state, без удаления чужого верхнего.
- MonoState не имеет собственного OnDestroy/OnDisable, который заменяет явное снятие state. Destroy GameObject при живом родителе недостаточен.
- ExitDelegate — native delegate с implicit conversion из CLR Action<ExitAction>. GameInput.Exit8AB760 обходит listeners с конца списка и прекращает обработку после ExitAction.Use(). Priority int.MaxValue и фактический порядок Escape всё ещё требуют игрового подтверждения.
- SceneState.Update9C3520 наблюдает изменение активного состояния и уведомляет подписчиков. Только metadata/JIT не доказывают порядок относительно mouse/keyboard callbacks в кадре открытия.
- В Phone App2.2.4 оба специфичных события являются CLR System.Action. OnPreferencesSaved — deprecated и вызывается только phone-путём, поэтому не является единым событием phone+menu.

## Диагностический fixture

SkoofidonsSettingsInputProbe0.1.1, временная клавиша F6. Никаких Harmony-patch, изменений настроек/сохранений/работников. Объект state живёт под transform владельца SceneState. Обычное закрытие держит ReleaseBarrier до следующего кадра/отпускания ввода и собственного top. Аварийная очистка удаляет только свой state, затем Escape, затем GameObject; retry сохраняет уже выполненные этапы. Посторонняя выгрузка additive-сцены не забывает живой owner. Ошибка очистки логируется один раз за сессию.

Чистые проверки:5release-barrier и4cleanup-order/retry. Actual-DLL JIT и API проверяются отдельно и не исполняют native код. Исходные четыре cleanup-теста падали до реализации, затем прошли. Независимое ревью выявило два исправленных случая (scene lifetime и ранняя отписка Escape); окончательная проверка исправлений записана в execution-отчёте.

## Остаётся доказать в игре

Свободный курсор, неподвижная камера, движение/прыжок/использование/взаимодействие без сквозных действий; same-frame и удерживаемый ввод; Escape не закрывает нижний телефон/паузу; потеря фокуса, повторное открытие, уход в главное меню. Пользователь сейчас не может выполнить проверку. Gate этапа1 не пройден; общий product F3 и игровые панели не включаются по одному факту успешной сборки.

## Пользовательская проверка probe

Пользователь сообщил «все ок» и приложил лог132строки SHA256782EF7A38B971E157180EB91E076DF64BCEE87DF0CD35751393C6D66397BA1B1. Probe0.1.1 загружен; три open/close цикла, один native ExitAction и два Close. Все12снимков, включая кадр открытия: top=True, mouseVisible=True, lock=None, canLook=False, canMove=False. Ошибок/исключений нет; одно известное предупреждение Il2CppInterop о запасной сигнатуре. Результат принят как прохождение gate для реализации; это не заменяет матрицу проверки полной пары и не доказывает каждый вид игрового действия отдельно.

Отмечена Minimap2.2.8-dev.3 в логе. Исходники текущего checkout основаны на2.2.8: c7e1692 является выпуском следующего за71c1fd8 (dev.3), поэтому функциональность dev.3 уже включена, затирания отдельной новой работы нет.
