# HR 1.0.5: исключение parent lookup

Источник: лог пользователя с запуском 1.0.4 в 16:22 и снимок Assigned (1/21), Routes (3/21; free 18). SHA256 загруженной DLL совпадает с установленной 1.0.4: 22D78E4104040245A6AE2171C655E258AB4044C94B5DDDA21BFDD92FECCDD6F3.

Уточнение пользователя: единственная назначенная станция видна при прокрутке вверх; дефекта отображения по этому снимку нет. Не менять visibility/layout на основании пустой видимой области. Сценарии 11–15 назначений и их удаления ещё не подтверждены.

Фактическая ошибка: System.Security.VerificationException в UnityEngine.Component.GetComponentInParent<T>(bool), сначала из PackagerUIFix.InitializeRouteOverride, затем из HandlerStationController.RefreshLayout/AfterBind. Наличие этого метода в API не гарантирует корректность его сгенерированного IL.

Исследование Mono.Cecil: у generic T в обёртке нет ограничения Il2CppObjectBase, но её тело вызывает Cast<T>, требующий этого ограничения. Перегрузка GetComponentInParent(Il2CppSystem.Type,bool) вызывает native API непосредственно и не содержит проблемного generic Cast.

Исправление: общий FindHandlerPanel(Component) вызывает native overload с Il2CppType.Of<PackagerConfigPanel>(), includeInactive=true и TryCast<PackagerConfigPanel>(). Оба прежних вызова заменены. Данные, лимиты, прокрутка, visibility и алгоритм клонирования не меняются.

Проверки:

- Новый тест использует RuntimeHelpers.PrepareMethod для всех вызываемых parent-lookup методов Unity: компилирует метод, но не запускает Unity или статические инициализаторы.
- Окончательная версия теста на 1.0.4: RED 21/22, точный текст VerificationException из пользовательского лога.
- На 1.0.5: GREEN 22/22, Release без ошибок/предупреждений. IL-декодер тестов дополнен generic context при ResolveMethod для сканирования всей сборки, включая общие методы StationRowExpansion<T>.
- API текущей игры: 169 внешних ссылок, 13 целей Harmony, 0 ошибок.
- Повторный игровой запуск нужен, чтобы подтвердить отсутствие исключений при загрузке Main и повторном открытии планшета. Отсутствие проблем изображения не означает отсутствие этих исключений в логе.

Отдельное ревью не нашло Critical/Important замечаний и независимо подтвердило RED на 1.0.4 и GREEN на 1.0.5. Игру ревьюер не запускал.

Артефакты: artifacts/hr-parent-lookup-1.0.5. Перед установкой повторно проверено отсутствие процесса игры. Установлена только DLL 1.0.5, SHA256 6D293EE64D743B688F4CD0DB3C9BED52CD6A4A9F7FFA2D84C10B010949CC1101. Резервная 1.0.4 сохранена вне Mods: artifacts/hr-backups/2026-10-30/SkoofidonsHRDepartment.v1.0.4.0.22D78E41.dll. Игровое подтверждение исправления ожидается; GitHub-релиз не создавался.
