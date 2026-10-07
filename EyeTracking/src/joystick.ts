import { clampToViewport, type Position } from "./gaze.ts";

export const JOYSTICK_LIMITS = {
  speed: { min: 50, max: 800, initial: 300 },
  deadZone: { min: 5, max: 40, initial: 15 }
} as const;

/**
 * Treats the gaze offset from the viewport centre as a joystick deflection.
 * deadZone is a fraction (0-1) of the half-viewport; speed ramps quadratically
 * from 0 at the dead-zone edge to maxSpeed (px/s) at the viewport edge.
 */
export function joystickVelocity(gaze: Position, viewport: Position, deadZone: number, maxSpeed: number): Position {
  if (!(deadZone >= 0 && deadZone < 1)) throw new RangeError("Dead zone must be between 0 and 1.");
  if (!(maxSpeed >= 0) || !Number.isFinite(maxSpeed)) throw new RangeError("Speed must be a non-negative number.");
  const halfX = viewport.x / 2;
  const halfY = viewport.y / 2;
  if (halfX <= 0 || halfY <= 0) return { x: 0, y: 0 };
  const nx = (gaze.x - halfX) / halfX;
  const ny = (gaze.y - halfY) / halfY;
  const magnitude = Math.hypot(nx, ny);
  if (!Number.isFinite(magnitude) || magnitude <= deadZone) return { x: 0, y: 0 };
  const deflection = Math.min(1, (magnitude - deadZone) / (1 - deadZone));
  const speed = maxSpeed * deflection * deflection;
  return { x: nx / magnitude * speed, y: ny / magnitude * speed };
}

export function stepPosition(position: Position, velocity: Position, seconds: number,
  viewport: Position, size: Position): Position {
  const dt = Math.max(0, seconds);
  return clampToViewport({ x: position.x + velocity.x * dt, y: position.y + velocity.y * dt }, viewport, size);
}
