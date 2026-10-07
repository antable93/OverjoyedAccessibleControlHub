import { build } from "esbuild";
import { createHash } from "node:crypto";
import { copyFile, mkdir, readFile, readdir, writeFile } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = dirname(fileURLToPath(import.meta.url));
const output = join(root, "..", "Resources", "Raw", "EyeTracking");
const cache = join(root, ".downloads");
const realEyeCommit = "d94850fa01f50087d5e51d24d741bc78f61ded64";
const files = new Set();

async function copy(source, relative) {
  const destination = join(output, relative);
  await mkdir(dirname(destination), { recursive: true });
  await copyFile(source, destination);
  files.add(relative);
}

async function download(url, filename, expectedHash) {
  const destination = join(cache, filename);
  try {
    const existing = await readFile(destination);
    if (existing.length > 0) {
      if (expectedHash && createHash("sha256").update(existing).digest("hex") !== expectedHash)
        throw new Error(`Cached download checksum failed: ${destination}. Remove this file and rebuild.`);
      return destination;
    }
  } catch (error) {
    if (error.code !== "ENOENT") throw error;
  }
  console.log(`Downloading ${filename}`);
  const response = await fetch(url);
  if (!response.ok) throw new Error(`Download failed: ${url} (${response.status})`);
  const contents = Buffer.from(await response.arrayBuffer());
  if (contents.length === 0) throw new Error(`Empty download: ${url}`);
  if (expectedHash && createHash("sha256").update(contents).digest("hex") !== expectedHash)
    throw new Error(`Download checksum failed: ${url}`);
  await mkdir(dirname(destination), { recursive: true });
  await writeFile(destination, contents);
  return destination;
}

await mkdir(output, { recursive: true });
await build({
  entryPoints: [join(root, "src", "main.ts")],
  outfile: join(output, "tracker.js"),
  bundle: true,
  minify: true,
  format: "esm",
  target: "es2022",
  legalComments: "inline"
});
files.add("tracker.js");
await copy(join(root, "index.html"), "index.html");
await copy(join(root, "style.css"), "style.css");

const model = await download(
  "https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task",
  "face_landmarker.task",
  "64184e229b263107bc2b804c6625db1341ff2bb731874b0bcc2fe6544e0bc9ff"
);
await copy(model, "models/face_landmarker.task");

const wasmDirectory = join(root, "node_modules", "@mediapipe", "tasks-vision", "wasm");
for (const name of await readdir(wasmDirectory)) {
  if (name.endsWith(".js") || name.endsWith(".wasm"))
    await copy(join(wasmDirectory, name), `wasm/${name}`);
}

for (const name of ["LICENSE", "LICENSE-AGPL.md", "LICENSE-COMMERCIAL.md", "THIRD_PARTY_NOTICES.md"]) {
  const source = await download(
    `https://raw.githubusercontent.com/RealEye-io/webcam-eyetracker-light-open/${realEyeCommit}/${name}`,
    `realeye/${name}`
  );
  await copy(source, `licenses/realeye/${name}`);
}
const modelLicense = await download(
  `https://raw.githubusercontent.com/RealEye-io/webcam-eyetracker-light-open/${realEyeCommit}/public/models/README.md`,
  "model-notices.md"
);
await copy(modelLicense, "licenses/model-notices.md");

const visited = new Set();
async function copyDependencyLicenses(name) {
  if (visited.has(name)) return;
  visited.add(name);
  const directory = join(root, "node_modules", name);
  const metadata = JSON.parse(await readFile(join(directory, "package.json"), "utf8"));
  const licenseFiles = (await readdir(directory)).filter(file => /^(license|copying|notice)(\.|$)/i.test(file));
  if (name === "@mediapipe/tasks-vision") {
    const license = await download(
      "https://raw.githubusercontent.com/google-ai-edge/mediapipe/v0.10.32/LICENSE",
      "mediapipe-LICENSE"
    );
    await copy(license, `licenses/packages/${name}/LICENSE`);
  } else if (licenseFiles.length === 0 && name !== "@realeye-io/webcam-eyetracker-light-open")
    throw new Error(`No license file found for ${name}; review its distribution terms.`);
  for (const file of licenseFiles)
    await copy(join(directory, file), `licenses/packages/${name}/${file}`);
  for (const dependency of Object.keys(metadata.dependencies ?? {}))
    await copyDependencyLicenses(dependency);
}
await copyDependencyLicenses("@realeye-io/webcam-eyetracker-light-open");

const manifest = [];
for (const path of [...files].sort()) {
  const data = await readFile(join(output, path));
  manifest.push({ path, sha256: createHash("sha256").update(data).digest("hex") });
}
await writeFile(join(output, "asset-manifest.json"), JSON.stringify(manifest, null, 2));
console.log(`Bundled ${manifest.length} local eye-tracking assets.`);
