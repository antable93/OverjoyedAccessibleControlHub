import assert from "node:assert/strict";
import { test } from "node:test";
import vm from "node:vm";
import { build } from "esbuild";
import { fileURLToPath } from "node:url";

const bundled = await build({
  entryPoints: [fileURLToPath(new URL("../src/main.ts", import.meta.url))],
  bundle: true,
  write: false,
  format: "iife",
  plugins: [{
    name: "fake-tracker",
    setup(builder) {
      builder.onResolve({ filter: /^@realeye-io\/webcam-eyetracker-light-open$/ }, () =>
        ({ path: "tracker", namespace: "test" }));
      builder.onLoad({ filter: /.*/, namespace: "test" }, () => ({
        contents: `
          export const WebcamETLight = globalThis.TestTracker;
          export const getRecommendedPattern = () => "GRID_17";
          export const getCalibrationPointsInPixels = (_, width, height) =>
            Array.from({length: 17}, (_, i) => ({x: width * (i + 1) / 18, y: height / 2}));
        `
      }));
    }
  }]
});

function harness(options = {}) {
  class Element extends EventTarget {
    disabled = false;
    hidden = false;
    textContent = "";
    dataset = {};
    style = {};
    value = "CPU";
    options = [];
    videoWidth = 640;
    videoHeight = 480;
    srcObject = null;
    checked = false;
    classes = new Set();
    classList = {
      add: name => this.classes.add(name),
      remove: name => this.classes.delete(name),
      contains: name => this.classes.has(name)
    };
    async play() {}
    showModal() { this.open = true; }
    close() { this.open = false; }
    getBoundingClientRect() { return { width: 26, height: 26 }; }
    replaceChildren(...options) { this.options = options; this.value = options[0]?.value ?? ""; }
    getContext() { return { drawImage() {}, getImageData: () => ({ frame: true }) }; }
  }
  const elements = new Map(
    ["start", "stop", "calibrate", "delegate", "preview", "status", "coordinates",
      "calibration", "calibration-status", "target", "gaze", "cancel",
      "camera", "refresh-cameras", "camera-status", "grant-camera",
      "stabilization", "stability-radius", "stability-radius-value", "gaze-speed", "gaze-speed-value", "help", "show-help", "close-help",
      "joystick", "joystick-speed", "joystick-speed-value", "dead-zone", "dead-zone-value",
      "recenter", "control", "dead-zone-ring"]
      .map(id => [id, new Element()])
  );
  elements.get("calibrate").disabled = true;
  elements.get("stop").disabled = true;
  elements.get("calibration").hidden = true;
  elements.get("gaze").hidden = true;
  elements.get("control").hidden = true;
  elements.get("dead-zone-ring").hidden = true;
  elements.get("joystick").checked = options.joystick ?? true;
  elements.get("joystick-speed").value = "300";
  elements.get("dead-zone").value = "15";
  elements.get("camera").value = "";
  elements.get("stabilization").value = "balanced";
  elements.get("stability-radius").value = "18";
  elements.get("gaze-speed").value = "50";
  const frames = new Map();
  const trackers = [];
  const messages = [];
  let stopped = 0;
  let requests = 0;
  const constraints = [];
  const track = Object.assign(new EventTarget(), {
    stop: () => stopped++,
    getSettings: () => ({ deviceId: options.actualDevice })
  });
  const stream = { getTracks: () => [track], getVideoTracks: () => [track] };
  class Tracker {
    constructor(config) { this.config = config; trackers.push(this); }
    async initialize() { await options.initialize?.(); }
    dispose() { this.disposed = true; }
    detectFace() { return options.faceLost ? null : {}; }
    calibrate(samples) { this.samples = samples; }
    predict() {
      this.predictions = (this.predictions ?? 0) + 1;
      return typeof options.prediction === "function" ? options.prediction() : options.prediction ?? null;
    }
  }
  const bridge = Object.assign(new EventTarget(), {
    postMessage: message => {
      messages.push(message);
      if (message.startsWith("request-camera:") && !options.ignoreAuthorization)
        queueMicrotask(() => bridge.dispatchEvent(new MessageEvent("message", {
          data: message.replace("request-camera:", "camera-authorized:")
        })));
    }
  });
  const window = Object.assign(new EventTarget(), {
    chrome: { webview: bridge }
  });
  const document = Object.assign(new EventTarget(), {
    baseURI: "https://eye-tracking.overjoyed.invalid/index.html",
    hidden: false,
    getElementById: id => elements.get(id),
    createElement: () => new Element()
  });
  const context = vm.createContext({
    window, document, URL, TestTracker: Tracker,
    Option: class {
      constructor(text, value) { this.text = text; this.value = value; }
    },
    HTMLElement: Element, HTMLButtonElement: Element, HTMLSelectElement: Element,
    HTMLVideoElement: Element, HTMLParagraphElement: Element, HTMLDivElement: Element,
    HTMLInputElement: Element, HTMLOutputElement: Element,
    HTMLDialogElement: Element,
    navigator: { mediaDevices: Object.assign(new EventTarget(), {
      enumerateDevices: async () => {
        if (options.enumerationError) throw new Error(options.enumerationError);
        return options.enumerate ? await options.enumerate() : options.devices ?? [];
      },
      getUserMedia: async value => {
      requests++;
      constraints.push(value);
      return options.acquire ? await options.acquire(stream) : stream;
    } }) },
    innerWidth: 800, innerHeight: 650,
    setTimeout: (callback, ms) => { if (ms < 10000) queueMicrotask(callback); return 1; },
    clearTimeout() {},
    requestAnimationFrame: callback => { const id = frames.size + 1; frames.set(id, callback); return id; },
    cancelAnimationFrame: id => frames.delete(id),
    console: { error() {} }
  });
  vm.runInContext(bundled.outputFiles[0].text, context);
  return {
    elements, trackers, messages, document, window, context, constraints,
    requests: () => requests,
    stopped: () => stopped,
    click: id => elements.get(id).dispatchEvent(new Event("click")),
    frame: timestamp => {
      const callback = frames.values().next().value;
      frames.clear();
      assert.ok(callback, "a prediction frame must be scheduled");
      callback(timestamp);
    }
  };
}

test("the full circle stays inside every viewport edge and coordinates match its position", async () => {
  const options = { prediction: { x: -500, y: -500 } };
  const app = await trackingHarness(options);
  app.elements.get("stabilization").value = "off";
  app.elements.get("stabilization").dispatchEvent(new Event("change"));
  app.frame(100);
  assert.equal(app.elements.get("gaze").style.left, "13px");
  assert.equal(app.elements.get("gaze").style.top, "13px");
  assert.match(app.elements.get("coordinates").textContent, /13, 13 CSS pixels/);
  options.prediction = { x: 5000, y: 5000 };
  app.frame(200);
  assert.equal(app.elements.get("gaze").style.left, "787px");
  assert.equal(app.elements.get("gaze").style.top, "637px");
  assert.match(app.elements.get("coordinates").textContent, /787, 637 CSS pixels/);
});

test("help opens separately and closes without stopping the camera", async () => {
  const app = await trackingHarness({ prediction: { x: 300, y: 200 } });
  app.click("show-help");
  assert.equal(app.elements.get("help").open, true);
  app.click("close-help");
  assert.equal(app.elements.get("help").open, false);
  assert.equal(app.stopped(), 0);
});

async function settle() {
  // Calibration has an asynchronous wait at each of its 17 targets.
  for (let i = 0; i < 3000; i++) await Promise.resolve();
}

async function trackingHarness(options) {
  const app = harness(options);
  app.click("start");
  await settle();
  app.click("calibrate");
  await settle();
  return app;
}

test("Balanced stabilizes the rendered ring but follows sustained gaze shifts", async () => {
  const options = { prediction: { x: 300, y: 200 } };
  const app = await trackingHarness(options);
  app.elements.get("gaze-speed").value = "100";
  app.elements.get("gaze-speed").dispatchEvent(new Event("input"));
  app.frame(100);
  options.prediction = { x: 310, y: 210 };
  app.frame(200);
  app.frame(300);
  assert.equal(app.elements.get("gaze").style.left, "300px");
  assert.equal(app.elements.get("gaze").style.top, "200px");
  options.prediction = { x: 700, y: 200 };
  for (let i = 4; i <= 11; i++) app.frame(i * 100);
  const x = parseFloat(app.elements.get("gaze").style.left);
  assert.ok(x >= 665 && x <= 700, `Rendered position: ${x}`);
});

test("preset changes reset history, update radius, and Off renders unfiltered coordinates", async () => {
  const options = { prediction: { x: 300, y: 200 } };
  const app = await trackingHarness(options);
  app.frame(100);
  const strength = app.elements.get("stabilization");
  strength.value = "strong";
  strength.dispatchEvent(new Event("change"));
  assert.equal(app.elements.get("stability-radius").value, "32");
  assert.equal(app.elements.get("stability-radius-value").textContent, "32 px");
  assert.equal(app.elements.get("gaze").hidden, false);
  options.prediction = { x: 600, y: 200 };
  app.frame(200);
  assert.equal(app.elements.get("gaze").style.left, "600px");
  strength.value = "off";
  strength.dispatchEvent(new Event("change"));
  assert.equal(app.elements.get("stability-radius").disabled, true);
  assert.equal(app.elements.get("gaze-speed").disabled, true);
  options.prediction = { x: 100, y: 100 };
  app.frame(300);
  options.prediction = { x: 790, y: 640 };
  app.frame(400);
  assert.equal(app.elements.get("gaze").style.left, "787px");
  assert.equal(app.elements.get("gaze").style.top, "637px");
  strength.value = "light";
  strength.dispatchEvent(new Event("change"));
  assert.equal(app.elements.get("stability-radius").value, "8");
  assert.equal(app.elements.get("stability-radius").disabled, false);
});

test("speed slider slows the circle while tracking without a jump", async () => {
  async function glide(speed) {
    const options = { prediction: { x: 300, y: 200 } };
    const app = await trackingHarness(options);
    app.frame(100);
    const slider = app.elements.get("gaze-speed");
    slider.value = speed;
    slider.dispatchEvent(new Event("input"));
    assert.equal(app.elements.get("gaze-speed-value").textContent, `${speed}%`);
    options.prediction = { x: 700, y: 200 };
    for (let i = 2; i <= 5; i++) app.frame(i * 100);
    return parseFloat(app.elements.get("gaze").style.left);
  }
  const fast = await glide("100"), slow = await glide("20");
  assert.ok(slow > 300 && slow < fast, `slow ${slow}, fast ${fast}`);
});

test("custom radius takes effect while tracking without restarting the camera", async () => {
  const options = { prediction: { x: 300, y: 200 } };
  const app = await trackingHarness(options);
  const radius = app.elements.get("stability-radius");
  radius.value = "60";
  radius.dispatchEvent(new Event("input"));
  assert.equal(app.elements.get("stability-radius-value").textContent, "60 px");
  app.frame(100);
  options.prediction = { x: 340, y: 200 };
  app.frame(200);
  app.frame(300);
  assert.equal(app.elements.get("gaze").style.left, "300px");
  assert.equal(app.requests(), 1);
  assert.equal(app.trackers[0].disposed, undefined);
});

test("tracking loss holds the circle indefinitely and a long gap reseeds reacquired gaze", async () => {
  const options = { prediction: { x: 300, y: 200 } };
  const app = await trackingHarness(options);
  app.frame(100);
  options.prediction = null;
  app.frame(200);
  assert.equal(app.elements.get("gaze").hidden, false);
  assert.equal(app.elements.get("gaze").style.left, "300px");
  assert.match(app.elements.get("status").textContent, /last known position/);
  app.frame(700);
  app.frame(10700);
  assert.equal(app.elements.get("gaze").hidden, false);
  assert.equal(app.elements.get("gaze").style.top, "200px");
  options.prediction = { x: 700, y: 400 };
  app.frame(10800);
  assert.equal(app.elements.get("gaze").style.left, "700px");
  assert.equal(app.elements.get("gaze").style.top, "400px");
});

test("isolated missed predictions do not blink or discard stabilization history", async () => {
  const options = { prediction: { x: 300, y: 200 } };
  const app = await trackingHarness(options);
  app.frame(100);
  for (let i = 2; i <= 10; i++) {
    options.prediction = i % 2 === 0 ? null : { x: 310, y: 210 };
    app.frame(i * 100);
    assert.equal(app.elements.get("gaze").hidden, false);
    assert.equal(app.elements.get("gaze").style.left, "300px");
    assert.equal(app.elements.get("gaze").style.top, "200px");
  }
  app.click("stop");
  assert.equal(app.elements.get("gaze").hidden, true);
});

test("tracking loss before any valid prediction does not invent a circle position", async () => {
  const app = await trackingHarness({ prediction: null });
  app.frame(100);
  app.frame(10100);
  assert.equal(app.elements.get("gaze").hidden, true);
  assert.match(app.elements.get("status").textContent, /Tracking lost/);
});

test("recalibration resets filter history and disables stabilization controls while sampling", async () => {
  const options = { prediction: { x: 300, y: 200 } };
  const app = await trackingHarness(options);
  app.frame(100);
  app.click("calibrate");
  assert.equal(app.elements.get("stabilization").disabled, true);
  assert.equal(app.elements.get("stability-radius").disabled, true);
  await settle();
  assert.equal(app.elements.get("stabilization").disabled, false);
  options.prediction = { x: 600, y: 500 };
  app.frame(200);
  assert.equal(app.elements.get("gaze").style.left, "600px");
});

test("loading the panel does not request the camera", () => {
  const app = harness();
  assert.equal(app.requests(), 0);
  assert.equal(app.elements.get("calibrate").disabled, true);
});

test("granting access reveals OBS before tracking starts and releases the temporary stream", async () => {
  let granted = false;
  const app = harness({
    enumerate: async () => granted ? [
      { kind: "videoinput", deviceId: "physical", label: "Laptop webcam" },
      { kind: "videoinput", deviceId: "obs", label: "OBS Virtual Camera" }
    ] : [{ kind: "videoinput", deviceId: "", label: "" }],
    acquire: async stream => { granted = true; return stream; }
  });
  await settle();
  assert.equal(app.elements.get("camera").options.length, 1);
  assert.match(app.elements.get("camera-status").textContent, /Grant camera access/);
  app.click("grant-camera");
  assert.equal(app.elements.get("start").disabled, true);
  await settle();
  assert.equal(app.requests(), 1);
  assert.equal(app.constraints[0].audio, false);
  assert.equal(app.stopped(), 1);
  assert.equal(app.trackers.length, 0);
  assert.deepEqual(app.elements.get("camera").options.map(option => option.text),
    ["System default camera", "Laptop webcam", "OBS Virtual Camera"]);
  assert.equal(app.elements.get("start").disabled, false);
  app.elements.get("camera").value = "obs";
  app.click("refresh-cameras");
  await settle();
  assert.equal(app.elements.get("camera").value, "obs");
  app.click("start");
  await settle();
  assert.equal(app.constraints[1].video.deviceId.exact, "obs");
});

test("canceling grant access releases the stream if permission resolves late", async () => {
  let complete;
  const pending = new Promise(resolve => { complete = resolve; });
  const app = harness({ acquire: async stream => { await pending; return stream; } });
  app.click("grant-camera");
  await settle();
  app.click("stop");
  complete();
  await settle();
  assert.equal(app.stopped(), 1);
  assert.equal(app.trackers.length, 0);
  assert.equal(app.elements.get("start").disabled, false);
});

test("denied grant access displays an error and allows retry without initializing tracking", async () => {
  const app = harness({ acquire: async () => { throw new Error("Permission denied"); } });
  app.click("grant-camera");
  await settle();
  assert.match(app.elements.get("status").textContent, /Permission denied/);
  assert.equal(app.elements.get("grant-camera").disabled, false);
  assert.equal(app.trackers.length, 0);
});

test("grant access refreshes cameras when permission succeeds but the default video source fails", async () => {
  let permitted = false;
  const app = harness({
    enumerate: async () => permitted ? [
      { kind: "videoinput", deviceId: "ndi", label: "NDI Webcam Video 1" },
      { kind: "videoinput", deviceId: "obs", label: "OBS Virtual Camera" }
    ] : [{ kind: "videoinput", deviceId: "", label: "" }],
    acquire: async () => {
      permitted = true;
      throw new Error("Timeout starting video source");
    }
  });
  await settle();
  app.click("grant-camera");
  await settle();
  assert.match(app.elements.get("status").textContent, /Timeout starting video source/);
  assert.deepEqual(app.elements.get("camera").options.map(option => option.text),
    ["System default camera", "NDI Webcam Video 1", "OBS Virtual Camera"]);
  assert.equal(app.elements.get("camera").disabled, false);
  assert.equal(app.trackers.length, 0);
});

test("camera list includes only webcams and supplies labels before permission", async () => {
  const app = harness({ devices: [
    { kind: "videoinput", deviceId: "built-in", label: "Laptop webcam" },
    { kind: "audioinput", deviceId: "microphone", label: "Microphone" },
    { kind: "videoinput", deviceId: "usb", label: "" }
  ] });
  await settle();
  assert.deepEqual(app.elements.get("camera").options.map(option => option.text),
    ["System default camera", "Laptop webcam", "Camera 2"]);
  assert.equal(app.requests(), 0);
});

test("selected camera uses an exact constraint and selection is locked until stopped", async () => {
  const app = harness({ devices: [
    { kind: "videoinput", deviceId: "usb", label: "USB webcam" }
  ] });
  await settle();
  app.elements.get("camera").value = "usb";
  app.click("start");
  assert.equal(app.elements.get("camera").disabled, true);
  assert.equal(app.elements.get("refresh-cameras").disabled, true);
  await settle();
  assert.equal(app.constraints[0].video.deviceId.exact, "usb");
  assert.equal(app.constraints[0].audio, false);
  assert.equal(app.elements.get("camera").value, "usb");
  app.click("stop");
  assert.equal(app.elements.get("camera").disabled, false);
  assert.equal(app.elements.get("refresh-cameras").disabled, false);
  assert.equal(app.elements.get("camera").value, "usb");
});

test("system default does not constrain the device and actual camera becomes selected", async () => {
  const app = harness({ actualDevice: "built-in", devices: [
    { kind: "videoinput", deviceId: "built-in", label: "Laptop webcam" }
  ] });
  await settle();
  app.click("start");
  await settle();
  assert.equal(app.constraints[0].video.deviceId, undefined);
  assert.equal(app.elements.get("camera").value, "built-in");
});

test("restarting after stopping requests the newly selected camera and needs new calibration", async () => {
  const app = harness({ devices: [
    { kind: "videoinput", deviceId: "built-in", label: "Laptop webcam" },
    { kind: "videoinput", deviceId: "usb", label: "USB webcam" }
  ] });
  await settle();
  app.elements.get("camera").value = "built-in";
  app.click("start");
  await settle();
  app.click("calibrate");
  await settle();
  app.click("stop");
  app.elements.get("camera").value = "usb";
  app.click("start");
  await settle();
  assert.equal(app.constraints[0].video.deviceId.exact, "built-in");
  assert.equal(app.constraints[1].video.deviceId.exact, "usb");
  assert.equal(app.trackers[0].disposed, true);
  assert.equal(app.trackers[1].samples, undefined);
  assert.match(app.elements.get("status").textContent, /Select Calibrate/);
});

test("device changes preserve selection and report when the selected camera disappears", async () => {
  const options = { devices: [{ kind: "videoinput", deviceId: "usb", label: "USB webcam" }] };
  const app = harness(options);
  await settle();
  app.elements.get("camera").value = "usb";
  app.click("refresh-cameras");
  await settle();
  assert.equal(app.elements.get("camera").value, "usb");
  options.devices = [];
  app.context.navigator.mediaDevices.dispatchEvent(new Event("devicechange"));
  await settle();
  assert.equal(app.elements.get("camera").value, "");
  assert.match(app.elements.get("camera-status").textContent, /no longer available/);
});

test("device enumeration errors are visible and refresh allows recovery", async () => {
  const options = { enumerationError: "Device list unavailable" };
  const app = harness(options);
  await settle();
  assert.match(app.elements.get("camera-status").textContent, /Device list unavailable/);
  options.enumerationError = null;
  options.devices = [{ kind: "videoinput", deviceId: "usb", label: "USB webcam" }];
  app.click("refresh-cameras");
  await settle();
  assert.equal(app.elements.get("camera").options.length, 2);
  assert.doesNotMatch(app.elements.get("camera-status").textContent, /Unable to list/);
});

test("an unavailable selected camera fails without retrying another camera", async () => {
  const app = harness({
    devices: [{ kind: "videoinput", deviceId: "usb", label: "USB webcam" }],
    acquire: async () => { throw new Error("Selected device unavailable"); }
  });
  await settle();
  app.elements.get("camera").value = "usb";
  app.click("start");
  await settle();
  assert.equal(app.requests(), 1);
  assert.equal(app.constraints[0].video.deviceId.exact, "usb");
  assert.match(app.elements.get("status").textContent, /Selected device unavailable/);
  assert.equal(app.elements.get("camera").disabled, false);
});

test("camera start uses only local model/WASM paths and sends permission intent", async () => {
  const app = harness();
  app.click("start");
  await settle();
  assert.equal(app.requests(), 1);
  assert.equal(app.messages[0], "request-camera:1");
  assert.equal(app.trackers[0].config.modelPath,
    "https://eye-tracking.overjoyed.invalid/models/face_landmarker.task");
  assert.equal(app.trackers[0].config.wasmPath, "https://eye-tracking.overjoyed.invalid/wasm");
  assert.equal(app.elements.get("calibrate").disabled, false);
});

test("calibration collects all 17 targets, displays gaze, and holds it on tracking loss", async () => {
  const options = { prediction: { x: 350, y: 250 } };
  const app = harness(options);
  app.click("start");
  await settle();
  app.click("calibrate");
  await settle();
  assert.equal(app.trackers[0].samples.length, 17);
  assert.equal(app.elements.get("calibration").hidden, true);
  app.frame(100);
  assert.equal(app.elements.get("gaze").hidden, false);
  assert.equal(app.elements.get("gaze").style.left, "350px");
  options.prediction = null;
  app.frame(200);
  assert.equal(app.elements.get("gaze").hidden, false);
  assert.match(app.elements.get("status").textContent, /Tracking lost/);
});

test("stopping disposes the tracker, releases capture and resets the panel", async () => {
  const app = harness();
  app.click("start");
  await settle();
  app.click("stop");
  assert.equal(app.stopped(), 1);
  assert.equal(app.trackers[0].disposed, true);
  assert.equal(app.elements.get("preview").srcObject, null);
  assert.equal(app.elements.get("start").disabled, false);
  assert.equal(app.elements.get("stop").disabled, true);
});

test("stopping while camera permission is pending releases the late stream", async () => {
  let complete;
  const pending = new Promise(resolve => { complete = resolve; });
  const app = harness({ acquire: async stream => { await pending; return stream; } });
  app.click("start");
  await settle();
  app.click("stop");
  complete();
  await settle();
  assert.equal(app.stopped(), 1);
  assert.equal(app.trackers.length, 0);
  assert.equal(app.elements.get("preview").srcObject, null);
});

test("stopping while model initialization is pending disposes the late tracker", async () => {
  let complete;
  const pending = new Promise(resolve => { complete = resolve; });
  const app = harness({ initialize: () => pending });
  app.click("start");
  await settle();
  app.click("stop");
  complete();
  await settle();
  assert.equal(app.stopped(), 1);
  assert.equal(app.trackers[0].disposed, true);
  assert.equal(app.elements.get("calibrate").disabled, true);
});

test("camera denial is an explicit visible error and permits retry", async () => {
  const app = harness({ acquire: async () => { throw new Error("Permission denied"); } });
  app.click("start");
  await settle();
  assert.equal(app.elements.get("status").dataset.error, "true");
  assert.match(app.elements.get("status").textContent, /Permission denied/);
  assert.equal(app.elements.get("start").disabled, false);
});

test("camera access waits for host authorization and can be canceled before it arrives", async () => {
  const app = harness({ ignoreAuthorization: true });
  app.click("start");
  await settle();
  assert.equal(app.requests(), 0);
  app.click("stop");
  await settle();
  assert.equal(app.requests(), 0);
  assert.equal(app.elements.get("start").disabled, false);
});

test("resize invalidates calibration and stops capture", async () => {
  const app = harness({ prediction: { x: 300, y: 200 } });
  app.click("start");
  await settle();
  app.click("calibrate");
  await settle();
  app.context.innerWidth = 900;
  app.window.dispatchEvent(new Event("resize"));
  assert.equal(app.stopped(), 1);
  assert.match(app.elements.get("status").textContent, /recalibrate/);
  assert.equal(app.elements.get("start").disabled, false);
  assert.equal(app.elements.get("gaze").hidden, true);
});

test("resize during calibration cancels it without fitting a model", async () => {
  const app = harness({ prediction: { x: 300, y: 200 } });
  app.click("start");
  await settle();
  app.click("calibrate");
  for (let i = 0; i < 20; i++) await Promise.resolve();
  app.context.innerWidth = 900;
  app.window.dispatchEvent(new Event("resize"));
  await settle();
  assert.equal(app.trackers[0].samples, undefined);
  assert.equal(app.stopped(), 1);
  assert.equal(app.elements.get("calibration").hidden, true);
});

test("hiding or unloading the document stops capture", async () => {
  for (const event of ["visibilitychange", "pagehide"]) {
    const app = harness();
    app.click("start");
    await settle();
    if (event === "visibilitychange") {
      app.document.hidden = true;
      app.document.dispatchEvent(new Event(event));
    } else {
      app.window.dispatchEvent(new Event(event));
    }
    assert.equal(app.stopped(), 1);
    assert.equal(app.elements.get("start").disabled, false);
  }
});

test("canceling calibration while the face is missing does not fit a partial model", async () => {
  const app = harness({ faceLost: true });
  app.click("start");
  await settle();
  app.click("calibrate");
  await Promise.resolve();
  app.click("cancel");
  await settle();
  assert.equal(app.stopped(), 1);
  assert.equal(app.trackers[0].samples, undefined);
  assert.equal(app.elements.get("calibration").hidden, true);
});

async function joystickHarness(options) {
  const app = await trackingHarness(options);
  app.elements.get("stabilization").value = "off";
  app.elements.get("stabilization").dispatchEvent(new Event("change"));
  return app;
}

const controlAt = app => ({
  x: parseFloat(app.elements.get("control").style.left),
  y: parseFloat(app.elements.get("control").style.top)
});

test("the joystick dot starts centred after calibration with a visible dead zone", async () => {
  const app = await joystickHarness({ prediction: { x: 400, y: 325 } });
  assert.equal(app.elements.get("control").hidden, false);
  assert.equal(app.elements.get("dead-zone-ring").hidden, false);
  assert.equal(app.elements.get("dead-zone-ring").style.width, "120px");
  assert.equal(app.elements.get("recenter").disabled, false);
  assert.deepEqual(controlAt(app), { x: 400, y: 325 });
});

test("gaze inside the dead zone keeps the dot still", async () => {
  const app = await joystickHarness({ prediction: { x: 430, y: 340 } });
  for (let t = 100; t <= 1000; t += 100) app.frame(t);
  assert.deepEqual(controlAt(app), { x: 400, y: 325 });
});

test("looking right moves the dot right and looking up moves it up", async () => {
  const options = { prediction: { x: 750, y: 325 } };
  const app = await joystickHarness(options);
  for (let t = 100; t <= 600; t += 100) app.frame(t);
  const right = controlAt(app);
  assert.ok(right.x > 400, `x: ${right.x}`);
  assert.equal(right.y, 325);
  options.prediction = { x: 400, y: 20 };
  for (let t = 700; t <= 1200; t += 100) app.frame(t);
  const up = controlAt(app);
  assert.ok(up.y < 325, `y: ${up.y}`);
  assert.ok(Math.abs(up.x - right.x) < 1);
});

test("the dot stays inside the panel and freezes on tracking loss", async () => {
  const options = { prediction: { x: 800, y: 650 } };
  const app = await joystickHarness(options);
  for (let t = 100; t <= 10000; t += 200) app.frame(t);
  assert.deepEqual(controlAt(app), { x: 787, y: 637 });
  options.prediction = { x: 0, y: 0 };
  app.frame(10100);
  app.frame(10200);
  const before = controlAt(app);
  options.prediction = null;
  for (let t = 10300; t <= 11000; t += 100) app.frame(t);
  assert.deepEqual(controlAt(app), before);
});

test("recenter, disabling, and stopping hide or reset the joystick", async () => {
  const app = await joystickHarness({ prediction: { x: 750, y: 325 } });
  for (let t = 100; t <= 600; t += 100) app.frame(t);
  app.click("recenter");
  assert.deepEqual(controlAt(app), { x: 400, y: 325 });
  app.elements.get("joystick").checked = false;
  app.elements.get("joystick").dispatchEvent(new Event("change"));
  assert.equal(app.elements.get("control").hidden, true);
  assert.equal(app.elements.get("recenter").disabled, true);
  app.elements.get("joystick").checked = true;
  app.elements.get("joystick").dispatchEvent(new Event("change"));
  assert.equal(app.elements.get("control").hidden, false);
  app.click("stop");
  assert.equal(app.elements.get("control").hidden, true);
  assert.equal(app.elements.get("dead-zone-ring").hidden, true);
});

test("the joystick stays hidden when disabled before calibration", async () => {
  const app = await joystickHarness({ prediction: { x: 750, y: 325 }, joystick: false });
  app.frame(100);
  assert.equal(app.elements.get("control").hidden, true);
});
