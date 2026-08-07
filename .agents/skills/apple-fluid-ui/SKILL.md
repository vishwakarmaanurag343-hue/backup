---
name: apple-fluid-ui
description: Complete Apple WWDC Fluid UI design system for responsive, spring-animated, tactile web applications.
---

# Apple Fluid UI Design System

How Apple builds interfaces that stop feeling like a computer and start feeling like an extension of you. This knowledge comes from Apple's WWDC design talks — chiefly *Designing Fluid Interfaces (WWDC 2018)* — distilled and translated into the web platform (CSS, Pointer Events, requestAnimationFrame, Motion/Framer Motion).

## The Core Idea
"When we align the interface to the way we think and move, something magical happens — it stops feeling like a computer and starts feeling like a seamless extension of us."

An interface is fluid when it behaves like the physical world: things respond instantly, move continuously, carry momentum, resist at boundaries, and can be redirected mid-motion.

---

## 1. Response — Kill Latency
- Respond on pointer-down (`pointerdown` / `:active`), not on release.
- Audit debounces, artificial timers, transition waits, and tap delays.
- Feedback must be continuous during interaction, not just at the end.

```css
.button:active {
  transform: scale(0.97);
  transition: transform 100ms ease-out;
}
```

---

## 2. Direct Manipulation — 1:1 Tracking
- Touch and content move together; respect grab offset.
- Use Pointer Events with `setPointerCapture`.
- Track velocity history (last few `pointermove` events).

```javascript
el.addEventListener('pointerdown', (e) => {
  el.setPointerCapture(e.pointerId);
  const grabOffset = e.clientY - el.getBoundingClientRect().top;
});
```

---

## 3. Interruptibility — Principle #1
- Every animation must be interruptible and redirectable at any moment.
- Never lock out input during a transition.
- Animate from the live presentation value, not target value.
- Carry velocity through re-targets to prevent velocity discontinuities ("brick wall").

---

## 4. Behavior Over Animation — Use Springs
- **Damping ratio**: 1.0 = critically damped (no overshoot). < 1.0 = bouncier.
- **Response**: how quickly the value reaches the target (snappiness).

| Interaction | Damping | Response |
|---|---|---|
| Move / Reposition | 1.0 | 0.4 |
| Rotation | 0.8 | 0.4 |
| Drawer / Sheet | 0.8 | 0.3 |

```javascript
import { animate } from 'motion';

// Critically damped default
animate(el, { y: 0 }, { type: 'spring', bounce: 0, duration: 0.4 });

// Momentum interaction
animate(el, { y: target }, { type: 'spring', bounce: 0.2, duration: 0.4 });
```

---

## 5. Velocity Handoff & Momentum Projection
- Pass release velocity to spring: `relativeVelocity = gestureVelocity / (target - current)`.
- Project resting position: `projectedEndpoint = currentPosition + (releaseVelocity / 1000) * d / (1 - d)` (`d ≈ 0.998`).

---

## 6. Spatial Consistency & Rubber-Banding
- Enter and exit along symmetric paths.
- Anchor popovers/menus to trigger origin (`transform-origin`).
- Apply progressive resistance at edges:
```javascript
function rubberband(overshoot, dimension, constant = 0.55) {
  return (overshoot * dimension * constant) / (dimension + constant * Math.abs(overshoot));
}
```

---

## 7. Materials, Depth & Translucency
```css
.toolbar {
  background: rgba(255, 255, 255, 0.6);
  backdrop-filter: blur(20px) saturate(180%);
  border-top: 1px solid rgba(255, 255, 255, 0.4);
}
```

---

## 8. Typography Guidelines
- Tighten tracking on large display headings (`letter-spacing: -0.02em`).
- Scale leading (line-height) inversely with font size.
- Use system font stack by default (`system-ui, -apple-system, sans-serif`).

---

## 9. Core Design Foundations (WWDC)
1. Purpose
2. Agency
3. Responsibility
4. Familiarity
5. Flexibility
6. Simplicity
7. Craft
8. Delight
