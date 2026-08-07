# Project Rules & Design Guidelines: Apple Fluid UI System

For all UI development, components, styling, animations, typography, interactions, and design decisions in this workspace, strictly follow Apple's **Fluid Interface & WWDC Design Principles**.

---

## 1. Response & Latency (Kill Latency)
- **Instant Press Feedback**: Always respond on `pointerdown` / `:active`, not on release. Use `transform: scale(0.97)` on active states with `transition: transform 100ms ease-out`.
- **Continuous Feedback**: Update UI 1:1 with pointer movements during interactions (drag, slider, drawer) — never animate only when gesture completes.
- **Audit Latency**: Eliminate artificial timers, 300ms tap delays, and unneeded transitions on input paths.

---

## 2. Direct Manipulation (1:1 Tracking)
- **1:1 Tracking**: Content stays glued to finger/pointer and respects the grab offset from where it was picked up.
- **Pointer Capture**: Use `setPointerCapture(e.pointerId)` so tracking continues even when the pointer leaves the element's bounds.
- **Velocity Tracking**: Track short position + timestamp history for momentum release.

---

## 3. Interruptibility & Physics-Based Motion
- **Interruptible Animations**: Every animation must be interruptible and redirectable mid-flight without input locking or positional jumps.
- **Presentation Value Re-targeting**: Read live presentation/on-screen transform on interrupt and start new motion from there.
- **Velocity-Aware Springs**: Use spring motion (critically damped `damping: 1.0` by default; slight bounce `damping: ~0.8` only for momentum flick release).

---

## 4. Materials, Depth & Translucency
- **Glassmorphic Hierarchy**: Use `backdrop-filter: blur(20px) saturate(180%)` with semi-transparent backgrounds (`rgba(255,255,255,0.65)` / dark modes).
- **Subtle Light Edges**: Use `border-top: 1px solid rgba(255,255,255,0.4)` to simulate light catching physical glass materials.
- **Rubber-Banding**: Progressively resist movement at boundary limits using rubber-band decay instead of hard stopping.

---

## 5. Typography & Spatial Consistency
- **Optical Letter-Spacing**: Tighten tracking on large display headings (`letter-spacing: -0.02em`), leave body copy neutral.
- **Leading**: Inversely scale line-height with font size.
- **Spatial Anchoring**: Enter and exit along symmetric paths (e.g. popovers scale from trigger origin).

---

## 6. Apple Core Principles (WWDC)
1. **Purpose** - Build with intention; spend user's attention budget wisely.
2. **Agency** - Keep people in control with easy undo and non-blocking flows.
3. **Responsibility** - Privacy, safety, clear previews, disclaimers, and transparent AI responses.
4. **Familiarity** - Consistent spatial mental models and platform physics.
5. **Flexibility** - Adapt seamlessly across devices and accessibility needs.
6. **Simplicity** - Clear hierarchy, concise copy, and logical progressive disclosure.
7. **Craft** - Pixel-perfect alignment, typography, and frame-level smoothness.
8. **Delight** - Earned through natural, responsive, and seamless interaction.
