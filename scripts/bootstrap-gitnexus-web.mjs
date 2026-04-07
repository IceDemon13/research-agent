import { execSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';

const repoRoot = '/opt/GitNexus';
const repoUrl = 'https://github.com/abhigyanpatwari/GitNexus.git';

function run(command, cwd = '/opt') {
  execSync(command, {
    cwd,
    stdio: 'inherit',
    env: process.env,
  });
}

function replaceOrThrow(filePath, searchValue, replaceValue) {
  const original = fs.readFileSync(filePath, 'utf8');
  if (original.includes(replaceValue)) {
    return;
  }
  if (!original.includes(searchValue)) {
    console.warn(`[bootstrap-gitnexus-web] Skipping patch because snippet was not found in ${filePath}`);
    return;
  }
  const updated = original.replace(searchValue, replaceValue);
  if (updated !== original) {
    fs.writeFileSync(filePath, updated, 'utf8');
  }
}

if (!fs.existsSync(path.join(repoRoot, '.git'))) {
  run(`git clone ${repoUrl} ${repoRoot}`);
}

const backendUrlSnippet = `const resolveDefaultBackendUrl = (): string => {
  const configured = (import.meta.env.VITE_GITNEXUS_BACKEND_URL as string | undefined)?.trim();
  if (configured) {
    return configured.replace(/\\/$/, '');
  }
  return \`\${window.location.protocol}//\${window.location.hostname}:3010\`;
};

export const DEFAULT_BACKEND_URL = resolveDefaultBackendUrl();`;

replaceOrThrow(
  path.join(repoRoot, 'gitnexus-web/src/config/ui-constants.ts'),
  `export const DEFAULT_BACKEND_URL = 'http://localhost:4747';`,
  backendUrlSnippet,
);

replaceOrThrow(
  path.join(repoRoot, 'gitnexus-web/src/services/backend-client.ts'),
  `let _backendUrl = 'http://localhost:4747';`,
  `let _backendUrl = DEFAULT_BACKEND_URL;`,
);

replaceOrThrow(
  path.join(repoRoot, 'gitnexus-web/src/services/backend-client.ts'),
  `import type { GraphNode, GraphRelationship } from 'gitnexus-shared';`,
  `import type { GraphNode, GraphRelationship } from 'gitnexus-shared';\nimport { DEFAULT_BACKEND_URL } from '../config/ui-constants';`,
);

replaceOrThrow(
  path.join(repoRoot, 'gitnexus-web/src/services/backend-client.ts'),
  `const PROBE_TIMEOUT_MS = 2_000;`,
  `const PROBE_TIMEOUT_MS = 2_000;\nconst GRAPH_WARMUP_TIMEOUT_MS = 120_000;\n\nconst waitWithSignal = (delayMs: number, signal?: AbortSignal): Promise<void> => {\n  if (!delayMs || delayMs <= 0) {\n    return Promise.resolve();\n  }\n  return new Promise((resolve, reject) => {\n    const timer = setTimeout(() => {\n      signal?.removeEventListener('abort', onAbort);\n      resolve();\n    }, delayMs);\n    const onAbort = () => {\n      clearTimeout(timer);\n      reject(new BackendError('Request aborted', 0, 'network'));\n    };\n    if (signal) {\n      if (signal.aborted) {\n        onAbort();\n        return;\n      }\n      signal.addEventListener('abort', onAbort, { once: true });\n    }\n  });\n};`,
);

replaceOrThrow(
  path.join(repoRoot, 'gitnexus-web/src/services/backend-client.ts'),
  `  const response = await fetchWithTimeout(url, { signal: opts?.signal }, 60_000);\n  await assertOk(response);\n\n  if (!opts?.onProgress || !response.body) {\n    return response.json() as Promise<{ nodes: GraphNode[]; relationships: GraphRelationship[] }>;\n  }`,
  `  const startedAt = Date.now();\n  let response: Response;\n\n  while (true) {\n    response = await fetchWithTimeout(url, { signal: opts?.signal }, 60_000);\n    if (response.status !== 202) {\n      break;\n    }\n\n    const warmingPayload = await response.json().catch(() => null) as\n      | { message?: string; progressPercent?: number }\n      | null;\n    const retryAfterHeader = response.headers.get('Retry-After');\n    const retryDelayMs = Math.max(500, (Number.parseInt(retryAfterHeader || '2', 10) || 2) * 1000);\n    opts?.onProgress?.(warmingPayload?.progressPercent ?? 0, null);\n\n    if (Date.now() - startedAt >= GRAPH_WARMUP_TIMEOUT_MS) {\n      throw new BackendError(\n        warmingPayload?.message || 'Graph is still warming up',\n        response.status,\n        'timeout',\n      );\n    }\n\n    await waitWithSignal(retryDelayMs, opts?.signal);\n  }\n\n  await assertOk(response);\n\n  if (!opts?.onProgress || !response.body) {\n    return response.json() as Promise<{ nodes: GraphNode[]; relationships: GraphRelationship[] }>;\n  }`,
);

replaceOrThrow(
  path.join(repoRoot, 'gitnexus-web/src/App.tsx'),
  `import { ERROR_RESET_DELAY_MS } from './config/ui-constants';`,
  `import { DEFAULT_BACKEND_URL, ERROR_RESET_DELAY_MS } from './config/ui-constants';`,
);

replaceOrThrow(
  path.join(repoRoot, 'gitnexus-web/src/App.tsx'),
  `const url = serverBaseUrl ?? 'http://localhost:4747';`,
  `const url = serverBaseUrl ?? DEFAULT_BACKEND_URL;`,
);

replaceOrThrow(
  path.join(repoRoot, 'gitnexus-web/src/components/SettingsPanel.tsx'),
  `placeholder="http://localhost:4747"`,
  `placeholder={DEFAULT_BACKEND_URL}`,
);

replaceOrThrow(
  path.join(repoRoot, 'gitnexus-web/src/components/SettingsPanel.tsx'),
  `import {`,
  `import { DEFAULT_BACKEND_URL } from '../config/ui-constants';\nimport {`,
);

replaceOrThrow(
  path.join(repoRoot, 'gitnexus-web/src/components/OnboardingGuide.tsx'),
  `<span>Port 4747</span>`,
  `<span>Port 3010</span>`,
);

run('npm install', path.join(repoRoot, 'gitnexus-shared'));
run('npm run build', path.join(repoRoot, 'gitnexus-shared'));
run('npm install', path.join(repoRoot, 'gitnexus-web'));
