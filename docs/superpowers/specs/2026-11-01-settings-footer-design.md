# Settings footer — approved 2026-11-01

User approved the graphical prototype `artifacts/settings-footer-2026-11-01/design/approved-footer-en.png` in chat and explicitly requested implementation and a test build.

Move the current-tab reset from the panel header into the common footer: Close → Reset to defaults → Apply changes. Apply remains the primary teal action. Show Current tab only over reset. RU labels: Закрыть / По умолчанию / Применить изменения. The HR reset notice also names Apply changes / Применить изменения.

Use the existing common host footer, with an optional BCL reset callback in the provider manifest. This fits the provider architecture and lets future mods participate without host-specific type checks. A provider-owned duplicate footer would complicate layout and modal blocking. HR supplies its existing draft reset; the legacy Minimap adapter supplies its existing confirmation request. Minimap has no Apply action because its changes apply immediately; its footer is Close → Reset to defaults. Preserve all reset, persistence, modal, input and language behavior. Do not update Minimap DLL or change any configuration values.

Allow longer button labels without overlap: reserve their widths, move status to a separate line on narrower windows, adjust content/sidebar bounds to the resulting footer height. Older providers without reset remain supported. Raise the embedded host capability version and reset providers' minimum version so an older host cannot silently omit reset.

Scope is a local HR beta.5 test build, documentation and offline verification. No publication or merge. Native visual/input acceptance remains a game check.
