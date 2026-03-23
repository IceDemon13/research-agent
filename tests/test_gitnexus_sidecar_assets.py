from __future__ import annotations

import unittest
from pathlib import Path


class GitNexusSidecarAssetsTests(unittest.TestCase):
    def test_sidecar_server_declares_control_endpoints_and_analyze_command(self) -> None:
        script_path = Path("scripts/gitnexus-sidecar-server.mjs")
        content = script_path.read_text(encoding="utf-8")

        self.assertIn("/control/health", content)
        self.assertIn("/control/analyze", content)
        self.assertIn("gitnexus@", content)
        self.assertIn("\"analyze\"", content)
        self.assertIn("\"serve\"", content)
        self.assertIn("\"analyze\", \"--help\"", content)
        self.assertIn("parseSupportedAnalyzeFlags", content)
        self.assertIn("Omitting unsupported GitNexus analyze flag", content)
        self.assertIn("--skip-embeddings", content)
        self.assertIn("--skills", content)
        self.assertIn("--force", content)
        self.assertIn("cliVersion", content)

    def test_docker_compose_uses_sidecar_wrapper_script(self) -> None:
        compose_text = Path("docker-compose.yml").read_text(encoding="utf-8")

        self.assertIn("gitnexus-sidecar-server.mjs", compose_text)
        self.assertIn("GITNEXUS_CONTROL_ENABLED", compose_text)
        self.assertIn("GITNEXUS_MCP_PORT_INTERNAL", compose_text)


if __name__ == "__main__":
    unittest.main()
