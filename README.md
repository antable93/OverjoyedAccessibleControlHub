# OverjoyedAccessibleControlHub

## Eye Tracking (Windows)

Open Settings and select **Eye Tracking**, directly below **Devices**. The panel
hosts a locally bundled RealEye webcam tracker in WebView2.

1. Select **Grant camera access** to reveal available webcams. This briefly opens
   the default camera, refreshes the device list, and stops capture without loading
   the tracking model. Windows still needs to allow desktop-app camera access.
2. Choose a **Camera**, then select **Start Camera**. Camera access is never requested
   on panel load. **Refresh cameras** updates the list.
3. Select **Calibrate** and look at each of the 17 targets until it moves.
4. The green ring displays estimated gaze within the panel. Keep your head steady.
5. Select **Stop Camera**, press Escape, leave the panel, or close Settings to stop
   capture. Changing the panel size cancels calibration; select **Calibrate** again.

**Reload tracker** resets the panel and returns to it after viewing a license.
The tracking page uses the available panel height without scrolling; its camera
preview shrinks to fit. **Help and licenses** opens instructions and attribution
in a separate dialog. The entire green circle, including its border, is clamped
inside the visible viewport; displayed coordinates match that clamped position.
To switch webcams, select **Stop Camera**, choose another camera, then start and
calibrate again. Camera selection is disabled while capture is starting or running.
If the selected camera cannot be opened, an error is shown instead of silently
using a different device.

**System default camera** is a placeholder, not a detected webcam. Before access
is granted, WebView2 may expose only one anonymous camera and hide the other devices.
Camera permission is saved for the app's trusted local eye-tracking origin, not
for arbitrary web pages, so the device list remains available after stopping
capture or reopening the panel. This permission covers webcams exposed by WebView2;
it does not open every camera. For OBS, start **OBS Virtual Camera**, grant access
in the panel, and refresh the list. If OBS remains absent from a named device list,
WebView2/driver enumeration requires further investigation.
If granting access reports a default-camera timeout, the list is still refreshed:
permission may have succeeded even though that camera could not produce video.
Select a different named camera, such as **OBS Virtual Camera**, and start it.

This is a tracking/calibration panel only: it does not move the OS cursor or
activate controller/dial bindings. Other platforms show an unsupported message.
Camera errors appear in the panel; check Windows camera privacy settings if access
is denied. CPU processing is the compatibility default; GPU is optional.

### Gaze stabilization

The green circle uses an original adaptive filter with **Balanced** as the default.
Choose **Off** for raw gaze, **Light** for less lag, or **Strong** for more stability.
Each preset sets an initial **Stability radius**, adjustable from 0 to 60 CSS pixels.
Small fluctuations inside that radius hold the circle still. A three-sample median
rejects isolated spikes, and time-based smoothing responds faster to larger sustained
gaze shifts. Slow intentional movement still leaves the hold region.

You can adjust strength and radius while tracking; changing either resets the filter
without hiding the circle. Missed predictions leave the circle at its last known
position indefinitely, with a tracking-lost status message. That held position is
not a live gaze estimate. Brief dropouts preserve stabilization history; a gap
between valid predictions over 500 ms reseeds the filter at the reacquired position.
The circle appears only after the first valid prediction and hides when stopping
or recalibrating. Stabilization controls are disabled during calibration and do not
alter calibration samples.

Stronger filtering adds response delay, and the hold radius can leave a small
position offset; reduce the radius if selecting nearby positions feels sticky.
Filtering reduces visible jitter, not calibration error. It affects only the
indicator and displayed coordinates, not OS mouse movement or click behavior.

### Gaze joystick

After calibration, a blue dot appears in the middle of the panel. The stabilized
gaze steers it like a joystick: looking away from the centre moves the dot in that
direction, and looking further out moves it faster, up to the Speed setting
(50–800 px/s). Gaze inside the dashed Dead zone ellipse (5–40% of the panel)
leaves the dot still, and so does tracking loss. The dot stays inside the panel.
Recenter returns it to the middle; Enabled hides or shows it. It is a visual demo
only and does not move the OS mouse.
This is not SteadyMouse's implementation or a medically validated tremor treatment.

### Building the local frontend

Windows builds require Node.js 24 or later and npm on PATH. The MAUI build installs
frontend dependencies when absent, type-checks/bundles the frontend, and packages
its generated assets. Initial installation/build requires internet access to
download dependencies, the versioned MediaPipe model, and license notices.
Subsequent builds reuse the downloads; the installed tracker runs offline.

For frontend-only validation, from the repository root:

```powershell
npm.cmd --prefix EyeTracking ci
npm.cmd --prefix EyeTracking run build
npm.cmd --prefix EyeTracking test
```

Frontend source and the lockfile live in `EyeTracking`. Generated web assets in
`Resources\Raw\EyeTracking`, downloaded build inputs, and `node_modules` are ignored.
The app extracts the packaged assets into a checksum-verified, versioned cache
directory and serves them through WebView2's local HTTPS virtual-host mapping.
Webcam images and gaze processing stay local; microphone access and external
navigation are disabled.

### Licensing

The hub's original code is MIT-licensed. RealEye Webcam EyeTracker Light Open
1.1.0 is separately **AGPL-3.0-or-later OR RealEye Commercial**; it is not
relicensed under MIT. Review the [upstream license options](https://github.com/RealEye-io/webcam-eyetracker-light-open/blob/d94850fa01f50087d5e51d24d741bc78f61ded64/LICENSE)
and choose a compliant distribution path before distributing an integrated build.
Using an embedded browser does not automatically exempt an integration from
copyleft obligations. The commercial terms include eligibility-dependent no-fee
use; do not assume eligibility.

The frontend bundles the license texts and third-party/model notices, accessible
under **Licenses and attribution**. MediaPipe has its own Apache-2.0 license.
