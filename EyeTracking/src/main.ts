import {
  WebcamETLight, getRecommendedPattern, getCalibrationPointsInPixels,
  type CalibrationSample
} from "@realeye-io/webcam-eyetracker-light-open";
import {
  GazeStabilizer, STABILIZATION_PRESETS, isStabilizationMode, clampToViewport, viewportChanged, type Position
} from "./gaze.ts";
import { joystickVelocity, stepPosition } from "./joystick.ts";

function element<T extends HTMLElement>(id: string, type: { new(): T }): T {
  const value = document.getElementById(id);
  if (!(value instanceof type)) throw new Error(`Missing element: ${id}`);
  return value;
}

const startButton = element("start", HTMLButtonElement);
const stopButton = element("stop", HTMLButtonElement);
const calibrateButton = element("calibrate", HTMLButtonElement);
const delegate = element("delegate", HTMLSelectElement);
const camera = element("camera", HTMLSelectElement);
const grantCameraButton = element("grant-camera", HTMLButtonElement);
const refreshCamerasButton = element("refresh-cameras", HTMLButtonElement);
const cameraStatus = element("camera-status", HTMLParagraphElement);
const video = element("preview", HTMLVideoElement);
const status = element("status", HTMLParagraphElement);
const coordinates = element("coordinates", HTMLParagraphElement);
const stabilization = element("stabilization", HTMLSelectElement);
const stabilityRadius = element("stability-radius", HTMLInputElement);
const stabilityRadiusValue = element("stability-radius-value", HTMLOutputElement);
const gazeSpeed = element("gaze-speed", HTMLInputElement);
const gazeSpeedValue = element("gaze-speed-value", HTMLOutputElement);
const gazeFilter = new GazeStabilizer();
const calibration = element("calibration", HTMLDivElement);
const calibrationStatus = element("calibration-status", HTMLParagraphElement);
const target = element("target", HTMLDivElement);
const gaze = element("gaze", HTMLDivElement);
const joystickEnabled = element("joystick", HTMLInputElement);
const joystickSpeed = element("joystick-speed", HTMLInputElement);
const joystickSpeedValue = element("joystick-speed-value", HTMLOutputElement);
const deadZone = element("dead-zone", HTMLInputElement);
const deadZoneValue = element("dead-zone-value", HTMLOutputElement);
const recenterButton = element("recenter", HTMLButtonElement);
const control = element("control", HTMLDivElement);
const deadZoneRing = element("dead-zone-ring", HTMLDivElement);
const canvas = document.createElement("canvas");
function frameContext(): CanvasRenderingContext2D {
  const value = canvas.getContext("2d", { willReadFrequently: true });
  if (!value) throw new Error("Webcam frame capture is unavailable.");
  return value;
}
const context = frameContext();

declare global {
  interface Window {
    chrome?: { webview?: {
      postMessage(message: string): void;
      addEventListener(type: "message", listener: (event: MessageEvent) => void): void;
      removeEventListener(type: "message", listener: (event: MessageEvent) => void): void;
    } };
  }
}

let tracker: WebcamETLight | null = null;
let stream: MediaStream | null = null;
let session = 0;
let animation = 0;
let calibrating = false;
let calibratedViewport: Position | null = null;
let lastFrame = 0;
let starting = false;
let cancelAuthorization: (() => void) | null = null;
let cameraListRequest = 0;
let controlPosition: Position = { x: 0, y: 0 };
let controlVelocity: Position = { x: 0, y: 0 };
let lastControlStep = 0;

function hideJoystick(): void {
  control.hidden = true;
  deadZoneRing.hidden = true;
  controlVelocity = { x: 0, y: 0 };
  lastControlStep = 0;
}

function placeControl(position: Position): void {
  const bounds = control.getBoundingClientRect();
  controlPosition = clampToViewport(position, { x: innerWidth, y: innerHeight }, { x: bounds.width, y: bounds.height });
  control.style.left = `${controlPosition.x}px`;
  control.style.top = `${controlPosition.y}px`;
}

function recenterControl(): void {
  controlVelocity = { x: 0, y: 0 };
  placeControl({ x: innerWidth / 2, y: innerHeight / 2 });
}

function showJoystick(): void {
  if (!joystickEnabled.checked || !calibratedViewport || calibrating) {
    hideJoystick();
    return;
  }
  const fraction = Number(deadZone.value) / 100;
  deadZoneRing.style.left = `${innerWidth / 2}px`;
  deadZoneRing.style.top = `${innerHeight / 2}px`;
  deadZoneRing.style.width = `${innerWidth * fraction}px`;
  deadZoneRing.style.height = `${innerHeight * fraction}px`;
  deadZoneRing.hidden = false;
  if (control.hidden) {
    control.hidden = false;
    recenterControl();
  }
}

function updateJoystickSettings(): void {
  joystickSpeedValue.textContent = `${joystickSpeed.value} px/s`;
  deadZoneValue.textContent = `${deadZone.value}%`;
  showJoystick();
}

function stepJoystick(timestamp: number): void {
  if (control.hidden) return;
  const seconds = lastControlStep === 0 ? 0 : Math.min(0.25, (timestamp - lastControlStep) / 1000);
  lastControlStep = timestamp;
  const bounds = control.getBoundingClientRect();
  controlPosition = stepPosition(controlPosition, controlVelocity, seconds,
    { x: innerWidth, y: innerHeight }, { x: bounds.width, y: bounds.height });
  control.style.left = `${controlPosition.x}px`;
  control.style.top = `${controlPosition.y}px`;
}

async function refreshCameras(): Promise<void> {
  const request = ++cameraListRequest;
  try {
    if (!navigator.mediaDevices?.enumerateDevices)
      throw new Error("This browser cannot list cameras.");
    const devices = (await navigator.mediaDevices.enumerateDevices())
      .filter(device => device.kind === "videoinput" && device.deviceId !== "");
    if (request !== cameraListRequest) return;

    const selected = camera.value;
    const options = [new Option("System default camera", "")];
    devices.forEach((device, index) =>
      options.push(new Option(device.label || `Camera ${index + 1}`, device.deviceId)));
    camera.replaceChildren(...options);
    const selectionMissing = selected !== "" && !devices.some(device => device.deviceId === selected);
    camera.value = selectionMissing ? "" : selected;
    cameraStatus.textContent = selectionMissing
      ? "The selected camera is no longer available. Choose a camera before restarting."
      : devices.length === 0 || devices.every(device => !device.label)
        ? "Camera access has not exposed the full device list. Select Grant camera access, then choose your webcam."
        : `${devices.length} camera(s) available. Stop the camera before changing it.`;
  } catch (error) {
    if (request !== cameraListRequest) return;
    console.error("Unable to list cameras:", error);
    cameraStatus.textContent = `Unable to list cameras: ${error instanceof Error ? error.message : String(error)}. Select Refresh cameras to retry.`;
  }
}

function authorizeCamera(currentSession: number): Promise<void> {
  const bridge = window.chrome?.webview;
  if (!bridge) return Promise.resolve();
  return new Promise((resolve, reject) => {
    const expected = `camera-authorized:${currentSession}`;
    const cleanup = (): void => {
      bridge.removeEventListener("message", onMessage);
      clearTimeout(timeout);
      cancelAuthorization = null;
    };
    const onMessage = (event: MessageEvent): void => {
      if (event.data !== expected) return;
      cleanup();
      resolve();
    };
    const timeout = setTimeout(() => {
      cleanup();
      reject(new Error("The app did not authorize the camera request."));
    }, 10000);
    cancelAuthorization = () => {
      cleanup();
      reject(new Error("Camera request canceled."));
    };
    bridge.addEventListener("message", onMessage);
    bridge.postMessage(`request-camera:${currentSession}`);
  });
}

function setStatus(message: string, error = false): void {
  if (status.textContent !== message) status.textContent = message;
  if (status.dataset.error !== String(error)) status.dataset.error = String(error);
}

function updateControls(): void {
  startButton.disabled = starting || stream !== null;
  delegate.disabled = startButton.disabled;
  camera.disabled = startButton.disabled;
  grantCameraButton.disabled = startButton.disabled;
  refreshCamerasButton.disabled = startButton.disabled;
  stopButton.disabled = !starting && stream === null;
  calibrateButton.disabled = starting || tracker === null || calibrating;
  stabilization.disabled = calibrating;
  stabilityRadius.disabled = calibrating || stabilization.value === "off";
  gazeSpeed.disabled = stabilityRadius.disabled;
  recenterButton.disabled = control.hidden;
}

function updateSpeed(): void {
  try {
    gazeFilter.setSpeed(Number(gazeSpeed.value) / 100);
    gazeSpeedValue.textContent = `${gazeSpeed.value}%`;
  } catch (error) {
    fail(error);
  }
}

function updateStabilization(usePresetRadius: boolean): void {
  try {
    const mode = stabilization.value;
    if (!isStabilizationMode(mode)) throw new Error("Unknown gaze stabilization mode.");
    if (usePresetRadius) stabilityRadius.value = String(STABILIZATION_PRESETS[mode].radius);
    gazeFilter.configure(mode, Number(stabilityRadius.value));
    stabilityRadiusValue.textContent = `${stabilityRadius.value} px`;
    updateControls();
  } catch (error) {
    fail(error);
  }
}

function stopCamera(message = "Camera stopped. Start the camera and calibrate to resume."): void {
  session++;
  cancelAuthorization?.();
  window.chrome?.webview?.postMessage("camera-stopped");
  cancelAnimationFrame(animation);
  stream?.getTracks().forEach(track => track.stop());
  stream = null;
  video.srcObject = null;
  tracker?.dispose();
  tracker = null;
  starting = false;
  calibrating = false;
  calibration.hidden = true;
  gaze.hidden = true;
  gazeFilter.reset();
  calibratedViewport = null;
  hideJoystick();
  coordinates.textContent = "No gaze prediction.";
  updateControls();
  setStatus(message);
}

function fail(error: unknown): void {
  const message = error instanceof Error ? error.message : String(error);
  console.error("Eye tracking failed:", error);
  stopCamera();
  setStatus(`Eye tracking error: ${message}. Check camera permissions or try CPU processing.`, true);
}

function captureFrame(): ImageData {
  if (!video.videoWidth || !video.videoHeight) throw new Error("The camera has not produced a frame.");
  canvas.width = 640;
  canvas.height = Math.round(video.videoHeight * canvas.width / video.videoWidth);
  context.drawImage(video, 0, 0, canvas.width, canvas.height);
  return context.getImageData(0, 0, canvas.width, canvas.height);
}

async function startCamera(): Promise<void> {
  if (starting || stream !== null) return;
  starting = true;
  const currentSession = ++session;
  const selectedCamera = camera.value;
  updateControls();
  setStatus("Requesting camera access...");
  let initializing: WebcamETLight | null = null;
  try {
    if (!navigator.mediaDevices?.getUserMedia)
      throw new Error("Camera access requires a secure WebView2 context.");
    await authorizeCamera(currentSession);
    if (session !== currentSession) return;
    const acquired = await navigator.mediaDevices.getUserMedia({
      video: {
        ...(selectedCamera ? { deviceId: { exact: selectedCamera } } : {}),
        width: { ideal: 640 }, height: { ideal: 480 }, frameRate: { ideal: 15 }
      },
      audio: false
    });
    if (session !== currentSession) {
      acquired.getTracks().forEach(track => track.stop());
      return;
    }
    stream = acquired;
    for (const track of stream.getVideoTracks())
      track.addEventListener("ended", () => {
        if (session === currentSession) fail(new Error("The camera was disconnected."));
      });
    video.srcObject = stream;
    await video.play();
    if (session !== currentSession) return;
    const actualDevice = stream.getVideoTracks()[0]?.getSettings().deviceId;
    await refreshCameras();
    if (session !== currentSession) return;
    if (actualDevice && Array.from(camera.options).some(option => option.value === actualDevice))
      camera.value = actualDevice;
    setStatus("Loading the bundled eye-tracking model...");
    initializing = new WebcamETLight({
      delegate: delegate.value === "GPU" ? "GPU" : "CPU",
      faceDetectorMode: "landmarker",
      modelPath: new URL("models/face_landmarker.task", document.baseURI).href,
      wasmPath: new URL("wasm", document.baseURI).href,
      runningMode: "VIDEO"
    });
    await initializing.initialize();
    if (session !== currentSession) {
      initializing.dispose();
      return;
    }
    tracker = initializing;
    initializing = null;
    starting = false;
    updateControls();
    setStatus("Camera ready. Select Calibrate, then look at each target.");
  } catch (error) {
    initializing?.dispose();
    if (session === currentSession) fail(error);
  }
}

async function grantCameraAccess(): Promise<void> {
  if (starting || stream !== null) return;
  starting = true;
  const currentSession = ++session;
  updateControls();
  setStatus("Granting camera access. The default camera will open briefly to reveal the device list...");
  try {
    if (!navigator.mediaDevices?.getUserMedia)
      throw new Error("Camera access requires a secure WebView2 context.");
    await authorizeCamera(currentSession);
    if (session !== currentSession) return;
    const acquired = await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
    if (session !== currentSession) {
      acquired.getTracks().forEach(track => track.stop());
      return;
    }
    stream = acquired;
    await refreshCameras();
    if (session !== currentSession) return;
    stopCamera("Camera access granted; capture is off. Choose a camera, then select Start Camera.");
  } catch (error) {
    if (session === currentSession) {
      fail(error);
      // Permission can be granted even when the default camera fails to produce video.
      await refreshCameras();
    }
  }
}

function delay(milliseconds: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, milliseconds));
}

async function calibrate(): Promise<void> {
  const activeTracker = tracker;
  if (!activeTracker || calibrating) return;
  const currentSession = session;
  const viewport = { x: innerWidth, y: innerHeight };
  const points = getCalibrationPointsInPixels(getRecommendedPattern(), viewport.x, viewport.y);
  const samples: CalibrationSample[] = [];
  calibrating = true;
  cancelAnimationFrame(animation);
  gaze.hidden = true;
  gazeFilter.reset();
  hideJoystick();
  calibration.hidden = false;
  updateControls();
  try {
    for (const [index, point] of points.entries()) {
      target.style.left = `${point.x}px`;
      target.style.top = `${point.y}px`;
      calibrationStatus.textContent = `Look at target ${index + 1} of ${points.length}.`;
      await delay(1400);
      if (session !== currentSession) return;
      let frame = captureFrame();
      while (activeTracker.detectFace(frame) === null) {
        calibrationStatus.textContent = `Target ${index + 1}: face not detected. Face the camera and keep looking at the target.`;
        await delay(300);
        if (session !== currentSession) return;
        frame = captureFrame();
      }
      samples.push({ image: frame, gazeX: point.x, gazeY: point.y });
    }
    calibrationStatus.textContent = "Fitting calibration...";
    await delay(50);
    if (session !== currentSession) return;
    activeTracker.calibrate(samples);
    calibratedViewport = viewport;
    calibrating = false;
    calibration.hidden = true;
    showJoystick();
    updateControls();
    setStatus("Tracking. The green ring shows your estimated gaze within this panel.");
    lastFrame = 0;
    animation = requestAnimationFrame(predict);
  } catch (error) {
    if (session === currentSession) fail(error);
  }
}

function predict(timestamp: number): void {
  if (!tracker || !calibratedViewport || calibrating) return;
  try {
    if (timestamp - lastFrame >= 1000 / 15) {
      lastFrame = timestamp;
      const prediction = tracker.predict(captureFrame());
      if (prediction === null) {
        controlVelocity = { x: 0, y: 0 };
        setStatus(gaze.hidden
          ? "Tracking lost. Face the camera to resume."
          : "Tracking lost. The green ring is holding its last known position. Face the camera to resume.");
      } else {
        const filtered = gazeFilter.update(prediction, timestamp);
        gaze.hidden = false;
        const bounds = gaze.getBoundingClientRect();
        const displayed = clampToViewport(filtered, { x: innerWidth, y: innerHeight },
          { x: bounds.width, y: bounds.height });
        gaze.style.left = `${displayed.x}px`;
        gaze.style.top = `${displayed.y}px`;
        if (!control.hidden)
          controlVelocity = joystickVelocity(displayed, { x: innerWidth, y: innerHeight },
            Number(deadZone.value) / 100, Number(joystickSpeed.value));
        coordinates.textContent = `Gaze: ${Math.round(displayed.x)}, ${Math.round(displayed.y)} CSS pixels.`;
        setStatus("Tracking. The green ring shows your estimated gaze within this panel.");
      }
    }
    stepJoystick(timestamp);
    animation = requestAnimationFrame(predict);
  } catch (error) {
    fail(error);
  }
}

startButton.addEventListener("click", () => void startCamera());
const help = element("help", HTMLDialogElement);
element("show-help", HTMLButtonElement).addEventListener("click", () => help.showModal());
element("close-help", HTMLButtonElement).addEventListener("click", () => help.close());
stabilization.addEventListener("change", () => updateStabilization(true));
stabilityRadius.addEventListener("input", () => updateStabilization(false));
gazeSpeed.addEventListener("input", updateSpeed);
updateSpeed();
joystickEnabled.addEventListener("change", () => { updateJoystickSettings(); updateControls(); });
joystickSpeed.addEventListener("input", updateJoystickSettings);
deadZone.addEventListener("input", updateJoystickSettings);
recenterButton.addEventListener("click", recenterControl);
updateJoystickSettings();
updateControls();
grantCameraButton.addEventListener("click", () => void grantCameraAccess());
refreshCamerasButton.addEventListener("click", () => void refreshCameras());
navigator.mediaDevices?.addEventListener("devicechange", () => void refreshCameras());
void refreshCameras();
calibrateButton.addEventListener("click", () => void calibrate());
stopButton.addEventListener("click", () => stopCamera());
element("cancel", HTMLButtonElement).addEventListener("click", () => stopCamera());
document.addEventListener("keydown", event => {
  if (event.key === "Escape") stopCamera();
});
document.addEventListener("visibilitychange", () => {
  if (document.hidden) stopCamera();
});
window.addEventListener("pagehide", () => stopCamera());
window.addEventListener("resize", () => {
  if (calibrating || (calibratedViewport &&
      viewportChanged(calibratedViewport, { x: innerWidth, y: innerHeight })))
    stopCamera("Panel size changed. Start the camera and recalibrate.");
});
