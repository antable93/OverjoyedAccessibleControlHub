export interface Position { x: number; y: number }

export function clampToViewport(point: Position, viewport: Position, size: Position): Position {
  function clamp(value: number, extent: number, diameter: number): number {
    const margin = Math.min(diameter / 2, extent / 2);
    return Math.max(margin, Math.min(extent - margin, value));
  }
  return { x: clamp(point.x, viewport.x, size.x), y: clamp(point.y, viewport.y, size.y) };
}

export const STABILIZATION_PRESETS = {
  off: { radius: 0, restingMs: 0, movingMs: 0 },
  light: { radius: 8, restingMs: 100, movingMs: 45 },
  balanced: { radius: 18, restingMs: 220, movingMs: 60 },
  strong: { radius: 32, restingMs: 360, movingMs: 100 }
} as const;

export type StabilizationMode = keyof typeof STABILIZATION_PRESETS;

export function isStabilizationMode(value: string): value is StabilizationMode {
  return Object.hasOwn(STABILIZATION_PRESETS, value);
}

function median(values: number[]): number {
  const ordered = [...values].sort((a, b) => a - b);
  return ordered[1];
}

export class GazeStabilizer {
  private mode: StabilizationMode = "balanced";
  private radius: number = STABILIZATION_PRESETS.balanced.radius;
  private speed = 1;
  private history: Position[] = [];
  private output: Position | null = null;
  private lastTimestamp: number | null = null;

  constructor(mode: StabilizationMode = "balanced", radius: number = STABILIZATION_PRESETS[mode].radius) {
    this.configure(mode, radius);
  }

  configure(mode: StabilizationMode, radius: number): void {
    if (!isStabilizationMode(mode)) throw new Error("Unknown gaze stabilization mode.");
    if (!Number.isFinite(radius) || radius < 0 || radius > 60)
      throw new Error("Gaze stability radius must be between 0 and 60 CSS pixels.");
    this.mode = mode;
    this.radius = radius;
    this.reset();
  }

  /** Scales glide speed (0.1 = ten times slower, 1 = preset speed) without discarding position history. */
  setSpeed(speed: number): void {
    if (!Number.isFinite(speed) || speed < 0.1 || speed > 1)
      throw new Error("Gaze speed must be between 10 and 100 percent.");
    this.speed = speed;
  }

  reset(): void {
    this.history = [];
    this.output = null;
    this.lastTimestamp = null;
  }

  update(next: Position, timestamp: number): Position {
    if (!Number.isFinite(next.x) || !Number.isFinite(next.y))
      throw new Error("The tracker returned invalid gaze coordinates.");
    if (!Number.isFinite(timestamp) ||
        (this.lastTimestamp !== null && timestamp <= this.lastTimestamp))
      throw new Error("Gaze timestamp must be finite and strictly increasing.");

    if (this.mode === "off" || this.output === null || this.lastTimestamp === null ||
        timestamp - this.lastTimestamp > 500) {
      this.history = [{ ...next }];
      this.output = { ...next };
      this.lastTimestamp = timestamp;
      return { ...this.output };
    }

    const elapsed = timestamp - this.lastTimestamp;
    this.lastTimestamp = timestamp;
    this.history.push({ ...next });
    if (this.history.length > 3) this.history.shift();
    // Repeat the initial sample until the median window fills; a second-frame spike must not move the cursor.
    const samples = this.history.length === 2
      ? [this.history[0], ...this.history]
      : this.history;
    const target = {
      x: median(samples.map(point => point.x)),
      y: median(samples.map(point => point.y))
    };
    const dx = target.x - this.output.x;
    const dy = target.y - this.output.y;
    const distance = Math.hypot(dx, dy);
    if (distance <= this.radius) return { ...this.output };

    const preset = STABILIZATION_PRESETS[this.mode];
    const excess = distance - this.radius;
    const responsiveness = Math.min(1, excess / 150);
    const timeConstant = (preset.restingMs +
      (preset.movingMs - preset.restingMs) * responsiveness) / this.speed;
    const alpha = -Math.expm1(-elapsed / timeConstant);
    // A soft deadband avoids a discontinuous jump when gaze first leaves the hold radius.
    const movement = alpha * excess / distance;
    this.output = {
      x: this.output.x + dx * movement,
      y: this.output.y + dy * movement
    };
    return { ...this.output };
  }
}

export function viewportChanged(before: Position, after: Position): boolean {
  return before.x !== after.x || before.y !== after.y;
}
