# PROJECT INTENT — LAUNCHER

This project is a production-grade desktop launcher application.

Core principles:

1. Clean, minimal UI
2. Strict theme consistency
3. No visual fragmentation
4. Centralized infrastructure control
5. Maintainable long-term architecture

--------------------------------------------------
UI PHILOSOPHY
--------------------------------------------------

The UI must be:

- Visually consistent across all screens
- Fully controlled by the theme system
- Easily extensible for future themes
- Minimalist and modern

Theme logic must NEVER be scattered.
All visual identity is controlled centrally.

--------------------------------------------------
INFRASTRUCTURE PHILOSOPHY
--------------------------------------------------

Logging is centralized.
No debug behavior should be scattered.
Observability must be predictable.

--------------------------------------------------
LONG TERM GOAL
--------------------------------------------------

This launcher should evolve into:
- A highly polished productivity tool
- Architecturally clean
- Easy to scale
- AI-integratable in the future

All code decisions must favor long-term maintainability over quick hacks.