import assert from "node:assert/strict";
import { test } from "node:test";
import { GazeStabilizer, STABILIZATION_PRESETS, clampToViewport, viewportChanged } from "../src/gaze.ts";

test("viewport clamping includes the circle border and preserves interior positions", () => {
  const viewport = { x: 800, y: 650 };
  const size = { x: 26, y: 26 };
  assert.deepEqual(clampToViewport({ x: -100, y: 1000 }, viewport, size), { x: 13, y: 637 });
  assert.deepEqual(clampToViewport({ x: 1000, y: -100 }, viewport, size), { x: 787, y: 13 });
  assert.deepEqual(clampToViewport({ x: 300, y: 200 }, viewport, size), { x: 300, y: 200 });
  assert.deepEqual(clampToViewport({ x: 100, y: -100 }, { x: 20, y: 10 }, size), { x: 10, y: 5 });
});

test("first gaze point is not shifted toward the origin", () => {
  assert.deepEqual(new GazeStabilizer().update({ x: 500, y: 300 }, 0), { x: 500, y: 300 });
});

test("balanced holds stationary jitter inside its stability radius", () => {
  const filter = new GazeStabilizer();
  filter.update({ x: 500, y: 300 }, 0);
  for (let i = 1; i <= 120; i++) {
    const result = filter.update({
      x: 500 + 10 * Math.sin(i * 2.3),
      y: 300 + 10 * Math.cos(i * 1.7)
    }, i * 1000 / 15);
    assert.deepEqual(result, { x: 500, y: 300 });
  }
});

test("an isolated large spike does not move a settled indicator", () => {
  const filter = new GazeStabilizer();
  for (let i = 0; i < 5; i++) filter.update({ x: 300, y: 200 }, i * 100);
  assert.deepEqual(filter.update({ x: 1200, y: -800 }, 500), { x: 300, y: 200 });
  assert.deepEqual(filter.update({ x: 300, y: 200 }, 600), { x: 300, y: 200 });
});

test("the second sample cannot pull a fresh indicator toward an isolated spike", () => {
  const filter = new GazeStabilizer();
  filter.update({ x: 300, y: 200 }, 0);
  assert.deepEqual(filter.update({ x: 900, y: 600 }, 100), { x: 300, y: 200 });
  assert.deepEqual(filter.update({ x: 300, y: 200 }, 200), { x: 300, y: 200 });
});

test("balanced removes at least 80 percent of stationary jitter RMS at 15 Hz", () => {
  const filter = new GazeStabilizer();
  const anchor = { x: 500, y: 300 };
  filter.update(anchor, 0);
  let rawSquared = 0;
  let filteredSquared = 0;
  for (let i = 1; i <= 180; i++) {
    const input = {
      x: anchor.x + 35 * Math.sin(i * 2.1),
      y: anchor.y + 30 * Math.cos(i * 2.4)
    };
    const output = filter.update(input, i * 1000 / 15);
    rawSquared += (input.x - anchor.x) ** 2 + (input.y - anchor.y) ** 2;
    filteredSquared += (output.x - anchor.x) ** 2 + (output.y - anchor.y) ** 2;
  }
  assert.ok(Math.sqrt(filteredSquared / rawSquared) <= 0.2,
    `Remaining RMS fraction: ${Math.sqrt(filteredSquared / rawSquared)}`);
});

test("balanced follows a sustained 400 px shift to within 35 px in eight samples", () => {
  const filter = new GazeStabilizer();
  filter.update({ x: 100, y: 200 }, 0);
  let output = { x: 100, y: 200 };
  for (let i = 1; i <= 8; i++) {
    output = filter.update({ x: 500, y: 200 }, i * 1000 / 15);
    assert.ok(output.x >= 100 && output.x <= 500);
  }
  assert.ok(500 - output.x <= 35, `Remaining error: ${500 - output.x}`);
});

test("a slow intentional drift eventually escapes the stability radius", () => {
  const filter = new GazeStabilizer();
  filter.update({ x: 100, y: 200 }, 0);
  let output = { x: 100, y: 200 };
  for (let i = 1; i <= 120; i++)
    output = filter.update({ x: 100 + i, y: 200 }, i * 1000 / 15);
  assert.ok(output.x > 190 && output.x <= 220, `Drift output: ${output.x}`);
});

test("Off is raw gaze without spike rejection, hold radius or smoothing", () => {
  const filter = new GazeStabilizer("off");
  filter.update({ x: 100, y: 200 }, 0);
  assert.deepEqual(filter.update({ x: 900, y: -100 }, 100), { x: 900, y: -100 });
});

test("preset strength increases both stability radius and response smoothing", () => {
  assert.ok(STABILIZATION_PRESETS.light.radius < STABILIZATION_PRESETS.balanced.radius);
  assert.ok(STABILIZATION_PRESETS.balanced.radius < STABILIZATION_PRESETS.strong.radius);
  const results = (["light", "balanced", "strong"] as const).map(mode => {
    const filter = new GazeStabilizer(mode);
    filter.update({ x: 0, y: 0 }, 0);
    filter.update({ x: 200, y: 0 }, 100);
    return filter.update({ x: 200, y: 0 }, 200).x;
  });
  assert.ok(results[0] > results[1] && results[1] > results[2]);
});

test("lower speed slows the glide without resetting position", () => {
  function after(speed: number): number {
    const filter = new GazeStabilizer("balanced", 0);
    filter.update({ x: 100, y: 200 }, 0);
    filter.setSpeed(speed);
    let output = { x: 100, y: 200 };
    for (let i = 1; i <= 8; i++) output = filter.update({ x: 500, y: 200 }, i * 1000 / 15);
    return output.x;
  }
  const full = after(1), half = after(0.5), slowest = after(0.1);
  assert.ok(full > half && half > slowest && slowest > 100, `${full} ${half} ${slowest}`);
  for (const invalid of [0, 0.05, 1.5, NaN]) assert.throws(() => new GazeStabilizer().setSpeed(invalid), /speed/);
});

test("zero radius allows small sustained movement rather than holding it", () => {
  const filter = new GazeStabilizer("balanced", 0);
  filter.update({ x: 100, y: 200 }, 0);
  let output = { x: 100, y: 200 };
  for (let i = 1; i <= 20; i++)
    output = filter.update({ x: 105, y: 200 }, i * 100);
  assert.ok(output.x > 104.9 && output.x <= 105);
});

test("reset and settings changes seed the next point without stale history", () => {
  const filter = new GazeStabilizer();
  filter.update({ x: 100, y: 200 }, 0);
  filter.reset();
  assert.deepEqual(filter.update({ x: 900, y: 600 }, 100), { x: 900, y: 600 });
  filter.configure("strong", 40);
  assert.deepEqual(filter.update({ x: 300, y: 200 }, 200), { x: 300, y: 200 });
});

test("long prediction gaps do not interpolate from a stale location", () => {
  const filter = new GazeStabilizer();
  filter.update({ x: 100, y: 200 }, 0);
  assert.deepEqual(filter.update({ x: 900, y: 600 }, 600), { x: 900, y: 600 });
});

test("time-based smoothing stays comparable at 15 and 30 Hz", () => {
  function step(rate: number): number {
    const filter = new GazeStabilizer("balanced", 0);
    filter.update({ x: 100, y: 200 }, 0);
    let output = { x: 100, y: 200 };
    for (let i = 1; i <= rate; i++)
      output = filter.update({ x: 120, y: 200 }, i * 1000 / rate);
    return output.x;
  }
  assert.ok(Math.abs(step(15) - step(30)) < 0.5);
});

test("invalid predictions surface an error rather than becoming a cursor position", () => {
  for (const invalid of [NaN, Infinity, -Infinity]) {
    assert.throws(() => new GazeStabilizer().update({ x: invalid, y: 0 }, 0), /invalid gaze/);
    assert.throws(() => new GazeStabilizer().update({ x: 0, y: invalid }, 0), /invalid gaze/);
    assert.throws(() => new GazeStabilizer().update({ x: 0, y: 0 }, invalid), /timestamp/);
  }
});

test("invalid settings and non-increasing timestamps surface explicit errors", () => {
  assert.throws(() => new GazeStabilizer("balanced", -1), /radius/);
  assert.throws(() => new GazeStabilizer("balanced", 61), /radius/);
  assert.throws(() => new GazeStabilizer("balanced", NaN), /radius/);
  const filter = new GazeStabilizer();
  filter.update({ x: 100, y: 200 }, 100);
  assert.throws(() => filter.update({ x: 100, y: 200 }, 100), /timestamp/);
});

test("viewport changes invalidate calibration in either direction", () => {
  assert.equal(viewportChanged({ x: 800, y: 600 }, { x: 800, y: 600 }), false);
  assert.equal(viewportChanged({ x: 800, y: 600 }, { x: 900, y: 600 }), true);
  assert.equal(viewportChanged({ x: 800, y: 600 }, { x: 800, y: 500 }), true);
});
