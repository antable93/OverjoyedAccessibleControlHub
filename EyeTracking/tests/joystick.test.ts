import assert from "node:assert/strict";
import { test } from "node:test";
import { joystickVelocity, stepPosition } from "../src/joystick.ts";

const viewport = { x: 800, y: 600 };

test("gaze inside the dead zone does not move the control", () => {
  assert.deepEqual(joystickVelocity({ x: 400, y: 300 }, viewport, 0.15, 300), { x: 0, y: 0 });
  assert.deepEqual(joystickVelocity({ x: 450, y: 320 }, viewport, 0.15, 300), { x: 0, y: 0 });
});

test("velocity points in the gaze direction", () => {
  const right = joystickVelocity({ x: 700, y: 300 }, viewport, 0.15, 300);
  assert.ok(right.x > 0);
  assert.equal(right.y, 0);
  const upLeft = joystickVelocity({ x: 100, y: 0 }, viewport, 0.15, 300);
  assert.ok(upLeft.x < 0 && upLeft.y < 0);
});

test("speed ramps from zero to the maximum at the edge", () => {
  const near = joystickVelocity({ x: 500, y: 300 }, viewport, 0.15, 300).x;
  const far = joystickVelocity({ x: 700, y: 300 }, viewport, 0.15, 300).x;
  const edge = joystickVelocity({ x: 800, y: 300 }, viewport, 0.15, 300).x;
  const beyond = joystickVelocity({ x: 5000, y: 300 }, viewport, 0.15, 300).x;
  assert.ok(near > 0 && near < far && far < edge);
  assert.equal(edge, 300);
  assert.equal(beyond, 300);
});

test("invalid tuning is rejected", () => {
  assert.throws(() => joystickVelocity({ x: 0, y: 0 }, viewport, 1, 300), RangeError);
  assert.throws(() => joystickVelocity({ x: 0, y: 0 }, viewport, 0.1, Number.NaN), RangeError);
});

test("stepping integrates velocity and stays inside the viewport", () => {
  const size = { x: 20, y: 20 };
  assert.deepEqual(stepPosition({ x: 400, y: 300 }, { x: 100, y: -50 }, 0.5, viewport, size), { x: 450, y: 275 });
  assert.deepEqual(stepPosition({ x: 780, y: 15 }, { x: 300, y: -300 }, 1, viewport, size), { x: 790, y: 10 });
  assert.deepEqual(stepPosition({ x: 400, y: 300 }, { x: 100, y: 0 }, -1, viewport, size), { x: 400, y: 300 });
});
