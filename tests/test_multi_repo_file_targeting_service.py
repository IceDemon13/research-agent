import shutil
import unittest
import uuid
import json
from pathlib import Path

from contracts.repo_index import RepoFileIndex, RepoFileIndexEntry, RepoGlossary, RepoGlossaryTerm, RepoProfile, RepoSymbol, RepoSymbolIndex
from services.multi_repo_file_targeting_service import MultiRepoFileTargetingService
from services.repo_registry import RepositoryRegistryService


class _FakeHistoricalMemory:
    def __init__(self, *, changes=None, tasks=None) -> None:
        self._changes = list(changes or [])
        self._tasks = list(tasks or [])

    def list_historical_changes(self):
        return list(self._changes)

    def list_task_snapshots(self):
        return list(self._tasks)


class _FakeSurvivingMemory:
    def __init__(self, *, snippets=None) -> None:
        self._snippets = list(snippets or [])

    def list_surviving_snippets(self, *, repo_id: str = "", jira_key: str = ""):
        repo_filter = str(repo_id or "").strip().lower()
        jira_filter = str(jira_key or "").strip().upper()
        items = list(self._snippets)
        if repo_filter:
            items = [item for item in items if str(item.get("repo_id", "")).strip().lower() == repo_filter]
        if jira_filter:
            items = [item for item in items if str(item.get("jira_key", "")).strip().upper() == jira_filter]
        return items


class _FakeIndexService:
    def __init__(self, *, profiles=None, glossaries=None, symbol_indexes=None, file_indexes=None) -> None:
        self._profiles = dict(profiles or {})
        self._glossaries = dict(glossaries or {})
        self._symbol_indexes = dict(symbol_indexes or {})
        self._file_indexes = dict(file_indexes or {})

    def get_repo_profile(self, repo_id: str):
        return self._profiles.get(repo_id)

    def get_glossary(self, repo_id: str):
        return self._glossaries.get(repo_id)

    def get_symbol_index(self, repo_id: str):
        return self._symbol_indexes.get(repo_id)

    def get_file_index(self, repo_id: str):
        return self._file_indexes.get(repo_id)


class MultiRepoFileTargetingServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"file-targeting-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.benchmark_confusions_path = self.workspace_root / "artifacts" / "routing_benchmarks" / "benchmark_confusions_latest.json"
        self.registry = RepositoryRegistryService(storage_path=self.registry_path)
        for repo_id in ("catalog_service", "pricing_service", "commerce_service", "client_portal", "telemart_service_test"):
            repo_root = self.workspace_root / repo_id
            (repo_root / ".git").mkdir(parents=True, exist_ok=True)
            self.registry.register_repo(
                root_path=str(repo_root),
                repo_id=repo_id,
                display_name=repo_id.replace("_", " ").title(),
                default_branch="main",
            )
        self.registry.update_repo_metadata("catalog_service", capability_tags=["accessories", "product", "catalog"])
        self.registry.update_repo_metadata("pricing_service", capability_tags=["pricing", "discounts"])
        self.registry.update_repo_metadata("commerce_service", capability_tags=["tradein", "quotas", "warehouse", "credit", "assembly"])
        self.registry.update_repo_metadata("client_portal", capability_tags=["assembly", "ui", "client"])
        self.registry.update_repo_metadata("telemart_service_test", capability_tags=["assembly", "order", "product", "report", "repository", "dto"])

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def _write_repo_knowledge_pack(self, repo_id: str, payload: dict) -> None:
        target = self.workspace_root / "artifacts" / "repo_knowledge" / repo_id
        target.mkdir(parents=True, exist_ok=True)
        (target / "repo_profile.json").write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")

    def _service(self) -> MultiRepoFileTargetingService:
        historical = _FakeHistoricalMemory(
            tasks=[
                {
                    "jira_key": "TEL-7154",
                    "task_snapshot_text": "Return accessories field in product card response",
                    "normalized_task_text": "return accessories field in product card response",
                }
            ],
            changes=[
                {
                    "jira_key": "TEL-7154",
                    "repo_id": "catalog_service",
                    "changed_files": [
                        "src/Features/Product/QueryProductInfoHandler.cs",
                        "src/External/MainClient.cs",
                        "tests/Telemart.Catalog.Service.Tests/Telemart.Catalog.Service.Tests.csproj",
                    ],
                },
                {
                    "jira_key": "TEL-7154",
                    "repo_id": "pricing_service",
                    "changed_files": [
                        "src/Pricing/Contracts/ProductAccessoryPriceDto.cs",
                        "src/Pricing/Handlers/BuildAccessoryPriceResponse.cs",
                    ],
                },
            ],
        )
        surviving = _FakeSurvivingMemory(
            snippets=[
                {
                    "repo_id": "catalog_service",
                    "jira_key": "TEL-7154",
                    "file_path": "src/Features/Product/QueryProductInfoHandler.cs",
                    "symbol_name": "QueryProductInfoHandler",
                    "snippet_text": "return accessories response field for product card",
                },
                {
                    "repo_id": "catalog_service",
                    "jira_key": "TEL-7154",
                    "file_path": "src/External/MainClient.cs",
                    "symbol_name": "MainClient",
                    "snippet_text": "product query request response accessories",
                },
                {
                    "repo_id": "pricing_service",
                    "jira_key": "TEL-7154",
                    "file_path": "src/Pricing/Handlers/BuildAccessoryPriceResponse.cs",
                    "symbol_name": "BuildAccessoryPriceResponse",
                    "snippet_text": "accessory price response dto",
                },
            ]
        )
        index = _FakeIndexService(
            profiles={
                "catalog_service": RepoProfile(repo_id="catalog_service", indexed_at="2026-03-25T10:00:00+00:00", source_roots=["src"], test_projects=["tests/Telemart.Catalog.Service.Tests/Telemart.Catalog.Service.Tests.csproj"]),
                "pricing_service": RepoProfile(repo_id="pricing_service", indexed_at="2026-03-25T10:00:00+00:00", source_roots=["src"]),
            },
            glossaries={
                "catalog_service": RepoGlossary(repo_id="catalog_service", indexed_at="2026-03-25T10:00:00+00:00", terms=[RepoGlossaryTerm(term="accessories", confidence=0.9), RepoGlossaryTerm(term="product", confidence=0.9)]),
                "pricing_service": RepoGlossary(repo_id="pricing_service", indexed_at="2026-03-25T10:00:00+00:00", terms=[RepoGlossaryTerm(term="accessories", confidence=0.6), RepoGlossaryTerm(term="price", confidence=0.8)]),
            },
            symbol_indexes={
                "catalog_service": RepoSymbolIndex(
                    repo_id="catalog_service",
                    indexed_at="2026-03-25T10:00:00+00:00",
                    symbols=[
                        RepoSymbol(name="QueryProductInfoHandler", kind="class", file_path="src/Features/Product/QueryProductInfoHandler.cs"),
                        RepoSymbol(name="MainClient", kind="class", file_path="src/External/MainClient.cs"),
                        RepoSymbol(name="ValidatorActionFilter", kind="class", file_path="src/Filters/ValidatorActionFilter.cs"),
                    ],
                ),
                "pricing_service": RepoSymbolIndex(
                    repo_id="pricing_service",
                    indexed_at="2026-03-25T10:00:00+00:00",
                    symbols=[
                        RepoSymbol(name="BuildAccessoryPriceResponse", kind="class", file_path="src/Pricing/Handlers/BuildAccessoryPriceResponse.cs"),
                    ],
                ),
            },
            file_indexes={
                "catalog_service": RepoFileIndex(
                    repo_id="catalog_service",
                    root_path="catalog_service",
                    indexed_at="2026-03-25T10:00:00+00:00",
                    file_count=4,
                    files=[
                        RepoFileIndexEntry(repo_id="catalog_service", relative_path="src/Features/Product/QueryProductInfoHandler.cs", language="csharp", file_size=1, content_hash="a", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="catalog_service", relative_path="src/External/MainClient.cs", language="csharp", file_size=1, content_hash="b", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="catalog_service", relative_path="src/Filters/ValidatorActionFilter.cs", language="csharp", file_size=1, content_hash="c", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="catalog_service", relative_path="tests/Telemart.Catalog.Service.Tests/Telemart.Catalog.Service.Tests.csproj", language="xml", file_size=1, content_hash="d", last_indexed_at="x"),
                    ],
                ),
                "pricing_service": RepoFileIndex(
                    repo_id="pricing_service",
                    root_path="pricing_service",
                    indexed_at="2026-03-25T10:00:00+00:00",
                    file_count=2,
                    files=[
                        RepoFileIndexEntry(repo_id="pricing_service", relative_path="src/Pricing/Contracts/ProductAccessoryPriceDto.cs", language="csharp", file_size=1, content_hash="e", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="pricing_service", relative_path="src/Pricing/Handlers/BuildAccessoryPriceResponse.cs", language="csharp", file_size=1, content_hash="f", last_indexed_at="x"),
                    ],
                ),
            },
        )
        return MultiRepoFileTargetingService(
            registry_service=self.registry,
            historical_change_memory_service=historical,
            surviving_code_memory_service=surviving,
            index_service=index,
            benchmark_confusions_path=self.benchmark_confusions_path,
        )

    def _service_for_regression_cases(self) -> MultiRepoFileTargetingService:
        self.benchmark_confusions_path.parent.mkdir(parents=True, exist_ok=True)
        self.benchmark_confusions_path.write_text(
            json.dumps(
                {
                    "per_repo_confusions": {
                        "commerce_service": {
                            "confusing_generic_files": [
                                {"file": "src/Services/OrderService.cs", "count": 9},
                                {"file": "src/Services/ServiceRequestService.cs", "count": 7},
                                {"file": "src/Business/BusinessOperation.cs", "count": 6},
                                {"file": "src/Startup.cs", "count": 5},
                            ],
                            "common_missing_expected_files": [
                                {"file": "src/TradeIn/Controllers/TradeInController.cs", "count": 8},
                                {"file": "src/Warehouse/Handlers/GetWarehouseQuotaHandler.cs", "count": 7},
                                {"file": "src/Credit/Responses/CreditOfferResponse.cs", "count": 6},
                            ],
                        },
                        "client_portal": {
                            "confusing_generic_files": [
                                {"file": "src/ViewModels/OrderViewModel.cs", "count": 5},
                            ],
                            "common_missing_expected_files": [
                                {"file": "src/Assembly/TransferObjects/AssemblyTransferObject.cs", "count": 6},
                            ],
                        },
                    }
                },
                ensure_ascii=False,
                indent=2,
            ),
            encoding="utf-8",
        )
        historical = _FakeHistoricalMemory(
            tasks=[
                {"jira_key": "TEL-TRADEIN", "task_snapshot_text": "TradeIn API returns enriched offer payload"},
                {"jira_key": "TEL-QUOTAS", "task_snapshot_text": "Quota read endpoint returns warehouse quotas"},
                {"jira_key": "TEL-CREDIT", "task_snapshot_text": "Credit offers API returns response DTO"},
                {"jira_key": "TEL-ASSEMBLY", "task_snapshot_text": "Assembly UI shows new transfer object fields"},
            ],
            changes=[
                {
                    "jira_key": "TEL-TRADEIN",
                    "repo_id": "commerce_service",
                    "changed_files": [
                        "src/TradeIn/Controllers/TradeInController.cs",
                        "src/TradeIn/Commands/CreateTradeInCommand.cs",
                    ],
                },
                {
                    "jira_key": "TEL-QUOTAS",
                    "repo_id": "commerce_service",
                    "changed_files": [
                        "src/Warehouse/Handlers/GetWarehouseQuotaHandler.cs",
                        "src/Warehouse/Requests/GetWarehouseQuotaRequest.cs",
                    ],
                },
                {
                    "jira_key": "TEL-CREDIT",
                    "repo_id": "commerce_service",
                    "changed_files": [
                        "src/Credit/Responses/CreditOfferResponse.cs",
                        "src/Credit/Dto/CreditOfferDto.cs",
                    ],
                },
                {
                    "jira_key": "TEL-ASSEMBLY",
                    "repo_id": "client_portal",
                    "changed_files": [
                        "src/Assembly/ViewModels/AssemblyViewModel.cs",
                        "src/Assembly/TransferObjects/AssemblyTransferObject.cs",
                    ],
                },
            ],
        )
        surviving = _FakeSurvivingMemory(
            snippets=[
                {
                    "repo_id": "commerce_service",
                    "jira_key": "TEL-TRADEIN",
                    "file_path": "src/TradeIn/Controllers/TradeInController.cs",
                    "symbol_name": "TradeInController",
                    "snippet_text": "tradein controller returns enriched offer response",
                },
                {
                    "repo_id": "commerce_service",
                    "jira_key": "TEL-QUOTAS",
                    "file_path": "src/Warehouse/Handlers/GetWarehouseQuotaHandler.cs",
                    "symbol_name": "GetWarehouseQuotaHandler",
                    "snippet_text": "warehouse quota response read handler",
                },
                {
                    "repo_id": "commerce_service",
                    "jira_key": "TEL-CREDIT",
                    "file_path": "src/Credit/Responses/CreditOfferResponse.cs",
                    "symbol_name": "CreditOfferResponse",
                    "snippet_text": "credit offer response dto payload",
                },
                {
                    "repo_id": "client_portal",
                    "jira_key": "TEL-ASSEMBLY",
                    "file_path": "src/Assembly/TransferObjects/AssemblyTransferObject.cs",
                    "symbol_name": "AssemblyTransferObject",
                    "snippet_text": "assembly ui transfer object fields for view rendering",
                },
            ]
        )
        index = _FakeIndexService(
            profiles={
                "commerce_service": RepoProfile(repo_id="commerce_service", indexed_at="2026-03-25T10:00:00+00:00", source_roots=["src"]),
                "client_portal": RepoProfile(repo_id="client_portal", indexed_at="2026-03-25T10:00:00+00:00", source_roots=["src"]),
            },
            glossaries={
                "commerce_service": RepoGlossary(repo_id="commerce_service", indexed_at="2026-03-25T10:00:00+00:00", terms=[
                    RepoGlossaryTerm(term="tradein", confidence=0.9),
                    RepoGlossaryTerm(term="quotas", confidence=0.9),
                    RepoGlossaryTerm(term="warehouse", confidence=0.9),
                    RepoGlossaryTerm(term="credit", confidence=0.9),
                ]),
                "client_portal": RepoGlossary(repo_id="client_portal", indexed_at="2026-03-25T10:00:00+00:00", terms=[
                    RepoGlossaryTerm(term="assembly", confidence=0.9),
                    RepoGlossaryTerm(term="ui", confidence=0.8),
                ]),
            },
            file_indexes={
                "commerce_service": RepoFileIndex(
                    repo_id="commerce_service",
                    root_path="commerce_service",
                    indexed_at="2026-03-25T10:00:00+00:00",
                    file_count=12,
                    files=[
                        RepoFileIndexEntry(repo_id="commerce_service", relative_path="src/TradeIn/Controllers/TradeInController.cs", language="csharp", file_size=1, content_hash="1", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="commerce_service", relative_path="src/TradeIn/Commands/CreateTradeInCommand.cs", language="csharp", file_size=1, content_hash="2", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="commerce_service", relative_path="src/Warehouse/Handlers/GetWarehouseQuotaHandler.cs", language="csharp", file_size=1, content_hash="3", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="commerce_service", relative_path="src/Warehouse/Requests/GetWarehouseQuotaRequest.cs", language="csharp", file_size=1, content_hash="4", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="commerce_service", relative_path="src/Credit/Responses/CreditOfferResponse.cs", language="csharp", file_size=1, content_hash="5", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="commerce_service", relative_path="src/Credit/Dto/CreditOfferDto.cs", language="csharp", file_size=1, content_hash="6", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="commerce_service", relative_path="src/Services/OrderService.cs", language="csharp", file_size=1, content_hash="7", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="commerce_service", relative_path="src/Services/ServiceRequestService.cs", language="csharp", file_size=1, content_hash="8", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="commerce_service", relative_path="src/Business/BusinessOperation.cs", language="csharp", file_size=1, content_hash="9", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="commerce_service", relative_path="src/Startup.cs", language="csharp", file_size=1, content_hash="10", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="commerce_service", relative_path="src/appsettings.json", language="json", file_size=1, content_hash="11", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="commerce_service", relative_path="src/Commerce.Service.csproj", language="xml", file_size=1, content_hash="12", last_indexed_at="x"),
                    ],
                ),
                "client_portal": RepoFileIndex(
                    repo_id="client_portal",
                    root_path="client_portal",
                    indexed_at="2026-03-25T10:00:00+00:00",
                    file_count=6,
                    files=[
                        RepoFileIndexEntry(repo_id="client_portal", relative_path="src/Assembly/ViewModels/AssemblyViewModel.cs", language="csharp", file_size=1, content_hash="13", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="client_portal", relative_path="src/Assembly/TransferObjects/AssemblyTransferObject.cs", language="csharp", file_size=1, content_hash="14", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="client_portal", relative_path="src/ViewModels/OrderViewModel.cs", language="csharp", file_size=1, content_hash="15", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="client_portal", relative_path="src/Services/ServiceRequestService.cs", language="csharp", file_size=1, content_hash="16", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="client_portal", relative_path="src/appsettings.json", language="json", file_size=1, content_hash="17", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="client_portal", relative_path="src/Telemart.Client.csproj", language="xml", file_size=1, content_hash="18", last_indexed_at="x"),
                    ],
                ),
            },
        )
        return MultiRepoFileTargetingService(
            registry_service=self.registry,
            historical_change_memory_service=historical,
            surviving_code_memory_service=surviving,
            index_service=index,
            benchmark_confusions_path=self.benchmark_confusions_path,
        )

    def _service_for_telemart_failures(self) -> MultiRepoFileTargetingService:
        self.benchmark_confusions_path.parent.mkdir(parents=True, exist_ok=True)
        self.benchmark_confusions_path.write_text(
            json.dumps(
                {
                    "per_repo_confusions": {
                        "telemart_service_test": {
                            "confusing_generic_files": [
                                {"file": "src/Telemart.Service/Services/OrderService.cs", "count": 20},
                                {"file": "src/Telemart.Service/Services/ServiceRequestService.cs", "count": 17},
                                {"file": "src/Telemart.Service/Startup.cs", "count": 12},
                                {"file": "src/Telemart.Service/Telemart.Service.csproj", "count": 14},
                                {"file": "src/Telemart.Service/AutoMappings/Profiles/AutoMappingProfile.cs", "count": 11},
                                {"file": "src/Telemart.Service/Controllers/OrdersController.cs", "count": 8},
                                {"file": "src/Telemart.Service/Controllers/TradeInsController.cs", "count": 7},
                            ],
                            "common_missing_expected_files": [
                                {"file": "src/Telemart.Service/Application/Notifications/AssemblyServices/CreateAssemblyServicesHandler.cs", "count": 9},
                                {"file": "src/Telemart.Service/Application/Commands/AssemblyServiceCommands/CompleteAssemblyHandler.cs", "count": 9},
                                {"file": "src/Telemart.Service/Repositories/OrderRepository.cs", "count": 8},
                                {"file": "src/Telemart.Service/Application/Commands/ReportCommands/GetOrderAssemblyReportHandler.cs", "count": 7},
                                {"file": "src/Telemart.Service/Repositories/ProductRepository.cs", "count": 7},
                                {"file": "src/Telemart.Service/DataTransferObjects/ProductLookupDto.cs", "count": 6},
                            ],
                        }
                    }
                },
                ensure_ascii=False,
                indent=2,
            ),
            encoding="utf-8",
        )
        historical = _FakeHistoricalMemory(
            tasks=[
                {"jira_key": "TEL-10000", "task_snapshot_text": "Populates tm_assembly_service.id_product when creating an assembly with folder type tm_order_folder.id_type = 1 Steps to reproduce: Open Telemart.Client, open orders, click approve. Notes: remove tm_assembly_service.id_product fill during create assembly flow."},
                {"jira_key": "TEL-10002", "task_snapshot_text": "The api/v1/maintenance/orders/actions/purchase method returns a quantity of zero for items. Steps to reproduce: call the method. Expected result: rows with zero quantities are absent."},
                {"jira_key": "TEL-10005", "task_snapshot_text": "The computer name does not appear on the printed form of the assembly letter for orders in which the assembly is in a status other than Completed. Steps to reproduce: open Telemart.Client and print assembly letter."},
                {"jira_key": "TEL-10179", "task_snapshot_text": "Values for group_name and id_group_feature are not saved in ps_chili_product_new when saving a new nomenclature. Steps to reproduce: open Telemart.Client, add nomenclature, save and reopen."},
                {"jira_key": "TEL-DTO", "task_snapshot_text": "API response contract should expose product lookup DTO fields"},
            ],
            changes=[
                {"jira_key": "TEL-10000", "repo_id": "telemart_service_test", "changed_files": [
                    "src/Telemart.Service/Application/Notifications/AssemblyServices/CreateAssemblyServicesHandler.cs",
                    "src/Telemart.Service/Application/Commands/AssemblyServiceCommands/CompleteAssemblyHandler.cs",
                ]},
                {"jira_key": "TEL-10002", "repo_id": "telemart_service_test", "changed_files": [
                    "src/Telemart.Service/Repositories/OrderRepository.cs",
                ]},
                {"jira_key": "TEL-10005", "repo_id": "telemart_service_test", "changed_files": [
                    "src/Telemart.Service/Application/Commands/ReportCommands/GetOrderAssemblyReportHandler.cs",
                ]},
                {"jira_key": "TEL-10179", "repo_id": "telemart_service_test", "changed_files": [
                    "src/Telemart.Service/Repositories/ProductRepository.cs",
                ]},
                {"jira_key": "TEL-DTO", "repo_id": "telemart_service_test", "changed_files": [
                    "src/Telemart.Service/DataTransferObjects/ProductLookupDto.cs",
                ]},
            ],
        )
        surviving = _FakeSurvivingMemory(
            snippets=[
                {
                    "repo_id": "telemart_service_test",
                    "jira_key": "TEL-10000",
                    "file_path": "src/Telemart.Service/Application/Notifications/AssemblyServices/CreateAssemblyServicesHandler.cs",
                    "symbol_name": "CreateAssemblyServicesHandler",
                    "snippet_text": "assembly services notification workflow create complete status",
                },
                {
                    "repo_id": "telemart_service_test",
                    "jira_key": "TEL-10000",
                    "file_path": "src/Telemart.Service/Application/Commands/AssemblyServiceCommands/CompleteAssemblyHandler.cs",
                    "symbol_name": "CompleteAssemblyHandler",
                    "snippet_text": "complete assembly command process handler",
                },
                {
                    "repo_id": "telemart_service_test",
                    "jira_key": "TEL-10002",
                    "file_path": "src/Telemart.Service/Repositories/OrderRepository.cs",
                    "symbol_name": "OrderRepository",
                    "snippet_text": "order repository query filters fetch order lookup",
                },
                {
                    "repo_id": "telemart_service_test",
                    "jira_key": "TEL-10005",
                    "file_path": "src/Telemart.Service/Application/Commands/ReportCommands/GetOrderAssemblyReportHandler.cs",
                    "symbol_name": "GetOrderAssemblyReportHandler",
                    "snippet_text": "order assembly report command handler build report",
                },
                {
                    "repo_id": "telemart_service_test",
                    "jira_key": "TEL-10179",
                    "file_path": "src/Telemart.Service/Repositories/ProductRepository.cs",
                    "symbol_name": "ProductRepository",
                    "snippet_text": "product repository lookup filter fetch product",
                },
                {
                    "repo_id": "telemart_service_test",
                    "jira_key": "TEL-DTO",
                    "file_path": "src/Telemart.Service/DataTransferObjects/ProductLookupDto.cs",
                    "symbol_name": "ProductLookupDto",
                    "snippet_text": "product lookup dto response contract payload",
                },
            ],
        )
        index = _FakeIndexService(
            profiles={
                "telemart_service_test": RepoProfile(repo_id="telemart_service_test", indexed_at="2026-03-25T10:00:00+00:00", source_roots=["src"]),
            },
            glossaries={
                "telemart_service_test": RepoGlossary(repo_id="telemart_service_test", indexed_at="2026-03-25T10:00:00+00:00", terms=[
                    RepoGlossaryTerm(term="assembly", confidence=0.9),
                    RepoGlossaryTerm(term="repository", confidence=0.9),
                    RepoGlossaryTerm(term="product", confidence=0.9),
                    RepoGlossaryTerm(term="report", confidence=0.9),
                    RepoGlossaryTerm(term="order", confidence=0.8),
                    RepoGlossaryTerm(term="dto", confidence=0.8),
                ]),
            },
            file_indexes={
                "telemart_service_test": RepoFileIndex(
                    repo_id="telemart_service_test",
                    root_path="telemart_service_test",
                    indexed_at="2026-03-25T10:00:00+00:00",
                    file_count=15,
                    files=[
                        RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Application/Notifications/AssemblyServices/CreateAssemblyServicesHandler.cs", language="csharp", file_size=1, content_hash="21", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Application/Commands/AssemblyServiceCommands/CompleteAssemblyHandler.cs", language="csharp", file_size=1, content_hash="22", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Repositories/OrderRepository.cs", language="csharp", file_size=1, content_hash="23", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Application/Commands/ReportCommands/GetOrderAssemblyReportHandler.cs", language="csharp", file_size=1, content_hash="24", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Repositories/ProductRepository.cs", language="csharp", file_size=1, content_hash="25", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/DataTransferObjects/ProductLookupDto.cs", language="csharp", file_size=1, content_hash="26", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Services/OrderService.cs", language="csharp", file_size=1, content_hash="27", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Services/ServiceRequestService.cs", language="csharp", file_size=1, content_hash="28", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Startup.cs", language="csharp", file_size=1, content_hash="29", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Telemart.Service.csproj", language="xml", file_size=1, content_hash="30", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/AutoMappings/Profiles/AutoMappingProfile.cs", language="csharp", file_size=1, content_hash="31", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Controllers/OrdersController.cs", language="csharp", file_size=1, content_hash="32", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Controllers/TradeInsController.cs", language="csharp", file_size=1, content_hash="33", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Application/Commands/CartCommands/GetCartHandler.cs", language="csharp", file_size=1, content_hash="34", last_indexed_at="x"),
                        RepoFileIndexEntry(repo_id="telemart_service_test", relative_path="src/Telemart.Service/Application/ProductSources/ProductSourceRepository.cs", language="csharp", file_size=1, content_hash="35", last_indexed_at="x"),
                    ],
                ),
            },
        )
        return MultiRepoFileTargetingService(
            registry_service=self.registry,
            historical_change_memory_service=historical,
            surviving_code_memory_service=surviving,
            index_service=index,
            benchmark_confusions_path=self.benchmark_confusions_path,
        )

    def test_single_repo_targeting_prefers_surviving_domain_files_and_penalizes_tests(self) -> None:
        service = self._service()

        result = service.build_targets(
            workflow_type="implementation_plan",
            task_text="Return accessories field in product card response",
            selected_repos=[{"repo_id": "catalog_service"}],
            jira_key="TEL-7154",
            provider_payload_by_repo={
                "catalog_service": {
                    "likely_file_details": [
                        {"name": "src/Features/Product/QueryProductInfoHandler.cs", "confidence": 0.85, "reason": "provider handler match"},
                        {"name": "tests/Telemart.Catalog.Service.Tests/Telemart.Catalog.Service.Tests.csproj", "confidence": 0.92, "reason": "test project"},
                    ],
                    "top_candidate_symbols": [
                        {"name": "QueryProductInfoHandler", "confidence": 0.82, "reason": "provider symbol"},
                        {"name": "Handle", "confidence": 0.95, "reason": "generic symbol"},
                    ],
                }
            },
        )

        selected = result["selected_files_by_repo"]["catalog_service"]
        candidates = result["candidate_files_by_repo"]["catalog_service"]
        self.assertTrue(selected)
        self.assertEqual(selected[0]["file"], "src/Features/Product/QueryProductInfoHandler.cs")
        self.assertNotEqual(candidates[0]["file"], "tests/Telemart.Catalog.Service.Tests/Telemart.Catalog.Service.Tests.csproj")
        diagnostics = result["candidate_diagnostics_by_repo"]["catalog_service"]
        self.assertGreater(diagnostics["candidate_count_before_dedupe"], diagnostics["candidate_count_after_dedupe"])
        self.assertIn("filename_recall", diagnostics["recall_source_summary"])
        self.assertGreaterEqual(diagnostics["final_pool_size_per_repo"], len(result["candidate_files_by_repo"]["catalog_service"]))
        self.assertGreaterEqual(diagnostics["dropped_generic_candidates_count"], 0)
        self.assertIn("filename_recall", candidates[0]["recall_channels"])
        self.assertIn("product", result["extracted_entities"])
        self.assertTrue(result["inferred_task_families"])
        self.assertIn("product", candidates[0]["matched_understanding_entities"])
        symbol_names = [item["name"] for item in result["top_candidate_symbols_by_repo"]["catalog_service"]]
        self.assertIn("QueryProductInfoHandler", symbol_names)
        self.assertNotIn("Handle", symbol_names)

    def test_repository_candidate_is_recalled_even_when_provider_is_weak(self) -> None:
        service = self._service_for_telemart_failures()

        result = service.build_targets(
            workflow_type="implementation_plan",
            task_text="The api/v1/maintenance/orders/actions/purchase method returns a quantity of zero for items. Expected result: rows with zero quantities are absent.",
            selected_repos=[{"repo_id": "telemart_service_test"}],
            jira_key="TEL-10002",
            provider_payload_by_repo={
                "telemart_service_test": {
                    "likely_file_details": [
                        {"name": "src/Telemart.Service/Services/OrderService.cs", "confidence": 0.95, "reason": "generic provider file"},
                    ]
                }
            },
        )

        order_repo = next(item for item in result["candidate_files_by_repo"]["telemart_service_test"] if item["file"] == "src/Telemart.Service/Repositories/OrderRepository.cs")
        diagnostics = result["candidate_diagnostics_by_repo"]["telemart_service_test"]
        self.assertGreaterEqual(diagnostics["candidates_dropped_by_admission_gate"], 0)
        self.assertIn("filename_recall", order_repo["recall_channels"])
        self.assertTrue(any(channel in order_repo["recall_channels"] for channel in ("historical_recall", "surviving_recall", "symbol_recall")))

    def test_repo_knowledge_only_enriches_recall_and_does_not_add_direct_scoring_prior(self) -> None:
        self._write_repo_knowledge_pack(
            "telemart_service_test",
            {
                "entity_vocabulary": ["ProductsCatalog", "CashboxResolver", "WarehouseRepository"],
                "feature_areas": [{"area": "Warehouse/Resolvers"}],
                "task_to_path_hints": {
                    "repository_query": {
                        "preferred_path_families": ["/Repositories/", "/Resolvers/"],
                        "preferred_suffixes": ["Repository", "Resolver"],
                    }
                },
                "common_true_positive_paths": [{"value": "src/Telemart.Service/Repositories/ProductRepository.cs"}],
                "common_false_positive_paths": [{"value": "src/Telemart.Service/Services/OrderService.cs"}],
            },
        )
        service = self._service_for_telemart_failures()

        result = service.build_targets(
            workflow_type="implementation_plan",
            task_text="Warehouse repository query should return productscatalog rows from cashbox resolver flow.",
            selected_repos=[{"repo_id": "telemart_service_test"}],
            jira_key="TEL-ENTITY",
            provider_payload_by_repo={
                "telemart_service_test": {
                    "likely_file_details": [
                        {"name": "src/Telemart.Service/Repositories/ProductRepository.cs", "confidence": 0.62, "reason": "repository match"},
                        {"name": "src/Telemart.Service/Services/OrderService.cs", "confidence": 0.91, "reason": "generic service"},
                    ]
                }
            },
        )

        diagnostics = result["candidate_diagnostics_by_repo"]["telemart_service_test"]
        top_candidate = result["candidate_files_by_repo"]["telemart_service_test"][0]
        self.assertTrue(result["repo_knowledge_used_for_enrichment"])
        self.assertTrue(diagnostics["repo_knowledge_used_for_enrichment"])
        self.assertTrue(diagnostics["enriched_entities_added"])
        self.assertEqual(top_candidate["repo_knowledge_bonus"], 0.0)
        self.assertEqual(top_candidate["repo_knowledge_penalty"], 0.0)

    def test_multi_repo_targeting_keeps_separate_shortlists_per_repo(self) -> None:
        service = self._service()

        result = service.build_targets(
            workflow_type="implementation_plan",
            task_text="Return accessories field in product card response",
            selected_repos=[{"repo_id": "catalog_service"}, {"repo_id": "pricing_service"}],
            jira_key="TEL-7154",
        )

        self.assertIn("catalog_service", result["selected_files_by_repo"])
        self.assertIn("pricing_service", result["selected_files_by_repo"])
        self.assertIn("src/Features/Product/QueryProductInfoHandler.cs", [item["file"] for item in result["selected_files_by_repo"]["catalog_service"]])
        self.assertIn("src/Pricing/Handlers/BuildAccessoryPriceResponse.cs", [item["file"] for item in result["selected_files_by_repo"]["pricing_service"]])
        self.assertGreaterEqual(result["total_selected_file_count"], 2)
        self.assertEqual(result["repo_file_match_quality_by_repo"]["catalog_service"], "exact")

    def test_explicit_test_task_signal_removes_test_penalty(self) -> None:
        service = self._service()

        result = service.build_targets(
            workflow_type="pre_review",
            task_text="Update tests for accessories product response",
            selected_repos=[{"repo_id": "catalog_service"}],
            jira_key="TEL-7154",
            provider_payload_by_repo={
                "catalog_service": {
                    "likely_file_details": [
                        {"name": "tests/Telemart.Catalog.Service.Tests/Telemart.Catalog.Service.Tests.csproj", "confidence": 0.92, "reason": "test project"},
                    ]
                }
            },
        )

        candidates = result["candidate_files_by_repo"]["catalog_service"]
        diagnostics = result["candidate_diagnostics_by_repo"]["catalog_service"]
        test_project = next(item for item in candidates if item["file"].endswith(".csproj"))
        self.assertEqual(test_project["test_penalty"], 0.0)
        self.assertNotIn("test", test_project["triggered_penalties"])

    def test_domain_cases_prefer_feature_files_over_generic_service_and_config_noise(self) -> None:
        service = self._service_for_regression_cases()
        scenarios = [
            {
                "jira_key": "TEL-TRADEIN",
                "repo_id": "commerce_service",
                "task_text": "TradeIn API should return enriched offer response and request fields",
                "expected_top": "src/TradeIn/Controllers/TradeInController.cs",
                "generic_files": [
                    "src/Services/OrderService.cs",
                    "src/appsettings.json",
                    "src/Business/BusinessOperation.cs",
                ],
            },
            {
                "jira_key": "TEL-QUOTAS",
                "repo_id": "commerce_service",
                "task_text": "Warehouse quotas read endpoint should return filtered quota response",
                "expected_top": "src/Warehouse/Handlers/GetWarehouseQuotaHandler.cs",
                "generic_files": [
                    "src/Services/ServiceRequestService.cs",
                    "src/Business/BusinessOperation.cs",
                ],
            },
            {
                "jira_key": "TEL-CREDIT",
                "repo_id": "commerce_service",
                "task_text": "Credit offers response should expose new DTO fields",
                "expected_top": "src/Credit/Responses/CreditOfferResponse.cs",
                "generic_files": [
                    "src/Services/OrderService.cs",
                    "src/Commerce.Service.csproj",
                ],
            },
            {
                "jira_key": "TEL-ASSEMBLY",
                "repo_id": "client_portal",
                "task_text": "Assembly UI should display new transfer object fields in the view model",
                "expected_top": "src/Assembly/TransferObjects/AssemblyTransferObject.cs",
                "generic_files": [
                    "src/Services/ServiceRequestService.cs",
                    "src/appsettings.json",
                ],
            },
        ]
        for scenario in scenarios:
            with self.subTest(jira_key=scenario["jira_key"]):
                result = service.build_targets(
                    workflow_type="implementation_plan",
                    task_text=scenario["task_text"],
                    selected_repos=[{"repo_id": scenario["repo_id"]}],
                    jira_key=scenario["jira_key"],
                    provider_payload_by_repo={
                        scenario["repo_id"]: {
                            "likely_file_details": [
                                {"name": scenario["expected_top"], "confidence": 0.62, "reason": "domain provider file"},
                                *[
                                    {"name": file_path, "confidence": 0.96, "reason": "generic provider file"}
                                    for file_path in scenario["generic_files"]
                                ],
                            ]
                        }
                    },
                )
                candidates = result["candidate_files_by_repo"][scenario["repo_id"]]
                diagnostics = result["candidate_diagnostics_by_repo"][scenario["repo_id"]]
                self.assertEqual(candidates[0]["file"], scenario["expected_top"])
                for generic_file in scenario["generic_files"]:
                    if generic_file in diagnostics["dropped_candidates"]:
                        self.assertIn(
                            diagnostics["dropped_candidates"][generic_file]["reason"],
                            {"generic_exclusion", "admission_gate"},
                        )
                    else:
                        generic_candidate = next(item for item in candidates if item["file"] == generic_file)
                        self.assertGreater(candidates[0]["final_score"], generic_candidate["final_score"])
                self.assertGreater(candidates[0].get("exact_domain_overlap_score", 0.0), 0.0)

    def test_multi_repo_targeting_stays_repo_local_for_service_and_client_repos(self) -> None:
        service = self._service_for_regression_cases()

        result = service.build_targets(
            workflow_type="implementation_plan",
            task_text="Assembly UI should display warehouse quota response from client and service repos",
            selected_repos=[{"repo_id": "commerce_service"}, {"repo_id": "client_portal"}],
            jira_key="TEL-ASSEMBLY",
            provider_payload_by_repo={
                "commerce_service": {
                    "likely_file_details": [
                        {"name": "src/Warehouse/Handlers/GetWarehouseQuotaHandler.cs", "confidence": 0.77, "reason": "warehouse handler"},
                        {"name": "src/Services/OrderService.cs", "confidence": 0.95, "reason": "generic shared service"},
                    ]
                },
                "client_portal": {
                    "likely_file_details": [
                        {"name": "src/Assembly/TransferObjects/AssemblyTransferObject.cs", "confidence": 0.7, "reason": "assembly transfer object"},
                        {"name": "src/Services/ServiceRequestService.cs", "confidence": 0.94, "reason": "generic shared service"},
                    ]
                },
            },
        )

        self.assertEqual(result["candidate_files_by_repo"]["commerce_service"][0]["file"], "src/Warehouse/Handlers/GetWarehouseQuotaHandler.cs")
        self.assertEqual(result["candidate_files_by_repo"]["client_portal"][0]["file"], "src/Assembly/TransferObjects/AssemblyTransferObject.cs")

    def test_client_view_targeting_prefers_task_aligned_viewmodel_over_order_viewmodel_noise(self) -> None:
        service = self._service_for_regression_cases()

        result = service.build_targets(
            workflow_type="implementation_plan",
            task_text="Assembly UI screen should show new assembly view model fields",
            selected_repos=[{"repo_id": "client_portal"}],
            jira_key="TEL-ASSEMBLY",
            provider_payload_by_repo={
                "client_portal": {
                    "likely_file_details": [
                        {"name": "src/Assembly/ViewModels/AssemblyViewModel.cs", "confidence": 0.72, "reason": "assembly view model"},
                        {"name": "src/ViewModels/OrderViewModel.cs", "confidence": 0.95, "reason": "generic order view model"},
                    ]
                }
            },
        )

        candidates = result["candidate_files_by_repo"]["client_portal"]
        self.assertEqual(candidates[0]["file"], "src/Assembly/ViewModels/AssemblyViewModel.cs")
        order_noise = next(item for item in candidates if item["file"] == "src/ViewModels/OrderViewModel.cs")
        self.assertGreater(order_noise["benchmark_confusion_penalty"], 0.0)

    def test_tel_10000_like_case_prefers_assembly_handlers_over_generic_services(self) -> None:
        service = self._service_for_telemart_failures()

        result = service.build_targets(
            workflow_type="implementation_plan",
            task_text="Populates tm_assembly_service.id_product when creating an assembly with folder type tm_order_folder.id_type = 1 Steps to reproduce: Open Telemart.Client, open orders, click approve. Notes: remove tm_assembly_service.id_product fill during create assembly flow.",
            selected_repos=[{"repo_id": "telemart_service_test"}],
            jira_key="TEL-10000",
            provider_payload_by_repo={
                "telemart_service_test": {
                    "likely_file_details": [
                        {"name": "src/Telemart.Service/Services/OrderService.cs", "confidence": 0.98, "reason": "historically popular service"},
                        {"name": "src/Telemart.Service/Telemart.Service.csproj", "confidence": 0.95, "reason": "project file"},
                        {"name": "src/Telemart.Service/Controllers/OrdersController.cs", "confidence": 0.93, "reason": "generic controller"},
                        {"name": "src/Telemart.Service/Application/Notifications/AssemblyServices/CreateAssemblyServicesHandler.cs", "confidence": 0.72, "reason": "assembly notification handler"},
                        {"name": "src/Telemart.Service/Application/Commands/AssemblyServiceCommands/CompleteAssemblyHandler.cs", "confidence": 0.7, "reason": "assembly command handler"},
                    ]
                }
            },
        )

        top_files = [item["file"] for item in result["selected_files_by_repo"]["telemart_service_test"][:5]]
        self.assertIn("src/Telemart.Service/Application/Notifications/AssemblyServices/CreateAssemblyServicesHandler.cs", top_files[:2])
        self.assertIn("src/Telemart.Service/Application/Commands/AssemblyServiceCommands/CompleteAssemblyHandler.cs", top_files[:3])
        self.assertNotEqual(top_files[0], "src/Telemart.Service/Services/OrderService.cs")

    def test_tel_10002_like_case_prefers_order_repository_over_order_service(self) -> None:
        service = self._service_for_telemart_failures()

        result = service.build_targets(
            workflow_type="implementation_plan",
            task_text="The api/v1/maintenance/orders/actions/purchase method returns a quantity of zero for items. Steps to reproduce: call the method. Expected result: rows with zero quantities are absent.",
            selected_repos=[{"repo_id": "telemart_service_test"}],
            jira_key="TEL-10002",
            provider_payload_by_repo={
                "telemart_service_test": {
                    "likely_file_details": [
                        {"name": "src/Telemart.Service/Services/OrderService.cs", "confidence": 0.99, "reason": "generic service"},
                        {"name": "src/Telemart.Service/Repositories/OrderRepository.cs", "confidence": 0.65, "reason": "repository match"},
                    ]
                }
            },
        )

        candidates = result["candidate_files_by_repo"]["telemart_service_test"]
        self.assertEqual(candidates[0]["file"], "src/Telemart.Service/Repositories/OrderRepository.cs")
        self.assertEqual(candidates[0]["inferred_task_family"], "repository_query")

    def test_tel_10005_like_case_prefers_report_handler_over_mapping_profile_and_startup(self) -> None:
        service = self._service_for_telemart_failures()

        result = service.build_targets(
            workflow_type="implementation_plan",
            task_text="The computer name does not appear on the printed form of the assembly letter for orders in which the assembly is in a status other than Completed. Steps to reproduce: open Telemart.Client and print assembly letter.",
            selected_repos=[{"repo_id": "telemart_service_test"}],
            jira_key="TEL-10005",
            provider_payload_by_repo={
                "telemart_service_test": {
                    "likely_file_details": [
                        {"name": "src/Telemart.Service/AutoMappings/Profiles/AutoMappingProfile.cs", "confidence": 0.97, "reason": "mapping profile"},
                        {"name": "src/Telemart.Service/Startup.cs", "confidence": 0.93, "reason": "startup"},
                        {"name": "src/Telemart.Service/Application/Commands/ReportCommands/GetOrderAssemblyReportHandler.cs", "confidence": 0.69, "reason": "report handler"},
                    ]
                }
            },
        )

        candidates = result["candidate_files_by_repo"]["telemart_service_test"]
        top_files = [item["file"] for item in candidates[:5]]
        self.assertIn("src/Telemart.Service/Application/Commands/ReportCommands/GetOrderAssemblyReportHandler.cs", top_files)
        self.assertNotIn("src/Telemart.Service/AutoMappings/Profiles/AutoMappingProfile.cs", top_files)
        self.assertNotIn("src/Telemart.Service/Startup.cs", top_files)
        self.assertEqual(candidates[0]["inferred_task_family"], "command_handler")

    def test_tel_10179_like_case_prefers_product_repository_over_order_and_request_services(self) -> None:
        service = self._service_for_telemart_failures()

        result = service.build_targets(
            workflow_type="implementation_plan",
            task_text="Values for group_name and id_group_feature are not saved in ps_chili_product_new when saving a new nomenclature. Steps to reproduce: open Telemart.Client, add nomenclature, save and reopen.",
            selected_repos=[{"repo_id": "telemart_service_test"}],
            jira_key="TEL-10179",
            provider_payload_by_repo={
                "telemart_service_test": {
                    "likely_file_details": [
                        {"name": "src/Telemart.Service/Services/OrderService.cs", "confidence": 0.99, "reason": "generic service"},
                        {"name": "src/Telemart.Service/Services/ServiceRequestService.cs", "confidence": 0.97, "reason": "generic request service"},
                        {"name": "src/Telemart.Service/Repositories/ProductRepository.cs", "confidence": 0.64, "reason": "product repository"},
                    ]
                }
            },
        )

        candidates = result["candidate_files_by_repo"]["telemart_service_test"]
        self.assertEqual(candidates[0]["file"], "src/Telemart.Service/Repositories/ProductRepository.cs")
        self.assertGreater(candidates[0]["missed_feature_family_boost"], 0.0)

    def test_dto_case_prefers_product_dto_over_generic_service(self) -> None:
        service = self._service_for_telemart_failures()

        result = service.build_targets(
            workflow_type="implementation_plan",
            task_text="API response contract should expose product lookup DTO fields",
            selected_repos=[{"repo_id": "telemart_service_test"}],
            jira_key="TEL-DTO",
            provider_payload_by_repo={
                "telemart_service_test": {
                    "likely_file_details": [
                        {"name": "src/Telemart.Service/Services/OrderService.cs", "confidence": 0.98, "reason": "generic service"},
                        {"name": "src/Telemart.Service/DataTransferObjects/ProductLookupDto.cs", "confidence": 0.66, "reason": "dto match"},
                    ]
                }
            },
        )

        candidates = result["candidate_files_by_repo"]["telemart_service_test"]
        self.assertEqual(candidates[0]["file"], "src/Telemart.Service/DataTransferObjects/ProductLookupDto.cs")
        self.assertEqual(candidates[0]["inferred_task_family"], "dto_contract")


if __name__ == "__main__":
    unittest.main()
