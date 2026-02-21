# HARD ENGINEERING RULES (MANDATORY)

These rules are non-negotiable.
All generated or modified code MUST comply.

--------------------------------------------------
1. THEME SYSTEM (CRITICAL)
--------------------------------------------------

- The application supports Black and White themes.
- ALL UI styling MUST come from the global theme system.
- NO hardcoded colors.
- NO inline Brush definitions.
- NO direct usage of #HEX colors in XAML or C#.
- NO new SolidColorBrush unless defined in theme resources.

All visual properties MUST use:
- DynamicResource
- Theme ResourceDictionary
- Centralized Style definitions

Violation example:
Background="#000000"   ❌
new SolidColorBrush()  ❌

Correct example:
Background="{DynamicResource PrimaryBackgroundBrush}"  ✅

--------------------------------------------------
2. LOGGING SYSTEM (CRITICAL)
--------------------------------------------------

- ALL debug or runtime logs MUST go through the centralized Logger class.
- Direct usage of:
  Console.WriteLine
  Debug.WriteLine
  Trace.WriteLine
  is strictly forbidden.

Only allowed:
Logger.Debug(...)
Logger.Info(...)
Logger.Warn(...)
Logger.Error(...)

No custom logging implementations allowed.

--------------------------------------------------
3. ARCHITECTURE

- MVVM strictly enforced.
- No business logic in code-behind.
- ViewModel must not reference View.
- Services injected via constructor.

--------------------------------------------------
4. CODE STYLE

- No static mutable state.
- No duplicated logic.
- Async methods must use async/await.
- No .Result or .Wait()

--------------------------------------------------
5. SELF-CHECK BEFORE OUTPUT

Before finalizing any response, verify:

[ ] No hardcoded colors
[ ] No inline brushes
[ ] Theme system respected
[ ] All logs use Logger class
[ ] MVVM respected

If any violation exists, fix it before output.