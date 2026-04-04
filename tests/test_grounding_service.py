import unittest
from pathlib import Path
import shutil

from contracts.grounding_contract import GroundingProviderResult
from contracts.grounding_contract import GroundingCandidateSymbol
from services.grounding_service import (
    EmbeddingRecallProvider,
    FamilyAwareLocalRecallProvider,
    GitNexusProvider,
    GroundingService,
    LocalLexicalFileSearchProvider,
    LocalCodeRecallProvider,
    LocalOwnerSnippetRecallProvider,
    RepoProfileProvider,
    TreeSitterSymbolProvider,
    _normalize_grounding_task_text,
)
from contracts.repo_metadata import RepoMetadata


class _StaticRepoProfileProvider:
    def __init__(self, repo_profile: dict | None = None) -> None:
        self._repo_profile = dict(repo_profile or {})

    def provide(self, *, repo_id: str, repo_context: dict, current_candidate_files: list[str]) -> GroundingProviderResult:
        return GroundingProviderResult(
            provider_name="repo_profile",
            available=True,
            indexed=True,
            query_succeeded=True,
            repo_profile=dict(self._repo_profile or {}),
            diagnostics={"repo_name": repo_id or "sample"},
        )


class _StaticGitNexusProvider:
    def __init__(self, *, available: bool, indexed: bool) -> None:
        self._available = available
        self._indexed = indexed

    def provide(self, *, repo_id: str, repo_context: dict, current_candidate_files: list[str], task_text: str) -> GroundingProviderResult:
        return GroundingProviderResult(
            provider_name="gitnexus",
            available=self._available,
            indexed=self._indexed,
            query_succeeded=False,
            diagnostics={"reason": "synthetic test provider"},
        )


class _StaticRegistryService:
    def __init__(self, repo: RepoMetadata | None) -> None:
        self._repo = repo

    def get_repo(self, repo_id: str):
        if self._repo and self._repo.repo_id == repo_id:
            return self._repo
        return None


class _StaticBridgeService:
    def __init__(self, *, available: bool = True, repo_allowed: bool = True, result=None, query_debug: dict | None = None) -> None:
        self._available = available
        self._repo_allowed = repo_allowed
        self._result = result
        self._query_debug = dict(query_debug or {})

    def probe_backend(self, timeout_seconds: int = 2):
        return {"available": self._available, "message": "ok" if self._available else "down"}

    def repo_allowed(self, repo_id: str) -> bool:
        return self._repo_allowed

    def query(self, repo_meta, task_text: str):
        if self._result is None:
            raise RuntimeError("query should not be called")
        return self._result

    def last_query_debug_snapshot(self):
        return dict(self._query_debug)


class _CapturingGitNexusProvider:
    def __init__(self) -> None:
        self.last_task_text = ""

    def provide(self, *, repo_id: str, repo_context: dict, current_candidate_files: list[str], task_text: str) -> GroundingProviderResult:
        self.last_task_text = str(task_text or "")
        return GroundingProviderResult(
            provider_name="gitnexus",
            available=True,
            indexed=True,
            query_succeeded=False,
            diagnostics={
                "gitnexus_available": True,
                "gitnexus_indexed": True,
                "gitnexus_query_succeeded": False,
                "gitnexus_result_count": 0,
                "gitnexus_used_in_grounding": False,
            },
        )


class _StaticGitNexusIndexService:
    def __init__(self, *, visible: bool, visible_count: int = 0, visible_paths: list[str] | None = None, index_root: str = "/gitnexus") -> None:
        self._visible = visible
        self._visible_count = visible_count
        self._visible_paths = list(visible_paths or [])
        self._index_root = index_root

    def backend_runtime_status(self):
        return {"backend_runtime": {"gitnexusHome": self._index_root}}

    def repo_visibility_debug(self, repo_meta):
        return {
            "visible": self._visible,
            "visible_repo_count": self._visible_count,
            "visible_repo_ids_or_paths": list(self._visible_paths),
            "visibility_match_reason": "synthetic visibility match" if self._visible else "",
            "error": "" if self._visible else "repo not visible",
        }


class _StaticTreeSitterProvider:
    def __init__(self, candidate_symbols: list | None = None, diagnostics: dict | None = None) -> None:
        self._candidate_symbols = list(candidate_symbols or [])
        self._diagnostics = dict(diagnostics or {})

    def provide(self, *, repo_id: str, repo_context: dict, current_candidate_files: list[str], task_text: str = "") -> GroundingProviderResult:
        return GroundingProviderResult(
            provider_name="tree_sitter",
            available=True,
            indexed=None,
            query_succeeded=bool(self._candidate_symbols),
            candidate_symbols=list(self._candidate_symbols),
            diagnostics=dict(self._diagnostics),
        )

    def extract_selected_file_symbols(self, *, root_path: str, selected_file: str, task_text: str = ""):
        normalized = selected_file.replace("\\", "/").strip().strip("/")
        matching = [
            item
            for item in list(self._candidate_symbols)
            if item.file_path.replace("\\", "/").strip().strip("/") == normalized
        ]
        return matching, {
            "selected_file_symbol_extraction_status": "passed" if matching else "method_parse_failed",
            "selected_file_symbol_extraction_reason": "synthetic test provider",
            "selected_file_exists": True,
            "selected_file_normalized_path": normalized,
            "selected_file_extension": Path(normalized).suffix.lower(),
            "selected_file_bytes_loaded": 123,
            "selected_file_chars_loaded": 123,
            "selected_file_extractor_invoked": True,
            "selected_file_classes_found": sorted({item.class_name for item in matching if item.class_name}),
            "selected_file_methods_found": [item.method_name for item in matching if item.method_name],
            "selected_file_symbol_filter_count": 0,
        }


class _NoopLexicalProvider:
    def provide(self, *, repo_id: str, repo_context: dict, current_candidate_files: list[str], task_text: str) -> GroundingProviderResult:
        return GroundingProviderResult(
            provider_name="lexical",
            available=True,
            indexed=None,
            query_succeeded=False,
            candidate_files=[],
            diagnostics={"lexical_provider_used": True, "lexical_candidates_count": 0, "lexical_candidates_added": 0},
        )


class _NoopLocalCodeRecallProvider:
    def provide(self, *, repo_id: str, repo_context: dict, current_candidate_files: list[str], task_text: str) -> GroundingProviderResult:
        return GroundingProviderResult(
            provider_name="local_code_recall",
            available=True,
            indexed=None,
            query_succeeded=False,
            candidate_files=[],
            diagnostics={
                "local_code_recall_used": True,
                "local_code_recall_result_count": 0,
                "local_code_recall_top_files": [],
                "local_code_recall_score_breakdown": [],
                "local_code_recall_added_candidates": 0,
            },
        )


class _NoopOwnerSnippetRecallProvider:
    def provide(self, *, repo_id: str, repo_context: dict, current_candidate_files: list[str], task_text: str) -> GroundingProviderResult:
        return GroundingProviderResult(
            provider_name="owner_snippet_recall",
            available=True,
            indexed=None,
            query_succeeded=False,
            candidate_files=[],
            diagnostics={
                "owner_snippet_recall_used": True,
                "owner_snippet_recall_result_count": 0,
                "owner_snippet_recall_top_files": [],
                "owner_snippet_recall_score_breakdown": [],
                "owner_snippet_recall_added_candidates": 0,
            },
        )


class _NoopFamilyRecallProvider:
    def provide(self, *, repo_id: str, repo_context: dict, current_candidate_files: list[str], task_text: str) -> GroundingProviderResult:
        return GroundingProviderResult(
            provider_name="family_recall",
            available=True,
            indexed=None,
            query_succeeded=False,
            candidate_files=[],
            diagnostics={
                "family_recall_used": True,
                "family_recall_family_detected": "",
                "family_recall_result_count": 0,
                "family_recall_top_files": [],
                "family_recall_score_breakdown": [],
                "family_recall_added_candidates": 0,
            },
        )


class _RepoLocalTempDir:
    def __init__(self, name: str) -> None:
        self._path = Path("tests") / "_tmp_grounding" / name

    def __enter__(self) -> Path:
        if self._path.exists():
            shutil.rmtree(self._path, ignore_errors=True)
        self._path.mkdir(parents=True, exist_ok=True)
        return self._path

    def __exit__(self, exc_type, exc, tb) -> None:
        shutil.rmtree(self._path, ignore_errors=True)


class GroundingServiceTests(unittest.TestCase):
    def test_normalize_grounding_task_text_prefers_prompt_task_text(self) -> None:
        self.assertEqual(
            _normalize_grounding_task_text(
                {
                    "prompt_task_text": "Real jira body",
                    "task_text": "Build an implementation plan for this Jira task content:\nWrapped body\n\nInstruction tail",
                }
            ),
            "Real jira body",
        )

    def test_build_planning_grounding_strips_workflow_wrapper_before_gitnexus(self) -> None:
        gitnexus_provider = _CapturingGitNexusProvider()
        service = GroundingService(
            repo_profile_provider=_StaticRepoProfileProvider({"primary_stack": "Python"}),
            lexical_provider=_NoopLexicalProvider(),
            tree_sitter_provider=_StaticTreeSitterProvider(),
            embedding_provider=EmbeddingRecallProvider(),
            gitnexus_provider=gitnexus_provider,
        )

        service.build_planning_grounding(
            repo_id="sample",
            jira_task_payload={
                "task_text": (
                    "Build an implementation plan for this Jira task content:\n"
                    "Pack order into several cells\n\n"
                    "Name exact file paths when confidence is high."
                )
            },
            current_candidate_files=["services/grounding_service.py"],
            repo_context={"repo_id": "sample", "root_path": ".", "resolved_symbols": {}},
        )

        self.assertEqual(gitnexus_provider.last_task_text, "Pack order into several cells")

    def test_gitnexus_provider_uses_visibility_fallback_when_registry_index_flag_is_false(self) -> None:
        repo = RepoMetadata(
            repo_id="sample",
            root_path="/repos/sample",
            local_path="/repos/sample",
            display_name="sample",
            default_branch="main",
            indexed_at="",
            status="ready",
            gitnexus_indexed=False,
            gitnexus_index_status="",
        )
        provider = GitNexusProvider(
            bridge_service=_StaticBridgeService(available=True, repo_allowed=True, result=type("R", (), {"files": [], "symbols": []})()),
            registry_service=_StaticRegistryService(repo),
            index_service=_StaticGitNexusIndexService(visible=True, visible_count=1, visible_paths=["/repos/sample"]),
        )

        result = provider.provide(
            repo_id="sample",
            repo_context={"root_path": "."},
            current_candidate_files=[],
            task_text="sample query",
        )

        self.assertTrue(result.available)
        self.assertTrue(result.indexed)
        self.assertTrue(result.query_succeeded)
        self.assertEqual(result.diagnostics["gitnexus_index_root"], "/gitnexus")
        self.assertEqual(result.diagnostics["gitnexus_indexed_repo_count"], 1)
        self.assertTrue(result.diagnostics["gitnexus_query_succeeded"])

    def test_gitnexus_provider_surfaces_diagnostics_when_not_indexed(self) -> None:
        repo = RepoMetadata(
            repo_id="sample",
            root_path="/repos/sample",
            local_path="/repos/sample",
            display_name="sample",
            default_branch="main",
            indexed_at="",
            status="ready",
            gitnexus_indexed=False,
            gitnexus_index_status="",
        )
        provider = GitNexusProvider(
            bridge_service=_StaticBridgeService(available=True, repo_allowed=True, result=None),
            registry_service=_StaticRegistryService(repo),
            index_service=_StaticGitNexusIndexService(visible=False, visible_count=0, visible_paths=[]),
        )

        result = provider.provide(
            repo_id="sample",
            repo_context={"root_path": "."},
            current_candidate_files=[],
            task_text="sample query",
        )

        self.assertTrue(result.available)
        self.assertFalse(result.indexed)
        self.assertFalse(result.query_succeeded)
        self.assertEqual(result.diagnostics["gitnexus_indexed_repo_count"], 0)
        self.assertFalse(result.diagnostics["gitnexus_used_in_grounding"])

    def test_gitnexus_provider_surfaces_raw_and_filtered_counts(self) -> None:
        repo = RepoMetadata(
            repo_id="sample",
            root_path="/repos/sample",
            local_path="/repos/sample",
            display_name="sample",
            default_branch="main",
            indexed_at="",
            status="ready",
            gitnexus_indexed=True,
            gitnexus_index_status="ready",
        )
        result_obj = type(
            "R",
            (),
            {
                "files": [
                    type("F", (), {"file_path": "src/Feature/OrderPackCellViewModel.cs", "name": "", "score": 0.81, "reason": "file match"})()
                ],
                "symbols": [
                    type("S", (), {"file_path": "src/Feature/OrderPackCellViewModel.cs", "name": "BuildRows", "score": 0.72, "reason": "symbol match"})()
                ],
            },
        )()
        provider = GitNexusProvider(
            bridge_service=_StaticBridgeService(
                available=True,
                repo_allowed=True,
                result=result_obj,
                query_debug={
                    "gitnexus_query_payload": "order pack cell",
                    "gitnexus_raw_hit_count": 4,
                    "gitnexus_raw_result_excerpt": "synthetic",
                    "normalization_drop_reasons": ["dropped unrelated process"],
                },
            ),
            registry_service=_StaticRegistryService(repo),
            index_service=_StaticGitNexusIndexService(visible=True, visible_count=1, visible_paths=["/repos/sample"]),
        )

        result = provider.provide(
            repo_id="sample",
            repo_context={"root_path": "/repos/sample"},
            current_candidate_files=[],
            task_text="order pack cell",
        )

        self.assertEqual(result.diagnostics["gitnexus_raw_result_count"], 4)
        self.assertEqual(result.diagnostics["gitnexus_filtered_result_count"], 2)
        self.assertEqual(result.diagnostics["gitnexus_result_count"], 2)
        self.assertTrue(result.diagnostics["gitnexus_used_in_grounding"])
        self.assertEqual(result.diagnostics["gitnexus_query_payload"], "order pack cell")

    def test_local_lexical_provider_finds_order_pack_cell_file(self) -> None:
        with _RepoLocalTempDir("order_pack_cell") as root:
            generic_view = root / "src" / "client" / "Telemart.Client" / "ViewModels" / "Store" / "Order" / "OrderViewModel.cs"
            generic_view.parent.mkdir(parents=True, exist_ok=True)
            generic_view.write_text("public class OrderViewModel { public void HandleLoadedAsync() {} }", encoding="utf-8")
            accessory = root / "src" / "client" / "Telemart.Client" / "ViewModels" / "Accessory" / "AccessoriesViewModel.cs"
            accessory.parent.mkdir(parents=True, exist_ok=True)
            accessory.write_text("public class AccessoriesViewModel { public void Refresh() {} }", encoding="utf-8")
            provider = root / "src" / "client" / "Telemart.Client" / "Business" / "Order" / "OrderViewProvider.cs"
            provider.parent.mkdir(parents=True, exist_ok=True)
            provider.write_text("public class OrderViewProvider { public void AddOrderAsync() {} }", encoding="utf-8")
            target = root / "src" / "client" / "Telemart.Client" / "ViewModels" / "Store" / "Order" / "OrderPackCellViewModel.cs"
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text("public class OrderPackCellViewModel { public void BuildRows() {} }", encoding="utf-8")

            result = LocalLexicalFileSearchProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="order pack cell",
            )

        self.assertTrue(result.query_succeeded)
        self.assertEqual(
            result.candidate_files[0].path,
            "src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs",
        )

    def test_local_lexical_provider_finds_streamline_worker(self) -> None:
        with _RepoLocalTempDir("streamline_worker") as root:
            dto = root / "src" / "Parsers" / "Telemart.Parser.Evolve" / "DataTransferObjects" / "ProductDto.cs"
            dto.parent.mkdir(parents=True, exist_ok=True)
            dto.write_text("public class ProductDto { public string Name { get; set; } = string.Empty; }", encoding="utf-8")
            worker = root / "src" / "Workers" / "Telemart.Worker.Streamline" / "StreamlineWorker.cs"
            worker.parent.mkdir(parents=True, exist_ok=True)
            worker.write_text("public class StreamlineWorker { public void Run() {} }", encoding="utf-8")

            result = LocalLexicalFileSearchProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="streamline worker",
            )

        self.assertTrue(result.query_succeeded)
        self.assertEqual(result.candidate_files[0].path, "src/Workers/Telemart.Worker.Streamline/StreamlineWorker.cs")

    def test_local_lexical_provider_prefers_service_invoice_family_over_generic_order_ui(self) -> None:
        with _RepoLocalTempDir("service_invoice") as root:
            order_vm = root / "src" / "client" / "Telemart.Client" / "ViewModels" / "Store" / "Order" / "OrderViewModel.cs"
            order_vm.parent.mkdir(parents=True, exist_ok=True)
            order_vm.write_text("public class OrderViewModel { public void HandleLoadedAsync() {} }", encoding="utf-8")
            invoice_vm = root / "src" / "client" / "Telemart.Client" / "ViewModels" / "Service" / "ServiceInvoices" / "SendServiceInvoiceViewModel.cs"
            invoice_vm.parent.mkdir(parents=True, exist_ok=True)
            invoice_vm.write_text("public class SendServiceInvoiceViewModel { public void SendAsync() {} }", encoding="utf-8")
            invoice_view = root / "src" / "client" / "Telemart.Client" / "Views" / "Service" / "ServiceInvoices" / "ServiceInvoicesView.xaml.cs"
            invoice_view.parent.mkdir(parents=True, exist_ok=True)
            invoice_view.write_text("public class ServiceInvoicesView { public void InitializeComponent() {} }", encoding="utf-8")

            result = LocalLexicalFileSearchProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="service invoice print dropdown",
            )

        self.assertTrue(result.query_succeeded)
        self.assertEqual(
            result.candidate_files[0].path,
            "src/client/Telemart.Client/ViewModels/Service/ServiceInvoices/SendServiceInvoiceViewModel.cs",
        )

    def test_local_lexical_provider_prefers_additional_service_handler_over_security_neighbors(self) -> None:
        with _RepoLocalTempDir("additional_service") as root:
            business_operation = root / "src" / "Telemart.Service" / "Security" / "BusinessOperation.cs"
            business_operation.parent.mkdir(parents=True, exist_ok=True)
            business_operation.write_text("public class BusinessOperation {}", encoding="utf-8")
            auth_handler = root / "src" / "Telemart.Service" / "Security" / "OperationAuthorizationHandler.cs"
            auth_handler.parent.mkdir(parents=True, exist_ok=True)
            auth_handler.write_text("public class OperationAuthorizationHandler { public void HandleRequirementAsync() {} }", encoding="utf-8")
            target_handler = root / "src" / "Telemart.Service" / "Application" / "Commands" / "MaintenanceCommands" / "ChargeAdditionalServicesHandler.cs"
            target_handler.parent.mkdir(parents=True, exist_ok=True)
            target_handler.write_text("public class ChargeAdditionalServicesHandler { public void ExecuteAsync() {} }", encoding="utf-8")
            options = root / "src" / "Telemart.Service" / "Options" / "AdditionalServiceLeftoversOptions.cs"
            options.parent.mkdir(parents=True, exist_ok=True)
            options.write_text("public class AdditionalServiceLeftoversOptions {}", encoding="utf-8")

            result = LocalLexicalFileSearchProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="charge additional services leftovers options",
            )

        self.assertTrue(result.query_succeeded)
        self.assertEqual(
            result.candidate_files[0].path,
            "src/Telemart.Service/Application/Commands/MaintenanceCommands/ChargeAdditionalServicesHandler.cs",
        )
        self.assertEqual(
            result.candidate_files[1].path,
            "src/Telemart.Service/Options/AdditionalServiceLeftoversOptions.cs",
        )

    def test_local_lexical_provider_prefers_order_repository_over_security_neighbors(self) -> None:
        with _RepoLocalTempDir("order_repository") as root:
            business_operation = root / "src" / "Telemart.Service" / "Security" / "BusinessOperation.cs"
            business_operation.parent.mkdir(parents=True, exist_ok=True)
            business_operation.write_text("public class BusinessOperation {}", encoding="utf-8")
            auth_handler = root / "src" / "Telemart.Service" / "Security" / "OperationAuthorizationHandler.cs"
            auth_handler.parent.mkdir(parents=True, exist_ok=True)
            auth_handler.write_text("public class OperationAuthorizationHandler { public void HandleRequirementAsync() {} }", encoding="utf-8")
            repository = root / "src" / "Telemart.Service" / "Repositories" / "OrderRepository.cs"
            repository.parent.mkdir(parents=True, exist_ok=True)
            repository.write_text("public class OrderRepository { public void SaveAsync() {} }", encoding="utf-8")

            result = LocalLexicalFileSearchProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="order repository update database table",
            )

        self.assertTrue(result.query_succeeded)
        self.assertEqual(
            result.candidate_files[0].path,
            "src/Telemart.Service/Repositories/OrderRepository.cs",
        )
        self.assertGreaterEqual(result.diagnostics.get("lexical_specific_intent_bonus_applied", 0), 1)
        self.assertGreaterEqual(result.diagnostics.get("lexical_same_stem_priority_applied", 0), 1)
        self.assertIsInstance(result.diagnostics.get("lexical_rank_breakdown"), list)

    def test_local_code_recall_prefers_order_repository_over_generic_neighbors(self) -> None:
        with _RepoLocalTempDir("local_code_recall_order_repository") as root:
            (root / "src" / "Telemart.Service" / "Security").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Repositories").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Security" / "BusinessOperation.cs").write_text(
                "namespace Telemart.Service.Security; public class BusinessOperation { public void ExecuteAsync() {} }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Security" / "OperationAuthorizationHandler.cs").write_text(
                "namespace Telemart.Service.Security; public class OperationAuthorizationHandler { public void HandleRequirementAsync() {} }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Repositories" / "OrderRepository.cs").write_text(
                "namespace Telemart.Service.Repositories; public class OrderRepository { public async Task UpdateOrderCellsAsync() { await Task.CompletedTask; } }",
                encoding="utf-8",
            )
            result = LocalCodeRecallProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="order repository update duplicate external order table",
            )

        self.assertTrue(result.query_succeeded)
        self.assertEqual(result.candidate_files[0].path, "src/Telemart.Service/Repositories/OrderRepository.cs")

    def test_local_code_recall_prefers_streamline_worker_over_generic_product_files(self) -> None:
        with _RepoLocalTempDir("local_code_recall_streamline_worker") as root:
            (root / "src" / "Parsers" / "Telemart.Parser.Diwave" / "Items").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Workers" / "Telemart.Worker.Streamline").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Parsers" / "Telemart.Parser.Diwave" / "Items" / "ProductItem.cs").write_text(
                "namespace Telemart.Parser.Diwave.Items; public class ProductItem { public string GetName() => string.Empty; }",
                encoding="utf-8",
            )
            (root / "src" / "Parsers" / "Telemart.Parser.Diwave" / "Items" / "ProductDto.cs").write_text(
                "namespace Telemart.Parser.Diwave.Items; public class ProductDto { public string GetCode() => string.Empty; }",
                encoding="utf-8",
            )
            (root / "src" / "Workers" / "Telemart.Worker.Streamline" / "StreamlineWorker.cs").write_text(
                "namespace Telemart.Worker.Streamline; public class StreamlineWorker { public async Task ExecuteAsync() { await Task.CompletedTask; } }",
                encoding="utf-8",
            )
            result = LocalCodeRecallProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="streamline worker transfer category manager product sync",
            )

        self.assertTrue(result.query_succeeded)
        self.assertEqual(result.candidate_files[0].path, "src/Workers/Telemart.Worker.Streamline/StreamlineWorker.cs")

    def test_local_code_recall_prefers_service_invoice_family_over_generic_order_ui(self) -> None:
        with _RepoLocalTempDir("local_code_recall_service_invoice") as root:
            (root / "src" / "client" / "Telemart.Client" / "ViewModels" / "Store" / "Order").mkdir(parents=True, exist_ok=True)
            (root / "src" / "client" / "Telemart.Client" / "ViewModels" / "Service" / "ServiceInvoices").mkdir(parents=True, exist_ok=True)
            (root / "src" / "client" / "Telemart.Client" / "ViewModels" / "Store" / "Order" / "OrderViewModel.cs").write_text(
                "namespace Telemart.Client.ViewModels.Store.Order; public class OrderViewModel { public void HandleLoadedAsync() {} }",
                encoding="utf-8",
            )
            (root / "src" / "client" / "Telemart.Client" / "ViewModels" / "Service" / "ServiceInvoices" / "ServiceInvoiceViewModel.cs").write_text(
                "namespace Telemart.Client.ViewModels.Service.ServiceInvoices; public class ServiceInvoiceViewModel { public async Task PrintAsync() { await Task.CompletedTask; } }",
                encoding="utf-8",
            )
            (root / "src" / "client" / "Telemart.Client" / "ViewModels" / "Service" / "ServiceInvoices" / "SendServiceInvoiceViewModel.cs").write_text(
                "namespace Telemart.Client.ViewModels.Service.ServiceInvoices; public class SendServiceInvoiceViewModel { public async Task SendServiceInvoiceAsync() { await Task.CompletedTask; } }",
                encoding="utf-8",
            )
            result = LocalCodeRecallProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="service invoice print dropdown ttn insurance fix",
            )

        self.assertTrue(result.query_succeeded)
        self.assertIn(
            result.candidate_files[0].path,
            {
                "src/client/Telemart.Client/ViewModels/Service/ServiceInvoices/ServiceInvoiceViewModel.cs",
                "src/client/Telemart.Client/ViewModels/Service/ServiceInvoices/SendServiceInvoiceViewModel.cs",
            },
        )

    def test_local_code_recall_prefers_additional_service_family_over_security_neighbors(self) -> None:
        with _RepoLocalTempDir("local_code_recall_additional_service") as root:
            (root / "src" / "Telemart.Service" / "Security").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Application" / "Commands" / "MaintenanceCommands").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Options").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Security" / "BusinessOperation.cs").write_text(
                "namespace Telemart.Service.Security; public class BusinessOperation {}",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Security" / "OperationAuthorizationHandler.cs").write_text(
                "namespace Telemart.Service.Security; public class OperationAuthorizationHandler { public void HandleRequirementAsync() {} }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Application" / "Commands" / "MaintenanceCommands" / "ChargeAdditionalServicesHandler.cs").write_text(
                "namespace Telemart.Service.Application.Commands.MaintenanceCommands; public class ChargeAdditionalServicesHandler { public async Task ExecuteAsync() { await Task.CompletedTask; } }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Options" / "AdditionalServiceLeftoversOptions.cs").write_text(
                "namespace Telemart.Service.Options; public class AdditionalServiceLeftoversOptions { public int[] ProductTypeIds { get; set; } = []; }",
                encoding="utf-8",
            )
            result = LocalCodeRecallProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="charge additional service leftovers options product type ids",
            )

        self.assertTrue(result.query_succeeded)
        top_paths = [item.path for item in result.candidate_files[:2]]
        self.assertIn("src/Telemart.Service/Application/Commands/MaintenanceCommands/ChargeAdditionalServicesHandler.cs", top_paths)
        self.assertIn("src/Telemart.Service/Options/AdditionalServiceLeftoversOptions.cs", top_paths)

    def test_owner_snippet_recall_prefers_order_repository_over_monobank_handlers_when_owner_text_matches(self) -> None:
        with _RepoLocalTempDir("owner_snippet_recall_order_repository") as root:
            (root / "src" / "Telemart.Service" / "Application" / "Commands" / "MonoBankCorpCommands").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Repositories").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Application" / "Commands" / "MonoBankCorpCommands" / "MonobankCorpFillStatementsHandler.cs").write_text(
                "namespace Telemart.Service.Application.Commands.MonoBankCorpCommands; public class MonobankCorpFillStatementsHandler { public async Task ExecuteAsync() { var duplicateOrders = true; await Task.CompletedTask; } }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Repositories" / "OrderRepository.cs").write_text(
                "namespace Telemart.Service.Repositories; public class OrderRepository { public Dictionary<string, int> BuildExternalOrderMap() => new(); public async Task UpdateOrderAsync() { await Task.CompletedTask; } public string ExternalOrderId { get; set; } = string.Empty; }",
                encoding="utf-8",
            )
            result = LocalOwnerSnippetRecallProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="fill monobank corp statements duplicate external order repository map",
            )

        self.assertTrue(result.query_succeeded)
        self.assertEqual(result.candidate_files[0].path, "src/Telemart.Service/Repositories/OrderRepository.cs")

    def test_owner_snippet_recall_prefers_streamline_worker_over_generic_product_files(self) -> None:
        with _RepoLocalTempDir("owner_snippet_recall_streamline") as root:
            (root / "src" / "Parsers" / "Telemart.Parser.Diwave" / "Items").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Workers" / "Telemart.Worker.Streamline").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Parsers" / "Telemart.Parser.Diwave" / "Items" / "ProductItem.cs").write_text(
                "namespace Telemart.Parser.Diwave.Items; public class ProductItem { public string CategoryManager { get; set; } = string.Empty; public string GetCategoryCode() => string.Empty; }",
                encoding="utf-8",
            )
            (root / "src" / "Workers" / "Telemart.Worker.Streamline" / "StreamlineWorker.cs").write_text(
                "namespace Telemart.Worker.Streamline; public class StreamlineWorker { public string CategoryManagerField = string.Empty; public async Task SyncCategoryManagerAsync() { await Task.CompletedTask; } }",
                encoding="utf-8",
            )
            first = LocalOwnerSnippetRecallProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="streamline worker category manager sync",
            )
            second = LocalOwnerSnippetRecallProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="streamline worker category manager sync",
            )

        self.assertTrue(first.query_succeeded)
        self.assertEqual(first.candidate_files[0].path, "src/Workers/Telemart.Worker.Streamline/StreamlineWorker.cs")
        self.assertEqual(
            [item.path for item in first.candidate_files],
            [item.path for item in second.candidate_files],
        )

    def test_family_recall_prefers_monobank_statement_repository_family_above_generic_handlers(self) -> None:
        with _RepoLocalTempDir("family_recall_monobank") as root:
            (root / "src" / "Telemart.Service" / "Application" / "Commands" / "MonoBankCorpCommands").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Repositories").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Application" / "Commands" / "MonoBankCorpCommands" / "MonobankCorpFillStatementsHandler.cs").write_text(
                "namespace Telemart.Service.Application.Commands.MonoBankCorpCommands; public class MonobankCorpFillStatementsHandler { public async Task ExecuteAsync() { await Task.CompletedTask; } }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Repositories" / "OrderRepository.cs").write_text(
                "namespace Telemart.Service.Repositories; public class OrderRepository { public Dictionary<string, int> BuildExternalOrderMap() => new(); public string ExternalOrderId { get; set; } = string.Empty; }",
                encoding="utf-8",
            )
            result = FamilyAwareLocalRecallProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="monobank fill statements duplicate external order map repository",
            )

        self.assertTrue(result.query_succeeded)
        self.assertEqual(result.candidate_files[0].path, "src/Telemart.Service/Repositories/OrderRepository.cs")
        self.assertEqual(result.diagnostics["family_recall_family_detected"], "monobank_statement")

    def test_family_recall_prefers_repository_implementation_over_interface_when_both_exist(self) -> None:
        with _RepoLocalTempDir("family_recall_repository_impl_pair") as root:
            (root / "src" / "Telemart.Service" / "Repositories" / "Interfaces").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Repositories").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Repositories" / "Interfaces" / "IOrderRepository.cs").write_text(
                "namespace Telemart.Service.Repositories.Interfaces; public interface IOrderRepository { Dictionary<string, int> BuildExternalOrderMap(); }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Repositories" / "OrderRepository.cs").write_text(
                "namespace Telemart.Service.Repositories; public class OrderRepository : IOrderRepository { public Dictionary<string, int> BuildExternalOrderMap() => new(); public async Task UpdateOrderAsync() { await Task.CompletedTask; } public string ExternalOrderId { get; set; } = string.Empty; }",
                encoding="utf-8",
            )
            result = FamilyAwareLocalRecallProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="monobank fill statements external order map repository",
            )

        self.assertTrue(result.query_succeeded)
        top_paths = [item.path for item in result.candidate_files[:3]]
        self.assertEqual(top_paths[0], "src/Telemart.Service/Repositories/OrderRepository.cs")
        self.assertIn("src/Telemart.Service/Repositories/Interfaces/IOrderRepository.cs", top_paths)
        impl_breakdown = next(
            item for item in result.diagnostics["family_recall_score_breakdown"]
            if item["path"] == "src/Telemart.Service/Repositories/OrderRepository.cs"
        )
        iface_breakdown = next(
            item for item in result.diagnostics["family_recall_score_breakdown"]
            if item["path"] == "src/Telemart.Service/Repositories/Interfaces/IOrderRepository.cs"
        )
        self.assertGreater(float(impl_breakdown["repository_impl_boost_applied"]), 0.0)
        self.assertGreater(float(iface_breakdown["repository_interface_penalty_applied"]), 0.0)

    def test_family_recall_monobank_order_map_prefers_order_repository_over_handler_request_neighbors(self) -> None:
        with _RepoLocalTempDir("family_recall_monobank_repository_impl_focus") as root:
            (root / "src" / "Telemart.Service" / "Application" / "Commands" / "MonoBankCorpCommands").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Repositories" / "Interfaces").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Repositories").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Application" / "Commands" / "MonoBankCorpCommands" / "MonobankCorpFillStatementsHandler.cs").write_text(
                "namespace Telemart.Service.Application.Commands.MonoBankCorpCommands; public class MonobankCorpFillStatementsHandler { public async Task ExecuteAsync() { await Task.CompletedTask; } }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Application" / "Commands" / "MonoBankCorpCommands" / "MonobankCorpFillStatementsRequest.cs").write_text(
                "namespace Telemart.Service.Application.Commands.MonoBankCorpCommands; public class MonobankCorpFillStatementsRequest {}",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Repositories" / "Interfaces" / "IOrderRepository.cs").write_text(
                "namespace Telemart.Service.Repositories.Interfaces; public interface IOrderRepository { Dictionary<string, int> BuildExternalOrderMap(); }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Repositories" / "OrderRepository.cs").write_text(
                "namespace Telemart.Service.Repositories; public class OrderRepository : IOrderRepository { public Dictionary<string, int> BuildExternalOrderMap() => new(); public async Task UpdateOrderAsync() { await Task.CompletedTask; } public string ExternalOrderId { get; set; } = string.Empty; }",
                encoding="utf-8",
            )
            first = FamilyAwareLocalRecallProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="monobank corp fill statements duplicate external order map repository",
            )
            second = FamilyAwareLocalRecallProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="monobank corp fill statements duplicate external order map repository",
            )

        self.assertTrue(first.query_succeeded)
        self.assertEqual(first.candidate_files[0].path, "src/Telemart.Service/Repositories/OrderRepository.cs")
        self.assertEqual(
            [item.path for item in first.candidate_files],
            [item.path for item in second.candidate_files],
        )

    def test_family_recall_monobank_order_repository_beats_external_payment_repository_when_order_map_semantics_are_strong(self) -> None:
        with _RepoLocalTempDir("family_recall_monobank_order_vs_external_payment") as root:
            (root / "src" / "Telemart.Service" / "Repositories" / "Interfaces").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Repositories").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Repositories" / "Interfaces" / "IOrderRepository.cs").write_text(
                "namespace Telemart.Service.Repositories.Interfaces; public interface IOrderRepository { Dictionary<string, int> BuildExternalOrderMap(); }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Repositories" / "OrderRepository.cs").write_text(
                "namespace Telemart.Service.Repositories; public class OrderRepository : IOrderRepository { public Dictionary<string, int> BuildExternalOrderMap() => new(); public async Task GetExternalOrderMapAsync() { await Task.CompletedTask; } public string ExternalOrderId { get; set; } = string.Empty; }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Repositories" / "Interfaces" / "IExternalPaymentDataRepository.cs").write_text(
                "namespace Telemart.Service.Repositories.Interfaces; public interface IExternalPaymentDataRepository { string FindExternalPaymentData(); }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Repositories" / "ExternalPaymentDataRepository.cs").write_text(
                "namespace Telemart.Service.Repositories; public class ExternalPaymentDataRepository : IExternalPaymentDataRepository { public string FindExternalPaymentData() => string.Empty; public string ExternalPaymentStatus { get; set; } = string.Empty; }",
                encoding="utf-8",
            )
            first = FamilyAwareLocalRecallProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="monobank corp fill statements duplicate external order map repository",
            )
            second = FamilyAwareLocalRecallProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="monobank corp fill statements duplicate external order map repository",
            )

        self.assertTrue(first.query_succeeded)
        self.assertEqual(first.candidate_files[0].path, "src/Telemart.Service/Repositories/OrderRepository.cs")
        self.assertEqual(
            [item.path for item in first.candidate_files],
            [item.path for item in second.candidate_files],
        )
        breakdown = {item["path"]: item for item in first.diagnostics["family_recall_score_breakdown"]}
        self.assertGreater(
            float(breakdown["src/Telemart.Service/Repositories/OrderRepository.cs"]["monobank_order_repository_boost_applied"]),
            0.0,
        )
        self.assertGreater(
            float(breakdown["src/Telemart.Service/Repositories/ExternalPaymentDataRepository.cs"]["monobank_external_payment_repository_penalty_applied"]),
            0.0,
        )

    def test_family_recall_prefers_streamline_worker_family_above_generic_product_files(self) -> None:
        with _RepoLocalTempDir("family_recall_streamline") as root:
            (root / "src" / "Parsers" / "Telemart.Parser.Diwave" / "Items").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Workers" / "Telemart.Worker.Streamline").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Parsers" / "Telemart.Parser.Diwave" / "Items" / "ProductItem.cs").write_text(
                "namespace Telemart.Parser.Diwave.Items; public class ProductItem { public string GetCategoryCode() => string.Empty; public string ProductStream { get; set; } = string.Empty; }",
                encoding="utf-8",
            )
            (root / "src" / "Workers" / "Telemart.Worker.Streamline" / "StreamlineWorker.cs").write_text(
                "namespace Telemart.Worker.Streamline; public class StreamlineWorker { public async Task ProcessItemImportAsync() { await Task.CompletedTask; } public string ItemMappingMode { get; set; } = string.Empty; }",
                encoding="utf-8",
            )
            first = FamilyAwareLocalRecallProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="streamline worker item mapping product sync import",
            )
            second = FamilyAwareLocalRecallProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=[],
                task_text="streamline worker item mapping product sync import",
            )

        self.assertTrue(first.query_succeeded)
        self.assertEqual(first.candidate_files[0].path, "src/Workers/Telemart.Worker.Streamline/StreamlineWorker.cs")
        self.assertEqual(first.diagnostics["family_recall_family_detected"], "streamline_worker")
        self.assertEqual(
            [item.path for item in first.candidate_files],
            [item.path for item in second.candidate_files],
        )

    def test_gitnexus_unavailable_still_returns_local_grounding(self) -> None:
        service = GroundingService(
            repo_profile_provider=_StaticRepoProfileProvider(
                {"primary_stack": "Python", "source_roots": ["services"], "project_files": ["pyproject.toml"]}
            ),
            lexical_provider=LocalLexicalFileSearchProvider(),
            tree_sitter_provider=TreeSitterSymbolProvider(),
            embedding_provider=EmbeddingRecallProvider(),
            gitnexus_provider=_StaticGitNexusProvider(available=False, indexed=False),
        )
        context = service.build_planning_grounding(
            repo_id="sample",
            jira_task_payload={"task_text": "Update planning grounding behavior."},
            current_candidate_files=["services/grounding_service.py"],
            repo_context={"repo_id": "sample", "root_path": ".", "resolved_symbols": {}},
        )

        self.assertTrue(context.candidate_files)
        self.assertIn("services/grounding_service.py", [item.path for item in context.candidate_files])
        self.assertTrue(context.candidate_symbols)
        self.assertEqual(
            context.diagnostics.provider_statuses["gitnexus"]["available"],
            False,
        )
        self.assertIn("lexical", context.diagnostics.provider_statuses)

    def test_gitnexus_unindexed_soft_fallback_works(self) -> None:
        with _RepoLocalTempDir("gitnexus_soft_fallback") as root:
            target = root / "src" / "feature" / "OrderPackCellViewModel.cs"
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text("public class OrderPackCellViewModel { public void BuildRows() {} }", encoding="utf-8")

            service = GroundingService(
                repo_profile_provider=_StaticRepoProfileProvider({"primary_stack": "C#, WPF"}),
                lexical_provider=LocalLexicalFileSearchProvider(),
                tree_sitter_provider=TreeSitterSymbolProvider(),
                embedding_provider=EmbeddingRecallProvider(),
                gitnexus_provider=_StaticGitNexusProvider(available=True, indexed=False),
            )
            context = service.build_planning_grounding(
                repo_id="sample",
                jira_task_payload={"task_text": "Update order pack cell behavior."},
                current_candidate_files=[],
                repo_context={"repo_id": "sample", "root_path": str(root), "resolved_symbols": {}},
            )

        self.assertEqual(context.diagnostics.provider_statuses["gitnexus"]["available"], True)
        self.assertEqual(context.diagnostics.provider_statuses["gitnexus"]["indexed"], False)
        self.assertTrue(any(item.path.endswith("OrderPackCellViewModel.cs") for item in context.candidate_files))
        self.assertGreaterEqual(context.diagnostics.provider_statuses["lexical"]["lexical_candidates_added"], 1)

    def test_tree_sitter_provider_returns_local_symbols(self) -> None:
        result = TreeSitterSymbolProvider().provide(
            repo_id="sample",
            repo_context={"root_path": "."},
            current_candidate_files=["services/grounding_service.py"],
            task_text="grounding service symbol ranking",
        )

        self.assertTrue(result.query_succeeded)
        self.assertTrue(result.candidate_symbols)
        self.assertTrue(result.candidate_symbols[0].class_name)
        self.assertTrue(result.candidate_symbols[0].method_name)

    def test_tree_sitter_provider_prefers_order_pack_cell_buildrows(self) -> None:
        with _RepoLocalTempDir("tree_sitter_order_pack") as root:
            target = root / "src" / "client" / "Telemart.Client" / "ViewModels" / "Store" / "Order" / "OrderPackCellViewModel.cs"
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(
                "public class OrderPackCellViewModel { public void HandleAsync() {} public void BuildRows() {} public void LoadData() {} }",
                encoding="utf-8",
            )
            result = TreeSitterSymbolProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=["src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs"],
                task_text="pack order into cells",
            )

        self.assertTrue(result.query_succeeded)
        top = result.candidate_symbols[0]
        self.assertEqual(top.file_path, "src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs")
        self.assertEqual(top.class_name, "OrderPackCellViewModel")
        self.assertEqual(top.method_name, "BuildRows")
        self.assertGreaterEqual(result.diagnostics.get("symbol_action_match", 0), 1)

    def test_tree_sitter_provider_prefers_streamline_worker_symbol(self) -> None:
        with _RepoLocalTempDir("tree_sitter_streamline") as root:
            target = root / "src" / "Workers" / "Telemart.Worker.Streamline" / "StreamlineWorker.cs"
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(
                "public class StreamlineWorker { public void Run() {} public void ExecuteAsync() {} }",
                encoding="utf-8",
            )
            result = TreeSitterSymbolProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=["src/Workers/Telemart.Worker.Streamline/StreamlineWorker.cs"],
                task_text="streamline worker sync products",
            )

        self.assertTrue(result.query_succeeded)
        top = result.candidate_symbols[0]
        self.assertEqual(top.class_name, "StreamlineWorker")
        self.assertEqual(top.method_name, "ExecuteAsync")
        self.assertIn("symbol rank in file: 1", top.reasons)

    def test_extract_selected_file_symbols_returns_debug_diagnostics_for_csharp_file(self) -> None:
        with _RepoLocalTempDir("selected_file_extract") as root:
            target = root / "src" / "client" / "Telemart.Client" / "ViewModels" / "Store" / "Order" / "OrderPackCellViewModel.cs"
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(
                "public class OrderPackCellViewModel { public async Task BuildRowsAsync(OrderCell cell) { await Task.CompletedTask; } }",
                encoding="utf-8",
            )

            symbols, diagnostics = TreeSitterSymbolProvider().extract_selected_file_symbols(
                root_path=str(root),
                selected_file="src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs",
                task_text="pack order into cells",
            )

        self.assertTrue(symbols)
        self.assertEqual(diagnostics["selected_file_symbol_extraction_status"], "passed")
        self.assertEqual(diagnostics["selected_file_classes_found"], ["OrderPackCellViewModel"])
        self.assertIn("BuildRowsAsync", diagnostics["selected_file_methods_found"])
        self.assertGreater(int(diagnostics["selected_file_bytes_loaded"]), 0)
        self.assertTrue(bool(diagnostics["selected_file_extractor_invoked"]))

    def test_tree_sitter_provider_scores_charge_additional_services_handler_above_generic_methods(self) -> None:
        with _RepoLocalTempDir("tree_sitter_additional_service") as root:
            target = root / "src" / "Telemart.Service" / "Application" / "Commands" / "MaintenanceCommands" / "ChargeAdditionalServicesHandler.cs"
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(
                "public class ChargeAdditionalServicesHandler { public void HandleAsync() {} public void Execute() {} public void ChargeAdditionalServices() {} }",
                encoding="utf-8",
            )
            result = TreeSitterSymbolProvider().provide(
                repo_id="sample",
                repo_context={"root_path": str(root)},
                current_candidate_files=["src/Telemart.Service/Application/Commands/MaintenanceCommands/ChargeAdditionalServicesHandler.cs"],
                task_text="charge additional services leftovers options",
            )

        self.assertTrue(result.query_succeeded)
        methods = [item.method_name for item in result.candidate_symbols[:3]]
        self.assertEqual(methods[0], "ChargeAdditionalServices")
        self.assertIn("HandleAsync", methods)
        self.assertGreaterEqual(result.diagnostics.get("symbol_penalty_applied", 0), 1)

    def test_symbol_promotion_adds_order_pack_cell_file_to_candidate_files(self) -> None:
        service = GroundingService(
            repo_profile_provider=_StaticRepoProfileProvider({"primary_stack": "C#, WPF"}),
            lexical_provider=LocalLexicalFileSearchProvider(),
            tree_sitter_provider=_StaticTreeSitterProvider(
                candidate_symbols=[
                    GroundingCandidateSymbol(
                        file_path="src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs",
                        class_name="OrderPackCellViewModel",
                        method_name="BuildRows",
                        symbol_name="OrderPackCellViewModel.BuildRows",
                        score=16.5,
                        provider="tree_sitter",
                    )
                ],
                diagnostics={
                    "symbol_score_breakdown": [
                        {
                            "file_path": "src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs",
                            "class_name": "OrderPackCellViewModel",
                            "method_name": "BuildRows",
                            "symbol_name": "OrderPackCellViewModel.BuildRows",
                            "score": 16.5,
                            "method_token_matches": ["pack", "cell"],
                            "class_token_matches": ["order", "pack", "cell"],
                            "file_token_matches": ["order", "pack", "cell"],
                        }
                    ]
                },
            ),
            embedding_provider=EmbeddingRecallProvider(),
            gitnexus_provider=_StaticGitNexusProvider(available=False, indexed=False),
        )

        context = service.build_planning_grounding(
            repo_id="sample",
            jira_task_payload={"task_text": "pack order into cells"},
            current_candidate_files=["src/client/Telemart.Client/ViewModels/Store/Order/OrderViewModel.cs"],
            repo_context={"repo_id": "sample", "root_path": ".", "resolved_symbols": {}},
        )

        self.assertIn(
            "src/client/Telemart.Client/ViewModels/Store/Order/OrderPackCellViewModel.cs",
            [item.path for item in context.candidate_files],
        )
        self.assertGreaterEqual(
            context.diagnostics.provider_statuses["symbol_promotion"]["promoted_from_symbols_count"],
            1,
        )

    def test_symbol_promotion_adds_streamline_worker_file_to_candidate_files(self) -> None:
        service = GroundingService(
            repo_profile_provider=_StaticRepoProfileProvider({"primary_stack": "C#"}),
            lexical_provider=LocalLexicalFileSearchProvider(),
            tree_sitter_provider=_StaticTreeSitterProvider(
                candidate_symbols=[
                    GroundingCandidateSymbol(
                        file_path="src/Workers/Telemart.Worker.Streamline/StreamlineWorker.cs",
                        class_name="StreamlineWorker",
                        method_name="QueryItemsAsync",
                        symbol_name="StreamlineWorker.QueryItemsAsync",
                        score=15.6,
                        provider="tree_sitter",
                    )
                ],
                diagnostics={
                    "symbol_score_breakdown": [
                        {
                            "file_path": "src/Workers/Telemart.Worker.Streamline/StreamlineWorker.cs",
                            "class_name": "StreamlineWorker",
                            "method_name": "QueryItemsAsync",
                            "symbol_name": "StreamlineWorker.QueryItemsAsync",
                            "score": 15.6,
                            "method_token_matches": ["item"],
                            "class_token_matches": ["streamline", "worker"],
                            "file_token_matches": ["streamline", "worker"],
                        }
                    ]
                },
            ),
            embedding_provider=EmbeddingRecallProvider(),
            gitnexus_provider=_StaticGitNexusProvider(available=False, indexed=False),
        )

        context = service.build_planning_grounding(
            repo_id="sample",
            jira_task_payload={"task_text": "streamline worker sync products"},
            current_candidate_files=["src/Parsers/Telemart.Parser.Diwave/Items/ProductItem.cs"],
            repo_context={"repo_id": "sample", "root_path": ".", "resolved_symbols": {}},
        )

        self.assertIn(
            "src/Workers/Telemart.Worker.Streamline/StreamlineWorker.cs",
            [item.path for item in context.candidate_files],
        )
        self.assertIn(
            "src/Workers/Telemart.Worker.Streamline/StreamlineWorker.cs",
            context.diagnostics.provider_statuses["symbol_promotion"]["promoted_files"],
        )

    def test_symbol_promotion_monobank_prefers_order_repository_over_handler_when_repository_storage_map_semantics_are_strong(self) -> None:
        with _RepoLocalTempDir("symbol_promotion_monobank_repository_balance") as root:
            (root / "src" / "Telemart.Service" / "Application" / "Commands" / "MonoBankCorpCommands").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Application" / "Commands" / "MonobankCommands").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Application" / "Processors" / "ExternalPaymentData").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Options").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Mappings").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Repositories" / "Interfaces").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Telemart.Service" / "Repositories").mkdir(parents=True, exist_ok=True)

            (root / "src" / "Telemart.Service" / "Application" / "Commands" / "MonoBankCorpCommands" / "MonobankCorpFillStatementsHandler.cs").write_text(
                "namespace Telemart.Service.Application.Commands.MonoBankCorpCommands; "
                "public class MonobankCorpFillStatementsHandler { "
                "public MonobankCorpFillStatementsHandler(IOrderRepository orderRepository, IExternalPaymentDataRepository externalPaymentRepository, object unitOfWork, object monobankCorpClient, object options, object paymentProvider, object logger) {} "
                "public string ParseExternalId(MonobankCorpStatementResponse response) => string.Empty; "
                "public async Task ExecuteAsync(MonobankCorpFillStatementsRequest request, CancellationToken cancellationToken) "
                "{ var externalOrderMap = new Dictionary<string, int>(); await Task.CompletedTask; } }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Application" / "Commands" / "MonoBankCorpCommands" / "MonobankCorpFillStatementsRequest.cs").write_text(
                "namespace Telemart.Service.Application.Commands.MonoBankCorpCommands; public class MonobankCorpFillStatementsRequest {}",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Application" / "Commands" / "MonobankCommands" / "MonobankFillStatementsHandler.cs").write_text(
                "namespace Telemart.Service.Application.Commands.MonobankCommands; public class MonobankFillStatementsHandler { public async Task ExecuteAsync() { await Task.CompletedTask; } }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Application" / "Processors" / "ExternalPaymentData" / "MonobankExternalPaymentDataProcessor.cs").write_text(
                "namespace Telemart.Service.Application.Processors.ExternalPaymentData; public class MonobankExternalPaymentDataProcessor { public string ProcessExternalPaymentData(string externalOrderId) => externalOrderId; }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Options" / "MonobankCorpExtOptions.cs").write_text(
                "namespace Telemart.Service.Options; public class MonobankCorpExtOptions { public string ApiKey { get; set; } = string.Empty; }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Mappings" / "ExternalOrderDataMap.cs").write_text(
                "namespace Telemart.Service.Mappings; public class ExternalOrderDataMap { public string ExternalOrderId { get; set; } = string.Empty; }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Repositories" / "Interfaces" / "IOrderRepository.cs").write_text(
                "namespace Telemart.Service.Repositories.Interfaces; public interface IOrderRepository { Dictionary<string, int> BuildExternalOrderMap(); }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Repositories" / "OrderRepository.cs").write_text(
                "namespace Telemart.Service.Repositories; public class OrderRepository : IOrderRepository { "
                "public Dictionary<string, int> BuildExternalOrderMap() => new(); "
                "public async Task UpdateExternalOrderMapAsync() { await Task.CompletedTask; } "
                "public string ExternalOrderId { get; set; } = string.Empty; }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Repositories" / "Interfaces" / "IExternalPaymentDataRepository.cs").write_text(
                "namespace Telemart.Service.Repositories.Interfaces; public interface IExternalPaymentDataRepository { string FindExternalPaymentData(); }",
                encoding="utf-8",
            )
            (root / "src" / "Telemart.Service" / "Repositories" / "ExternalPaymentDataRepository.cs").write_text(
                "namespace Telemart.Service.Repositories; public class ExternalPaymentDataRepository : IExternalPaymentDataRepository { public string FindExternalPaymentData() => string.Empty; }",
                encoding="utf-8",
            )

            service = GroundingService(
                repo_profile_provider=_StaticRepoProfileProvider({"primary_stack": "C#"}),
                lexical_provider=LocalLexicalFileSearchProvider(),
                owner_snippet_recall_provider=LocalOwnerSnippetRecallProvider(),
                family_recall_provider=FamilyAwareLocalRecallProvider(),
                local_code_recall_provider=LocalCodeRecallProvider(),
                tree_sitter_provider=TreeSitterSymbolProvider(),
                embedding_provider=EmbeddingRecallProvider(),
                gitnexus_provider=_StaticGitNexusProvider(available=False, indexed=False),
            )
            first = service.build_planning_grounding(
                repo_id="sample",
                jira_task_payload={"task_text": "monobank corp fill statements duplicate external order map repository"},
                current_candidate_files=[],
                repo_context={"repo_id": "sample", "root_path": str(root), "resolved_symbols": {}},
            )
            second = service.build_planning_grounding(
                repo_id="sample",
                jira_task_payload={"task_text": "monobank corp fill statements duplicate external order map repository"},
                current_candidate_files=[],
                repo_context={"repo_id": "sample", "root_path": str(root), "resolved_symbols": {}},
            )

        self.assertEqual(first.candidate_files[0].path, "src/Telemart.Service/Repositories/OrderRepository.cs")
        self.assertEqual(
            [item.path for item in first.candidate_files],
            [item.path for item in second.candidate_files],
        )
        promotion_rows = first.diagnostics.provider_statuses["symbol_promotion"]["symbol_to_file_promotions"]
        order_repo_row = next(item for item in promotion_rows if item["file_path"] == "src/Telemart.Service/Repositories/OrderRepository.cs")
        self.assertGreater(float(order_repo_row["monobank_repository_symbol_boost_applied"]), 0.0)
        handler_row = next(
            (
                item
                for item in promotion_rows
                if item["file_path"] == "src/Telemart.Service/Application/Commands/MonoBankCorpCommands/MonobankCorpFillStatementsHandler.cs"
            ),
            None,
        )
        if handler_row is not None:
            self.assertGreater(float(handler_row["monobank_handler_symbol_penalty_applied"]), 0.0)

    def test_symbol_promotion_monobank_refinement_leaves_streamline_family_stable(self) -> None:
        with _RepoLocalTempDir("symbol_promotion_monobank_refinement_streamline_guard") as root:
            (root / "src" / "Parsers" / "Telemart.Parser.Diwave" / "Items").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Workers" / "Telemart.Worker.Streamline").mkdir(parents=True, exist_ok=True)
            (root / "src" / "Parsers" / "Telemart.Parser.Diwave" / "Items" / "ProductItem.cs").write_text(
                "namespace Telemart.Parser.Diwave.Items; public class ProductItem { public string GetCategoryCode() => string.Empty; }",
                encoding="utf-8",
            )
            (root / "src" / "Workers" / "Telemart.Worker.Streamline" / "StreamlineWorker.cs").write_text(
                "namespace Telemart.Worker.Streamline; public class StreamlineWorker { public async Task ProcessItemImportAsync() { await Task.CompletedTask; } }",
                encoding="utf-8",
            )
            service = GroundingService(
                repo_profile_provider=_StaticRepoProfileProvider({"primary_stack": "C#"}),
                lexical_provider=LocalLexicalFileSearchProvider(),
                owner_snippet_recall_provider=LocalOwnerSnippetRecallProvider(),
                family_recall_provider=FamilyAwareLocalRecallProvider(),
                local_code_recall_provider=LocalCodeRecallProvider(),
                tree_sitter_provider=TreeSitterSymbolProvider(),
                embedding_provider=EmbeddingRecallProvider(),
                gitnexus_provider=_StaticGitNexusProvider(available=False, indexed=False),
            )
            first = service.build_planning_grounding(
                repo_id="sample",
                jira_task_payload={"task_text": "streamline worker item mapping product sync import"},
                current_candidate_files=[],
                repo_context={"repo_id": "sample", "root_path": str(root), "resolved_symbols": {}},
            )
            second = service.build_planning_grounding(
                repo_id="sample",
                jira_task_payload={"task_text": "streamline worker item mapping product sync import"},
                current_candidate_files=[],
                repo_context={"repo_id": "sample", "root_path": str(root), "resolved_symbols": {}},
            )

        self.assertEqual(first.candidate_files[0].path, "src/Workers/Telemart.Worker.Streamline/StreamlineWorker.cs")
        self.assertEqual(
            [item.path for item in first.candidate_files],
            [item.path for item in second.candidate_files],
        )

    def test_build_planning_grounding_exposes_file_scoped_grounded_methods(self) -> None:
        service = GroundingService(
            repo_profile_provider=_StaticRepoProfileProvider({"primary_stack": "Python"}),
            lexical_provider=_NoopLexicalProvider(),
            owner_snippet_recall_provider=_NoopOwnerSnippetRecallProvider(),
            family_recall_provider=_NoopFamilyRecallProvider(),
            local_code_recall_provider=_NoopLocalCodeRecallProvider(),
            tree_sitter_provider=_StaticTreeSitterProvider(
                candidate_symbols=[
                    GroundingCandidateSymbol(
                        file_path="agents/spec_agent.py",
                        class_name="SpecAgent",
                        method_name="run_spec_agent",
                        symbol_name="SpecAgent.run_spec_agent",
                        score=14.0,
                        provider="tree_sitter",
                    ),
                    GroundingCandidateSymbol(
                        file_path="agents/spec_agent.py",
                        class_name="SpecAgent",
                        method_name="format_spec_for_ui",
                        symbol_name="SpecAgent.format_spec_for_ui",
                        score=10.0,
                        provider="tree_sitter",
                    ),
                    GroundingCandidateSymbol(
                        file_path="other/file.py",
                        class_name="Other",
                        method_name="ignored_method",
                        symbol_name="Other.ignored_method",
                        score=99.0,
                        provider="tree_sitter",
                    ),
                ]
            ),
            embedding_provider=EmbeddingRecallProvider(),
            gitnexus_provider=_StaticGitNexusProvider(available=False, indexed=False),
        )

        context = service.build_planning_grounding(
            repo_id="sample",
            jira_task_payload={"task_text": "run spec agent patch generation"},
            current_candidate_files=["agents/spec_agent.py"],
            repo_context={"repo_id": "sample", "root_path": ".", "resolved_symbols": {}},
        )

        self.assertEqual(context.system_selected_file, "agents/spec_agent.py")
        self.assertEqual(
            [item.method_name for item in context.grounded_method_candidates],
            ["run_spec_agent", "format_spec_for_ui"],
        )
        self.assertEqual(context.grounded_classes_for_selected_file, ["SpecAgent"])
        self.assertEqual(
            context.grounded_methods_for_selected_file,
            ["run_spec_agent", "format_spec_for_ui"],
        )
        self.assertEqual(context.diagnostics.provider_statuses["method_grounding"]["grounded_method_count"], 2)
        self.assertTrue(context.diagnostics.provider_statuses["method_grounding"]["selected_file_has_grounded_methods"])

    def test_build_planning_grounding_extracts_selected_file_methods_even_without_merged_symbol_pool_hit(self) -> None:
        with _RepoLocalTempDir("selected_file_direct_grounding") as root:
            target = root / "src" / "Workers" / "Telemart.Worker.Streamline" / "StreamlineWorker.cs"
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(
                "public class StreamlineWorker { public async Task ExecuteAsync() { await Task.CompletedTask; } }",
                encoding="utf-8",
            )

            service = GroundingService(
                repo_profile_provider=_StaticRepoProfileProvider({"primary_stack": "C#"}),
                lexical_provider=LocalLexicalFileSearchProvider(),
                owner_snippet_recall_provider=_NoopOwnerSnippetRecallProvider(),
                family_recall_provider=_NoopFamilyRecallProvider(),
                local_code_recall_provider=_NoopLocalCodeRecallProvider(),
                tree_sitter_provider=TreeSitterSymbolProvider(),
                embedding_provider=EmbeddingRecallProvider(),
                gitnexus_provider=_StaticGitNexusProvider(available=False, indexed=False),
            )
            context = service.build_planning_grounding(
                repo_id="sample",
                jira_task_payload={"task_text": "streamline worker execute sync"},
                current_candidate_files=[],
                repo_context={"repo_id": "sample", "root_path": str(root), "resolved_symbols": {}},
            )

        self.assertEqual(context.system_selected_file, "src/Workers/Telemart.Worker.Streamline/StreamlineWorker.cs")
        self.assertEqual(context.grounded_classes_for_selected_file, ["StreamlineWorker"])
        self.assertIn("ExecuteAsync", context.grounded_methods_for_selected_file)
        method_grounding = context.diagnostics.provider_statuses["method_grounding"]
        self.assertEqual(method_grounding["selected_file_symbol_extraction_status"], "passed")
        self.assertTrue(method_grounding["selected_file_extractor_invoked"])
        self.assertGreater(method_grounding["selected_file_bytes_loaded"], 0)


if __name__ == "__main__":
    unittest.main()
