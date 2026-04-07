import http from "node:http";
import os from "node:os";
import crypto from "node:crypto";
import fs from "node:fs/promises";
import path from "node:path";
import { spawn } from "node:child_process";

const ENABLED = String(process.env.GITNEXUS_ENABLED || "").trim().toLowerCase() === "true";
const VERSION = String(process.env.GITNEXUS_VERSION || "latest").trim() || "latest";
const PUBLIC_PORT = Number.parseInt(process.env.GITNEXUS_PORT || "3010", 10) || 3010;
const INTERNAL_MCP_PORT = Number.parseInt(process.env.GITNEXUS_MCP_PORT_INTERNAL || String(PUBLIC_PORT + 1), 10) || (PUBLIC_PORT + 1);
const CONTROL_ENABLED = String(process.env.GITNEXUS_CONTROL_ENABLED || "true").trim().toLowerCase() !== "false";
const GITNEXUS_HOME = String(process.env.GITNEXUS_HOME || "/gitnexus").trim() || "/gitnexus";
const GITNEXUS_REPO_ROOT = String(process.env.GITNEXUS_REPO_ROOT || "/repos").trim() || "/repos";
const ANALYZE_OPTION_FLAGS = ["--force", "--skills", "--skip-embeddings"];
const GRAPH_CACHE_DIR = path.join(GITNEXUS_HOME, ".graph-cache");
const GRAPH_WARMUP_WAIT_MS = Number.parseInt(process.env.GITNEXUS_GRAPH_WARMUP_WAIT_MS || "2000", 10) || 2000;
const GRAPH_BUILD_TIMEOUT_MS = Number.parseInt(process.env.GITNEXUS_GRAPH_BUILD_TIMEOUT_MS || "900000", 10) || 900000;
const GRAPH_RETRY_AFTER_SECONDS = Number.parseInt(process.env.GITNEXUS_GRAPH_RETRY_AFTER_SECONDS || "2", 10) || 2;


let backendProcess = null;
let backendExited = false;
let backendExitCode = null;
let analyzeFlagSupportPromise = null;
let cliVersionPromise = null;
const graphBuilds = new Map();

function log(message, extra = "") {
  const suffix = extra ? ` ${extra}` : "";
  process.stdout.write(`[gitnexus-sidecar] ${message}${suffix}\n`);
}

function logError(message, extra = "") {
  const suffix = extra ? ` ${extra}` : "";
  process.stderr.write(`[gitnexus-sidecar] ${message}${suffix}\n`);
}

function isAllowedOrigin(origin) {
  if (!origin) {
    return true;
  }
  if (
    origin.startsWith("http://localhost:") ||
    origin === "http://localhost" ||
    origin.startsWith("http://127.0.0.1:") ||
    origin === "http://127.0.0.1" ||
    origin.startsWith("http://[::1]:") ||
    origin === "http://[::1]" ||
    origin === "https://gitnexus.vercel.app"
  ) {
    return true;
  }
  try {
    const parsed = new URL(origin);
    const hostname = parsed.hostname;
    const protocol = parsed.protocol;
    if (protocol !== "http:" && protocol !== "https:") {
      return false;
    }
    const octets = hostname.split(".").map(Number);
    if (octets.length !== 4 || octets.some((part) => !Number.isInteger(part) || part < 0 || part > 255)) {
      return false;
    }
    const [a, b] = octets;
    if (a === 10) return true;
    if (a === 172 && b >= 16 && b <= 31) return true;
    if (a === 192 && b === 168) return true;
    return false;
  } catch {
    return false;
  }
}

function firstNonEmptyLine(text) {
  return String(text || "")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .find(Boolean) || "";
}

function buildCorsHeaders(request, extraHeaders = {}) {
  const origin = String(request && request.headers && request.headers.origin || "").trim();
  const headers = {
    Vary: "Origin, Access-Control-Request-Headers",
    ...extraHeaders,
  };
  if (!origin) {
    return headers;
  }
  log("CORS request received", `origin=${origin}`);
  if (isAllowedOrigin(origin)) {
    headers["Access-Control-Allow-Origin"] = origin;
  } else {
    logError("Rejected CORS origin", `origin=${origin}`);
  }
  return headers;
}

function json(request, response, statusCode, payload, extraHeaders = {}) {
  response.writeHead(statusCode, {
    ...buildCorsHeaders(request, extraHeaders),
    "Content-Type": "application/json; charset=utf-8",
    "Cache-Control": "no-store",
  });
  response.end(JSON.stringify(payload));
}

function bodySha256(buffer) {
  try {
    return crypto.createHash("sha256").update(Buffer.from(buffer || [])).digest("hex");
  } catch {
    return "";
  }
}

function headersSummary(headers) {
  const summary = {};
  for (const [key, value] of Object.entries(headers || {})) {
    summary[String(key)] = Array.isArray(value) ? value.map((item) => String(item)) : String(value);
  }
  return summary;
}

function normalizePathForCompare(value) {
  const normalized = String(value || "").trim().replace(/\\/g, "/").replace(/\/+$/, "");
  return process.platform === "win32" ? normalized.toLowerCase() : normalized;
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function readJsonFile(filePath, fallbackValue = null) {
  try {
    const raw = await fs.readFile(filePath, "utf-8");
    return JSON.parse(raw);
  } catch {
    return fallbackValue;
  }
}

async function ensureDirectory(dirPath) {
  await fs.mkdir(dirPath, { recursive: true });
}

async function getRegistryEntries() {
  const registryPath = path.join(GITNEXUS_HOME, ".gitnexus", "registry.json");
  const entries = await readJsonFile(registryPath, []);
  return Array.isArray(entries) ? entries : [];
}

async function resolveGraphRepoEntry(requestedRepo) {
  const requested = String(requestedRepo || "").trim();
  const entries = await getRegistryEntries();
  if (!requested) {
    return entries.length === 1 ? entries[0] : null;
  }
  const requestedNormalized = normalizePathForCompare(requested);
  return entries.find((entry) => {
    const entryName = normalizePathForCompare(entry && entry.name);
    const entryPath = normalizePathForCompare(entry && entry.path);
    const entryBasename = normalizePathForCompare(path.basename(String(entry && entry.path || "")));
    return requestedNormalized === entryName || requestedNormalized === entryPath || requestedNormalized === entryBasename;
  }) || null;
}

async function readRepoMeta(repoEntry) {
  const metaPath = path.join(String(repoEntry.storagePath || ""), "meta.json");
  const meta = await readJsonFile(metaPath, null);
  return meta && typeof meta === "object" ? meta : null;
}

async function hasGraphIndex(repoEntry) {
  try {
    await fs.access(path.join(String(repoEntry.storagePath || ""), "lbug"));
    return true;
  } catch {
    return false;
  }
}

function graphCacheKey(repoEntry, includeContent) {
  return crypto
    .createHash("sha256")
    .update(`${normalizePathForCompare(repoEntry && repoEntry.path)}::${includeContent ? "content" : "summary"}`)
    .digest("hex");
}

function graphCachePaths(repoEntry, includeContent) {
  const key = graphCacheKey(repoEntry, includeContent);
  return {
    key,
    metaPath: path.join(GRAPH_CACHE_DIR, `${key}.meta.json`),
    bodyPath: path.join(GRAPH_CACHE_DIR, `${key}.json`),
  };
}

async function loadFreshGraphCache(repoEntry, includeContent) {
  const repoMeta = await readRepoMeta(repoEntry);
  if (!repoMeta) {
    return null;
  }
  const paths = graphCachePaths(repoEntry, includeContent);
  const cacheMeta = await readJsonFile(paths.metaPath, null);
  if (!cacheMeta || !cacheMeta.freshness) {
    return null;
  }
  const freshness = cacheMeta.freshness || {};
  if (
    String(freshness.lastCommit || "") !== String(repoMeta.lastCommit || "") ||
    String(freshness.indexedAt || "") !== String(repoMeta.indexedAt || "") ||
    normalizePathForCompare(freshness.repoPath) !== normalizePathForCompare(repoEntry.path) ||
    Boolean(freshness.includeContent) !== Boolean(includeContent)
  ) {
    return null;
  }
  try {
    const bodyBuffer = await fs.readFile(paths.bodyPath);
    return {
      bodyBuffer,
      cacheMeta,
      repoMeta,
    };
  } catch {
    return null;
  }
}

async function persistGraphCache(repoEntry, includeContent, responseBuffer, upstreamStatus, upstreamHeaders) {
  const repoMeta = await readRepoMeta(repoEntry);
  if (!repoMeta) {
    return null;
  }
  const paths = graphCachePaths(repoEntry, includeContent);
  await ensureDirectory(GRAPH_CACHE_DIR);
  await fs.writeFile(paths.bodyPath, responseBuffer);
  const cacheMeta = {
    cachedAt: new Date().toISOString(),
    upstreamStatus,
    upstreamHeaders,
    freshness: {
      repoPath: String(repoEntry.path || ""),
      repoName: String(repoEntry.name || ""),
      storagePath: String(repoEntry.storagePath || ""),
      includeContent: Boolean(includeContent),
      lastCommit: String(repoMeta.lastCommit || ""),
      indexedAt: String(repoMeta.indexedAt || ""),
    },
  };
  await fs.writeFile(paths.metaPath, JSON.stringify(cacheMeta, null, 2), "utf-8");
  return cacheMeta;
}

function buildGraphResponseHeaders(request, extraHeaders = {}) {
  return {
    ...buildCorsHeaders(request, extraHeaders),
    "Content-Type": "application/json; charset=utf-8",
    "Cache-Control": "no-store",
  };
}

function requestedRepoNameFromUrl(url) {
  return String(url.searchParams.get("repo") || "").trim();
}

async function buildGraphCacheInBackground(repoEntry, includeContent) {
  const buildKey = `${graphCacheKey(repoEntry, includeContent)}::${includeContent ? "content" : "summary"}`;
  if (graphBuilds.has(buildKey)) {
    return graphBuilds.get(buildKey);
  }
  const buildPromise = (async () => {
    try {
      await ensureDirectory(GRAPH_CACHE_DIR);
      const upstreamUrl = new URL(`http://127.0.0.1:${INTERNAL_MCP_PORT}/api/graph`);
      upstreamUrl.searchParams.set("repo", String(repoEntry.name || path.basename(String(repoEntry.path || ""))));
      if (includeContent) {
        upstreamUrl.searchParams.set("includeContent", "true");
      }
      log("Graph cache build started", `repo=${String(repoEntry.name || "")} includeContent=${String(includeContent)}`);
      const controller = new AbortController();
      const timer = setTimeout(() => controller.abort(), GRAPH_BUILD_TIMEOUT_MS);
      try {
        const upstreamResponse = await fetch(upstreamUrl, {
          method: "GET",
          signal: controller.signal,
          headers: {
            Accept: "application/json",
          },
        });
        const responseBuffer = Buffer.from(await upstreamResponse.arrayBuffer());
        if (!upstreamResponse.ok) {
          logError(
            "Graph cache build failed",
            `repo=${String(repoEntry.name || "")} status=${upstreamResponse.status} bodyPreview=${JSON.stringify(responseBuffer.toString("utf-8").slice(0, 240))}`,
          );
          return null;
        }
        const upstreamHeaders = {};
        upstreamResponse.headers.forEach((value, key) => {
          upstreamHeaders[key] = value;
        });
        const cacheMeta = await persistGraphCache(
          repoEntry,
          includeContent,
          responseBuffer,
          upstreamResponse.status,
          upstreamHeaders,
        );
        log(
          "Graph cache build completed",
          `repo=${String(repoEntry.name || "")} includeContent=${String(includeContent)} bytes=${responseBuffer.length}`,
        );
        return {
          cacheMeta,
          responseBuffer,
        };
      } finally {
        clearTimeout(timer);
      }
    } catch (error) {
      logError(
        "Graph cache build crashed",
        `repo=${String(repoEntry && repoEntry.name || "")} includeContent=${String(includeContent)} error=${String(error && error.message || error)}`,
      );
      return null;
    } finally {
      graphBuilds.delete(buildKey);
    }
  })();
  graphBuilds.set(buildKey, buildPromise);
  return buildPromise;
}

async function maybeWarmGraphForRepo(requestedRepo, includeContent = false) {
  const repoEntry = await resolveGraphRepoEntry(requestedRepo);
  if (!repoEntry) {
    return;
  }
  if (!(await hasGraphIndex(repoEntry))) {
    return;
  }
  const existingCache = await loadFreshGraphCache(repoEntry, includeContent);
  if (existingCache) {
    return;
  }
  void buildGraphCacheInBackground(repoEntry, includeContent);
}

async function handleGraphRequest(request, url, response) {
  const requestedRepo = requestedRepoNameFromUrl(url);
  const includeContent = String(url.searchParams.get("includeContent") || "").trim().toLowerCase() === "true";
  const repoEntry = await resolveGraphRepoEntry(requestedRepo);
  if (!repoEntry) {
    return json(request, response, 404, {
      success: false,
      error: "Repository not found",
      warmingUp: false,
    });
  }
  if (!(await hasGraphIndex(repoEntry))) {
    return json(request, response, 202, {
      success: true,
      repo: String(repoEntry.name || ""),
      warmingUp: true,
      indexed: false,
      message: "Repository index is not ready yet.",
      nodes: [],
      relationships: [],
    });
  }
  const cachedGraph = await loadFreshGraphCache(repoEntry, includeContent);
  if (cachedGraph) {
    response.writeHead(200, buildGraphResponseHeaders(request, {
      "X-GitNexus-Graph-Cache": "hit",
      "X-GitNexus-Graph-Repo": String(repoEntry.name || ""),
    }));
    response.end(cachedGraph.bodyBuffer);
    return;
  }
  const buildPromise = buildGraphCacheInBackground(repoEntry, includeContent);
  const buildResult = await Promise.race([
    buildPromise,
    sleep(GRAPH_WARMUP_WAIT_MS).then(() => null),
  ]);
  if (buildResult && buildResult.responseBuffer) {
    response.writeHead(200, buildGraphResponseHeaders(request, {
      "X-GitNexus-Graph-Cache": "fresh",
      "X-GitNexus-Graph-Repo": String(repoEntry.name || ""),
    }));
    response.end(buildResult.responseBuffer);
    return;
  }
  response.writeHead(202, buildGraphResponseHeaders(request, {
    "Retry-After": String(GRAPH_RETRY_AFTER_SECONDS),
    "X-GitNexus-Graph-Cache": "warming",
    "X-GitNexus-Graph-Repo": String(repoEntry.name || ""),
  }));
  response.end(JSON.stringify({
    success: true,
    repo: String(repoEntry.name || ""),
    warmingUp: true,
    indexed: true,
    includeContent,
    message: "Graph cache is warming up.",
    nodes: [],
    relationships: [],
  }));
}

async function proxyJsonRequest(request, response, bodyBuffer, { afterJson }) {
  const targetUrl = `http://127.0.0.1:${INTERNAL_MCP_PORT}${request.url || "/"}`;
  try {
    const proxied = await fetch(targetUrl, {
      method: request.method || "GET",
      headers: request.headers,
      body: ["GET", "HEAD"].includes(String(request.method || "GET").toUpperCase()) ? undefined : bodyBuffer,
    });
    const responseBuffer = Buffer.from(await proxied.arrayBuffer());
    const headers = {};
    proxied.headers.forEach((value, key) => {
      headers[key] = value;
    });
    response.writeHead(proxied.status, headers);
    response.end(responseBuffer);
    if (typeof afterJson === "function" && proxied.ok) {
      try {
        const parsed = JSON.parse(responseBuffer.toString("utf-8"));
        await afterJson(parsed);
      } catch {
        // Ignore non-JSON proxy bodies for side effects.
      }
    }
  } catch (error) {
    json(request, response, 502, {
      success: false,
      message: `GitNexus backend proxy failed: ${String(error && error.message || error)}`,
      backendExited,
      backendExitCode,
    });
  }
}

function buildGitNexusEnv() {
  return {
    ...process.env,
    HOME: GITNEXUS_HOME,
    GITNEXUS_HOME,
    GITNEXUS_REPO_ROOT,
  };
}

function runtimeSnapshot({ cwd = GITNEXUS_HOME } = {}) {
  let username = "";
  try {
    username = String(os.userInfo().username || "").trim();
  } catch {
    username = String(process.env.USER || process.env.USERNAME || "").trim();
  }
  return {
    processUser: username,
    home: String(buildGitNexusEnv().HOME || "").trim(),
    gitnexusHome: GITNEXUS_HOME,
    cwd: String(cwd || "").trim(),
    repoRoot: GITNEXUS_REPO_ROOT,
  };
}

function readRequestBody(request) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    request.on("data", (chunk) => chunks.push(Buffer.from(chunk)));
    request.on("end", () => resolve(Buffer.concat(chunks)));
    request.on("error", reject);
  });
}

function runNpxCommand(args, options = {}) {
  const resolvedCwd = String(options.cwd || GITNEXUS_HOME).trim() || GITNEXUS_HOME;
  return new Promise((resolve) => {
    const command = `npx ${args.join(" ")}`;
    const child = spawn("npx", args, {
      cwd: resolvedCwd,
      env: buildGitNexusEnv(),
      stdio: ["ignore", "pipe", "pipe"],
    });
    let stdout = "";
    let stderr = "";
    child.stdout.on("data", (chunk) => {
      stdout += String(chunk);
    });
    child.stderr.on("data", (chunk) => {
      stderr += String(chunk);
    });
    child.on("exit", (code) => {
      resolve({
        command,
        exitCode: typeof code === "number" ? code : -1,
        stdout: stdout.trim(),
        stderr: stderr.trim(),
        runtime: runtimeSnapshot({ cwd: resolvedCwd }),
      });
    });
  });
}

function parseSupportedAnalyzeFlags(helpText) {
  const supported = new Set();
  const normalized = String(helpText || "");
  for (const flag of ANALYZE_OPTION_FLAGS) {
    if (normalized.includes(flag)) {
      supported.add(flag);
    }
  }
  return supported;
}

async function getCliVersion() {
  if (!cliVersionPromise) {
    cliVersionPromise = runNpxCommand(["-y", `gitnexus@${VERSION}`, "--version"])
      .then((result) => firstNonEmptyLine(result.stdout) || firstNonEmptyLine(result.stderr) || VERSION)
      .catch(() => VERSION);
  }
  return cliVersionPromise;
}

async function getSupportedAnalyzeFlags() {
  if (!analyzeFlagSupportPromise) {
    analyzeFlagSupportPromise = runNpxCommand(["-y", `gitnexus@${VERSION}`, "analyze", "--help"])
      .then((result) => {
        const combinedText = `${result.stdout}\n${result.stderr}`.trim();
        const supportedFlags = parseSupportedAnalyzeFlags(combinedText);
        log(
          "Detected GitNexus analyze flags",
          `supported=${Array.from(supportedFlags).join(",") || "<none>"} command=${result.command}`,
        );
        return supportedFlags;
      })
      .catch(() => new Set());
  }
  return analyzeFlagSupportPromise;
}

function startBackend() {
  if (!ENABLED) {
    log("GitNexus sidecar disabled; backend process will not start.");
    return;
  }
  const args = ["-y", `gitnexus@${VERSION}`, "serve", "--host", "0.0.0.0", "--port", String(INTERNAL_MCP_PORT)];
  log("Starting GitNexus MCP backend", `command=npx ${args.join(" ")}`);
  backendProcess = spawn("npx", args, {
    cwd: GITNEXUS_HOME,
    env: buildGitNexusEnv(),
    stdio: ["ignore", "pipe", "pipe"],
  });
  backendProcess.stdout.on("data", (chunk) => {
    process.stdout.write(chunk);
  });
  backendProcess.stderr.on("data", (chunk) => {
    process.stderr.write(chunk);
  });
  backendProcess.on("exit", (code) => {
    backendExited = true;
    backendExitCode = typeof code === "number" ? code : -1;
    logError("GitNexus MCP backend exited", `exitCode=${backendExitCode}`);
  });
}

async function proxyToBackend(request, response, bodyBuffer) {
  const targetUrl = `http://127.0.0.1:${INTERNAL_MCP_PORT}${request.url || "/"}`;
  let parsedBody = null;
  try {
    parsedBody = JSON.parse(Buffer.from(bodyBuffer || []).toString("utf-8") || "{}");
  } catch {
    parsedBody = null;
  }
  const requestMethod = String(parsedBody && parsedBody.method || "").trim();
  const requestId = parsedBody && Object.prototype.hasOwnProperty.call(parsedBody, "id") ? parsedBody.id : "";
  const isMcpRequest = String(request.url || "").startsWith("/api/mcp");
  const isInitialize = requestMethod === "initialize";
  const bodyText = Buffer.from(bodyBuffer || []).toString("utf-8");
  const bodyPreview = bodyText.length > 240 ? `${bodyText.slice(0, 240)}...[truncated]` : bodyText;
  const originalForwardedHeaders = headersSummary(request.headers);
  const normalizedHeaders = {
    Accept: String(request.headers.accept || "application/json, text/event-stream"),
    "Content-Type": String(request.headers["content-type"] || "application/json"),
    "MCP-Protocol-Version": String(request.headers["mcp-protocol-version"] || ""),
  };
  const sessionHeader = String(request.headers["mcp-session-id"] || "");
  if (!normalizedHeaders["MCP-Protocol-Version"]) {
    delete normalizedHeaders["MCP-Protocol-Version"];
  }
  if (sessionHeader) {
    normalizedHeaders["MCP-Session-Id"] = sessionHeader;
  }
  const useNormalizedHeaders = isMcpRequest;
  const forwardedHeaders = useNormalizedHeaders ? normalizedHeaders : request.headers;
  const normalizedForwardedHeaders = headersSummary(forwardedHeaders);
  if (isMcpRequest) {
    log(
      "MCP proxy request received",
      `url=${request.url || "/"} method=${requestMethod || request.method || "unknown"} id=${String(requestId)} bodyBytes=${Buffer.byteLength(bodyBuffer || Buffer.alloc(0))}`,
    );
    log(
      "MCP proxy forwarded request summary",
      `method=${request.method || "GET"} target=${targetUrl} normalizedMode=${String(useNormalizedHeaders)} contentType=${String(request.headers["content-type"] || "")} bodySha256=${bodySha256(bodyBuffer)} bodyPreview=${JSON.stringify(bodyPreview)} originalHeaderKeys=${JSON.stringify(Object.keys(originalForwardedHeaders).sort())} normalizedHeaderKeys=${JSON.stringify(Object.keys(normalizedForwardedHeaders).sort())} headers=${JSON.stringify(normalizedForwardedHeaders)}`,
    );
  }
  try {
    if (isMcpRequest) {
      log("MCP proxy backend fetch started", `target=${targetUrl}`);
    }
    const proxied = await fetch(targetUrl, {
      method: request.method || "GET",
      headers: forwardedHeaders,
      body: ["GET", "HEAD"].includes(String(request.method || "GET").toUpperCase()) ? undefined : bodyBuffer,
    });
    if (isMcpRequest) {
      log(
        "MCP proxy backend response headers received",
        `status=${proxied.status} contentType=${proxied.headers.get("content-type") || ""} sessionId=${proxied.headers.get("MCP-Session-Id") || ""}`,
      );
    }
    const responseBuffer = Buffer.from(await proxied.arrayBuffer());
    if (isMcpRequest) {
      log("MCP proxy backend body buffered", `bytes=${responseBuffer.length}`);
    }
    const headers = {};
    proxied.headers.forEach((value, key) => {
      headers[key] = value;
    });
    if (isMcpRequest) {
      log("MCP proxy transport write started", `status=${proxied.status}`);
    }
    response.writeHead(proxied.status, headers);
    response.end(responseBuffer);
    if (isMcpRequest) {
      log("MCP proxy transport write finished", `status=${proxied.status} bytes=${responseBuffer.length}`);
    }
  } catch (error) {
    if (isMcpRequest) {
      const errorName = String(error && error.name || "");
      const errorMessage = String(error && error.message || error || "");
      const errorCauseCode = String(error && error.cause && error.cause.code || "");
      const errorCauseMessage = String(error && error.cause && error.cause.message || "");
      logError(
        "MCP backend proxy failed",
        `target=${targetUrl} errorName=${errorName} error=${errorMessage} causeCode=${errorCauseCode} causeMessage=${errorCauseMessage}`,
      );
    }
    json(request, response, 502, {
      success: false,
      message: `GitNexus backend proxy failed: ${String(error && error.message || error)}`,
      backendExited,
      backendExitCode,
    });
  }
}

async function buildAnalyzeInvocation({ repoPath, force, skipEmbeddings, useSkills }) {
  const supportedFlags = await getSupportedAnalyzeFlags();
  const args = ["-y", `gitnexus@${VERSION}`, "analyze", String(repoPath || "").trim()];
  const omittedFlags = [];
  const requestedFlags = [];

  function appendFlagIfSupported(flag, requested) {
    if (!requested) {
      return;
    }
    requestedFlags.push(flag);
    if (supportedFlags.has(flag)) {
      args.push(flag);
      return;
    }
    omittedFlags.push(flag);
    log("Omitting unsupported GitNexus analyze flag", `flag=${flag}`);
  }

  appendFlagIfSupported("--force", Boolean(force));
  appendFlagIfSupported("--skip-embeddings", Boolean(skipEmbeddings));
  appendFlagIfSupported("--skills", Boolean(useSkills));

  return {
    args,
    requestedFlags,
    omittedFlags,
    supportedFlags: Array.from(supportedFlags),
  };
}

async function runAnalyze({ repoPath, force, skipEmbeddings, useSkills }) {
  const invocation = await buildAnalyzeInvocation({ repoPath, force, skipEmbeddings, useSkills });
  const cliVersion = await getCliVersion();
  const result = await runNpxCommand(invocation.args, { cwd: GITNEXUS_HOME });
  if (result.exitCode === 0) {
    log("GitNexus analyze completed", `repoPath=${repoPath} exitCode=${result.exitCode} command=${result.command}`);
  } else {
    logError(
      "GitNexus analyze failed",
      `repoPath=${repoPath} exitCode=${result.exitCode} command=${result.command} stderr=${result.stderr}`,
    );
  }
  return {
    success: result.exitCode === 0,
    repoPath,
    command: result.command,
    exitCode: result.exitCode,
    stdout: result.stdout,
    stderr: result.stderr,
    message: result.exitCode === 0 ? "GitNexus analyze completed successfully." : (result.stderr || "GitNexus analyze failed."),
    cliVersion,
    supportedFlags: invocation.supportedFlags,
    requestedFlags: invocation.requestedFlags,
    omittedFlags: invocation.omittedFlags,
    runtime: result.runtime,
  };
}

startBackend();

const server = http.createServer(async (request, response) => {
  const url = new URL(request.url || "/", `http://${request.headers.host || `127.0.0.1:${PUBLIC_PORT}`}`);
  if (url.pathname === "/control/health" && request.method === "GET") {
    const cliVersion = await getCliVersion();
    return json(request, response, 200, {
      success: true,
      enabled: ENABLED,
      controlEnabled: CONTROL_ENABLED,
      service: "gitnexus-sidecar",
      version: VERSION,
      cliVersion,
      backendExited,
      backendExitCode,
      mcpBaseUrl: `http://127.0.0.1:${INTERNAL_MCP_PORT}`,
      serviceRuntime: runtimeSnapshot({ cwd: process.cwd() }),
      analyzeRuntime: runtimeSnapshot({ cwd: GITNEXUS_HOME }),
      backendRuntime: runtimeSnapshot({ cwd: GITNEXUS_HOME }),
    });
  }

  if (url.pathname === "/control/analyze" && request.method === "POST") {
    if (!ENABLED || !CONTROL_ENABLED) {
      return json(request, response, 503, {
        success: false,
        message: "GitNexus control API is disabled.",
      });
    }
    const bodyBuffer = await readRequestBody(request);
    let payload = {};
    try {
      payload = JSON.parse(bodyBuffer.toString("utf-8") || "{}");
    } catch {
      return json(request, response, 400, {
        success: false,
        message: "Invalid JSON body.",
      });
    }
    const repoPath = String(payload.repoPath || "").trim();
    if (!repoPath) {
      return json(request, response, 400, {
        success: false,
        message: "repoPath is required.",
      });
    }
    const result = await runAnalyze({
      repoPath,
      force: Boolean(payload.force),
      skipEmbeddings: Boolean(payload.skipEmbeddings),
      useSkills: Boolean(payload.useSkills),
    });
    return json(request, response, result.success ? 200 : 500, result);
  }

  if (url.pathname === "/control/status" && request.method === "GET") {
    const cliVersion = await getCliVersion();
    return json(request, response, 200, {
      success: true,
      repoPath: String(url.searchParams.get("repoPath") || "").trim(),
      backendExited,
      backendExitCode,
      version: VERSION,
      cliVersion,
      serviceRuntime: runtimeSnapshot({ cwd: process.cwd() }),
      analyzeRuntime: runtimeSnapshot({ cwd: GITNEXUS_HOME }),
      backendRuntime: runtimeSnapshot({ cwd: GITNEXUS_HOME }),
    });
  }

  if (url.pathname === "/api/graph" && request.method === "GET") {
    return handleGraphRequest(request, url, response);
  }

  if (request.method === "OPTIONS") {
    const origin = String(request.headers.origin || "").trim();
    if (origin) {
      log("CORS preflight received", `path=${url.pathname} origin=${origin}`);
    }
    response.writeHead(204, buildCorsHeaders(request, {
      "Access-Control-Allow-Methods": "GET, HEAD, POST, OPTIONS",
      "Access-Control-Allow-Headers": String(request.headers["access-control-request-headers"] || "Content-Type"),
      "Access-Control-Max-Age": "600",
    }));
    response.end();
    return;
  }

  if (url.pathname === "/api/repo" && request.method === "GET") {
    const bodyBuffer = await readRequestBody(request);
    return proxyJsonRequest(request, response, bodyBuffer, {
      afterJson: async () => {
        await maybeWarmGraphForRepo(requestedRepoNameFromUrl(url), false);
      },
    });
  }

  const bodyBuffer = await readRequestBody(request);
  if (url.pathname === "/api/repos" && request.method === "GET") {
    return proxyJsonRequest(request, response, bodyBuffer, {
      afterJson: async (payload) => {
        if (!Array.isArray(payload)) {
          return;
        }
        for (const repoEntry of payload.slice(0, 3)) {
          await maybeWarmGraphForRepo(String(repoEntry && (repoEntry.name || repoEntry.repoPath || repoEntry.path) || ""), false);
        }
      },
    });
  }
  return proxyToBackend(request, response, bodyBuffer);
});

server.listen(PUBLIC_PORT, "0.0.0.0", () => {
  log("GitNexus sidecar wrapper listening", `port=${PUBLIC_PORT}`);
});

process.on("SIGTERM", () => {
  if (backendProcess) {
    backendProcess.kill("SIGTERM");
  }
  server.close(() => process.exit(0));
});
