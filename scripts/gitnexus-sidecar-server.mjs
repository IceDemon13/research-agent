import http from "node:http";
import os from "node:os";
import { spawn } from "node:child_process";

const ENABLED = String(process.env.GITNEXUS_ENABLED || "").trim().toLowerCase() === "true";
const VERSION = String(process.env.GITNEXUS_VERSION || "latest").trim() || "latest";
const PUBLIC_PORT = Number.parseInt(process.env.GITNEXUS_PORT || "3010", 10) || 3010;
const INTERNAL_MCP_PORT = Number.parseInt(process.env.GITNEXUS_MCP_PORT_INTERNAL || String(PUBLIC_PORT + 1), 10) || (PUBLIC_PORT + 1);
const CONTROL_ENABLED = String(process.env.GITNEXUS_CONTROL_ENABLED || "true").trim().toLowerCase() !== "false";
const GITNEXUS_HOME = String(process.env.GITNEXUS_HOME || "/gitnexus").trim() || "/gitnexus";
const GITNEXUS_REPO_ROOT = String(process.env.GITNEXUS_REPO_ROOT || "/repos").trim() || "/repos";
const ANALYZE_OPTION_FLAGS = ["--force", "--skills", "--skip-embeddings"];

let backendProcess = null;
let backendExited = false;
let backendExitCode = null;
let analyzeFlagSupportPromise = null;
let cliVersionPromise = null;

function log(message, extra = "") {
  const suffix = extra ? ` ${extra}` : "";
  process.stdout.write(`[gitnexus-sidecar] ${message}${suffix}\n`);
}

function logError(message, extra = "") {
  const suffix = extra ? ` ${extra}` : "";
  process.stderr.write(`[gitnexus-sidecar] ${message}${suffix}\n`);
}

function firstNonEmptyLine(text) {
  return String(text || "")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .find(Boolean) || "";
}

function json(response, statusCode, payload) {
  response.writeHead(statusCode, {
    "Content-Type": "application/json; charset=utf-8",
    "Cache-Control": "no-store",
  });
  response.end(JSON.stringify(payload));
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
  const args = ["-y", `gitnexus@${VERSION}`, "serve", "--host", "127.0.0.1", "--port", String(INTERNAL_MCP_PORT)];
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
  } catch (error) {
    json(response, 502, {
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
    return json(response, 200, {
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
      return json(response, 503, {
        success: false,
        message: "GitNexus control API is disabled.",
      });
    }
    const bodyBuffer = await readRequestBody(request);
    let payload = {};
    try {
      payload = JSON.parse(bodyBuffer.toString("utf-8") || "{}");
    } catch {
      return json(response, 400, {
        success: false,
        message: "Invalid JSON body.",
      });
    }
    const repoPath = String(payload.repoPath || "").trim();
    if (!repoPath) {
      return json(response, 400, {
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
    return json(response, result.success ? 200 : 500, result);
  }

  if (url.pathname === "/control/status" && request.method === "GET") {
    const cliVersion = await getCliVersion();
    return json(response, 200, {
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

  const bodyBuffer = await readRequestBody(request);
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
