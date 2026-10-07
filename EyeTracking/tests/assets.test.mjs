import assert from "node:assert/strict";
import { test } from "node:test";
import { createHash } from "node:crypto";
import { readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { join } from "node:path";

const output = fileURLToPath(new URL("../../Resources/Raw/EyeTracking/", import.meta.url));

test("all packaged assets match the extraction manifest", async () => {
  const manifest = JSON.parse(await readFile(join(output, "asset-manifest.json"), "utf8"));
  assert.ok(manifest.length >= 20);
  const paths = manifest.map(asset => asset.path);
  assert.equal(new Set(paths).size, paths.length);
  for (const asset of manifest) {
    assert.ok(!asset.path.split("/").some(part => part === "" || part === ".." || part === "."));
    const data = await readFile(join(output, asset.path));
    assert.equal(createHash("sha256").update(data).digest("hex"), asset.sha256, asset.path);
  }
  for (const required of [
    "index.html", "tracker.js", "style.css", "models/face_landmarker.task",
    "wasm/vision_wasm_internal.js", "wasm/vision_wasm_internal.wasm",
    "wasm/vision_wasm_nosimd_internal.js", "wasm/vision_wasm_nosimd_internal.wasm",
    "licenses/realeye/LICENSE", "licenses/realeye/LICENSE-AGPL.md",
    "licenses/realeye/LICENSE-COMMERCIAL.md", "licenses/realeye/THIRD_PARTY_NOTICES.md"
  ]) assert.ok(paths.includes(required), `missing packaged asset: ${required}`);
});
