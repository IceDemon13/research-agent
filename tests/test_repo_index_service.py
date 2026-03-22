from __future__ import annotations

import shutil
import time
import unittest
import uuid
from pathlib import Path

from services.repo_index_service import RepositoryIndexService
from services.repo_registry import INDEXED_REPO_STATUS, RepositoryRegistryService


class RepositoryIndexServiceTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp_root = (Path("artifacts") / "test-temp").resolve()
        self._temp_root.mkdir(parents=True, exist_ok=True)
        self.workspace_root = (self._temp_root / f"repo-index-{uuid.uuid4().hex}").resolve()
        self.workspace_root.mkdir(parents=True, exist_ok=True)
        self.registry_path = self.workspace_root / "artifacts" / "repos" / "registry.json"
        self.registry_service = RepositoryRegistryService(storage_path=self.registry_path)
        self.index_service = RepositoryIndexService(storage_path=self.registry_path)
        self.repo_root = self.workspace_root / "sample-repo"
        (self.repo_root / "src").mkdir(parents=True)
        (self.repo_root / "tests").mkdir(parents=True)
        (self.repo_root / "docs").mkdir(parents=True)
        (self.repo_root / ".git").mkdir(parents=True)
        (self.repo_root / ".git" / "HEAD").write_text("ref: refs/heads/main\n", encoding="utf-8")
        (self.repo_root / "README.md").write_text("# Sample Repo\n", encoding="utf-8")
        (self.repo_root / "pyproject.toml").write_text("[project]\nname='sample'\n", encoding="utf-8")
        (self.repo_root / "src" / "app.py").write_text("def run() -> str:\n    return 'ok'\n", encoding="utf-8")
        (self.repo_root / "tests" / "test_app.py").write_text("def test_run():\n    assert True\n", encoding="utf-8")
        (self.repo_root / "docs" / "architecture.md").write_text("# Architecture\n", encoding="utf-8")
        self.registry_service.register_repo(
            root_path=str(self.repo_root),
            repo_id="sample",
            display_name="Sample Repo",
        )

    def tearDown(self) -> None:
        shutil.rmtree(self.workspace_root, ignore_errors=True)

    def test_build_repo_index_persists_manifest_and_file_index(self) -> None:
        artifacts = self.index_service.build_repo_index("sample")

        manifest_path = self.index_service.repo_storage_dir("sample") / "repo_manifest.json"
        file_index_path = self.index_service.repo_storage_dir("sample") / "file_index.json"

        self.assertTrue(manifest_path.exists())
        self.assertTrue(file_index_path.exists())
        self.assertEqual(artifacts.manifest.repo_id, "sample")
        self.assertEqual(artifacts.file_index.repo_id, "sample")
        self.assertEqual(artifacts.manifest.file_count, artifacts.file_index.file_count)
        self.assertIn("README.md", artifacts.manifest.main_docs_candidates)
        self.assertIn("pyproject.toml", artifacts.manifest.config_candidates)
        self.assertIn("tests/test_app.py", artifacts.manifest.likely_test_paths)

        app_entry = next(
            entry for entry in artifacts.file_index.files
            if entry.relative_path == "src/app.py"
        )
        self.assertEqual(app_entry.language, "python")
        self.assertGreater(app_entry.file_size, 0)
        self.assertTrue(app_entry.content_hash)
        self.assertTrue(app_entry.last_indexed_at)

        refreshed_repo = self.registry_service.refresh_repo_metadata("sample")
        self.assertIsNotNone(refreshed_repo)
        self.assertEqual(refreshed_repo.status, INDEXED_REPO_STATUS)
        self.assertTrue(refreshed_repo.indexed_at)

    def test_refresh_repo_index_reuses_unchanged_entries_and_updates_changed_files(self) -> None:
        initial = self.index_service.build_repo_index("sample")
        initial_readme = next(
            entry for entry in initial.file_index.files
            if entry.relative_path == "README.md"
        )
        initial_app = next(
            entry for entry in initial.file_index.files
            if entry.relative_path == "src/app.py"
        )

        time.sleep(0.02)
        (self.repo_root / "README.md").write_text("# Sample Repo\n\nUpdated\n", encoding="utf-8")

        refreshed = self.index_service.refresh_repo_index("sample")
        refreshed_readme = next(
            entry for entry in refreshed.file_index.files
            if entry.relative_path == "README.md"
        )
        refreshed_app = next(
            entry for entry in refreshed.file_index.files
            if entry.relative_path == "src/app.py"
        )

        self.assertNotEqual(refreshed_readme.content_hash, initial_readme.content_hash)
        self.assertNotEqual(refreshed_readme.last_indexed_at, initial_readme.last_indexed_at)
        self.assertEqual(refreshed_app.content_hash, initial_app.content_hash)
        self.assertEqual(refreshed_app.last_indexed_at, initial_app.last_indexed_at)

        loaded_manifest = self.index_service.get_repo_manifest("sample")
        loaded_file_index = self.index_service.get_file_index("sample")
        self.assertIsNotNone(loaded_manifest)
        self.assertIsNotNone(loaded_file_index)
        self.assertEqual(loaded_manifest.file_count, refreshed.file_index.file_count)
        self.assertEqual(loaded_file_index.file_count, refreshed.file_index.file_count)

    def test_build_repo_index_generates_profile_symbols_dependencies_and_glossary(self) -> None:
        dotnet_root = self.workspace_root / "dotnet-repo"
        (dotnet_root / "src" / "Catalog.Api" / "Controllers").mkdir(parents=True)
        (dotnet_root / "src" / "Catalog.Application" / "Bonuses").mkdir(parents=True)
        (dotnet_root / "src" / "Catalog.Application" / "Validators").mkdir(parents=True)
        (dotnet_root / "src" / "Catalog.Application" / "Services").mkdir(parents=True)
        (dotnet_root / "src" / "Catalog.Contracts" / "Responses").mkdir(parents=True)
        (dotnet_root / "tests" / "Catalog.Tests").mkdir(parents=True)
        (dotnet_root / ".git").mkdir(parents=True)
        (dotnet_root / ".git" / "HEAD").write_text("ref: refs/heads/main\n", encoding="utf-8")
        (dotnet_root / "Catalog.sln").write_text("Project(\"{GUID}\") = \"Catalog\"", encoding="utf-8")
        (dotnet_root / "src" / "Catalog.Api" / "Catalog.Api.csproj").write_text(
            "<Project Sdk=\"Microsoft.NET.Sdk.Web\">"
            "<ItemGroup>"
            "<ProjectReference Include=\"..\\Catalog.Application\\Catalog.Application.csproj\" />"
            "<PackageReference Include=\"Swashbuckle.AspNetCore\" Version=\"6.0.0\" />"
            "</ItemGroup>"
            "</Project>",
            encoding="utf-8",
        )
        (dotnet_root / "src" / "Catalog.Application" / "Catalog.Application.csproj").write_text(
            "<Project Sdk=\"Microsoft.NET.Sdk\">"
            "<ItemGroup>"
            "<ProjectReference Include=\"..\\Catalog.Contracts\\Catalog.Contracts.csproj\" />"
            "<PackageReference Include=\"MediatR\" Version=\"12.0.0\" />"
            "<PackageReference Include=\"FluentValidation\" Version=\"11.0.0\" />"
            "</ItemGroup>"
            "</Project>",
            encoding="utf-8",
        )
        (dotnet_root / "src" / "Catalog.Contracts" / "Catalog.Contracts.csproj").write_text(
            "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>",
            encoding="utf-8",
        )
        (dotnet_root / "tests" / "Catalog.Tests" / "Catalog.Tests.csproj").write_text(
            "<Project Sdk=\"Microsoft.NET.Sdk\">"
            "<ItemGroup>"
            "<ProjectReference Include=\"..\\..\\src\\Catalog.Api\\Catalog.Api.csproj\" />"
            "<PackageReference Include=\"xunit\" Version=\"2.0.0\" />"
            "</ItemGroup>"
            "</Project>",
            encoding="utf-8",
        )
        (dotnet_root / "src" / "Catalog.Api" / "Program.cs").write_text(
            "var builder = WebApplication.CreateBuilder(args);\n"
            "builder.Services.AddSwaggerGen();\n"
            "var app = builder.Build();\n",
            encoding="utf-8",
        )
        (dotnet_root / "src" / "Catalog.Api" / "appsettings.Development.json").write_text("{}", encoding="utf-8")
        (dotnet_root / "src" / "Catalog.Api" / "Controllers" / "BonusController.cs").write_text(
            "using Catalog.Application.Bonuses;\n"
            "using Catalog.Contracts.Responses;\n"
            "using MediatR;\n"
            "using Microsoft.AspNetCore.Mvc;\n"
            "namespace Catalog.Api.Controllers;\n"
            "[ApiController]\n"
            "[Route(\"api/bonus\")]\n"
            "public class BonusController : ControllerBase {\n"
            "  private readonly IMediator _mediator;\n"
            "  public BonusController(IMediator mediator) { _mediator = mediator; }\n"
            "  [HttpGet(\"info\")]\n"
            "  public ActionResult<BonusInfoResponse> GetInfo() { return new BonusInfoResponse(); }\n"
            "}\n",
            encoding="utf-8",
        )
        (dotnet_root / "src" / "Catalog.Application" / "Bonuses" / "GetBonusInfoQuery.cs").write_text(
            "using Catalog.Contracts.Responses;\n"
            "using MediatR;\n"
            "namespace Catalog.Application.Bonuses;\n"
            "public record GetBonusInfoQuery : IRequest<BonusInfoResponse>;\n",
            encoding="utf-8",
        )
        (dotnet_root / "src" / "Catalog.Application" / "Bonuses" / "GetBonusInfoHandler.cs").write_text(
            "using Catalog.Application.Services;\n"
            "using Catalog.Application.Validators;\n"
            "using Catalog.Contracts.Responses;\n"
            "using MediatR;\n"
            "namespace Catalog.Application.Bonuses;\n"
            "public class GetBonusInfoHandler : IRequestHandler<GetBonusInfoQuery, BonusInfoResponse> {\n"
            "  public GetBonusInfoHandler(IBonusService bonusService, GetBonusInfoQueryValidator validator) {}\n"
            "  public Task<BonusInfoResponse> Handle(GetBonusInfoQuery request, CancellationToken cancellationToken) { return Task.FromResult(new BonusInfoResponse()); }\n"
            "}\n",
            encoding="utf-8",
        )
        (dotnet_root / "src" / "Catalog.Application" / "Services" / "BonusService.cs").write_text(
            "namespace Catalog.Application.Services;\n"
            "public interface IBonusService {}\n"
            "public class BonusService : IBonusService {}\n",
            encoding="utf-8",
        )
        (dotnet_root / "src" / "Catalog.Application" / "Validators" / "GetBonusInfoQueryValidator.cs").write_text(
            "using Catalog.Application.Bonuses;\n"
            "using FluentValidation;\n"
            "namespace Catalog.Application.Validators;\n"
            "public class GetBonusInfoQueryValidator : AbstractValidator<GetBonusInfoQuery> {}\n",
            encoding="utf-8",
        )
        (dotnet_root / "src" / "Catalog.Contracts" / "Responses" / "BonusInfoResponse.cs").write_text(
            "namespace Catalog.Contracts.Responses;\n"
            "public class BonusInfoResponse {\n"
            "  public decimal AmountToExpire { get; set; }\n"
            "}\n",
            encoding="utf-8",
        )
        (dotnet_root / "tests" / "Catalog.Tests" / "BonusControllerTests.cs").write_text(
            "using Xunit;\n"
            "namespace Catalog.Tests;\n"
            "public class BonusControllerTests {\n"
            "  [Fact]\n"
            "  public void GetInfo_returns_ok() {}\n"
            "}\n",
            encoding="utf-8",
        )
        (dotnet_root / ".env").write_text("SECRET=1\n", encoding="utf-8")
        self.registry_service.register_repo(
            root_path=str(dotnet_root),
            repo_id="dotnet",
            display_name="Dotnet Repo",
        )

        artifacts = self.index_service.build_repo_index("dotnet")

        self.assertIsNotNone(artifacts.repo_profile)
        self.assertEqual(artifacts.repo_profile.primary_stack, "dotnet")
        self.assertIn("dotnet", artifacts.repo_profile.detected_stacks)
        self.assertIn("src", artifacts.repo_profile.source_roots)
        self.assertIn("tests", artifacts.repo_profile.test_roots)
        self.assertIn("aspnet-core", artifacts.repo_profile.framework_markers)
        self.assertIn("mediatr", artifacts.repo_profile.framework_markers)
        self.assertIn("fluentvalidation", artifacts.repo_profile.framework_markers)
        self.assertIn("swagger", artifacts.repo_profile.framework_markers)
        self.assertIn("xunit", artifacts.repo_profile.framework_markers)
        self.assertIn("dotnet build", artifacts.repo_profile.build_command_candidates)
        self.assertIn("dotnet test", artifacts.repo_profile.test_command_candidates)
        self.assertIn("src/Catalog.Api/Program.cs", artifacts.repo_profile.program_files)
        self.assertIn("src/Catalog.Api/appsettings.Development.json", artifacts.repo_profile.appsettings_files)
        self.assertIn("tests/Catalog.Tests/Catalog.Tests.csproj", artifacts.repo_profile.test_projects)
        self.assertEqual(artifacts.repo_profile.controller_count, 1)
        self.assertEqual(artifacts.repo_profile.route_count, 1)
        self.assertEqual(artifacts.repo_profile.handler_count, 1)
        self.assertEqual(artifacts.repo_profile.validator_count, 1)

        symbol_names = {item.name for item in artifacts.symbol_index.symbols}
        self.assertIn("BonusController", symbol_names)
        self.assertIn("GetBonusInfoHandler", symbol_names)
        self.assertIn("IBonusService", symbol_names)
        self.assertIn("GetBonusInfoQueryValidator", symbol_names)
        self.assertIn("BonusInfoResponse", symbol_names)
        controller_symbol = next(item for item in artifacts.symbol_index.symbols if item.name == "BonusController")
        self.assertEqual(controller_symbol.kind, "controller")
        self.assertEqual(controller_symbol.namespace, "Catalog.Api.Controllers")
        action_symbol = next(item for item in artifacts.symbol_index.symbols if item.name == "GetInfo")
        self.assertEqual(action_symbol.http_method, "GET")
        self.assertEqual(action_symbol.route, "api/bonus/info")
        self.assertEqual(action_symbol.return_type, "ActionResult<BonusInfoResponse>")
        self.assertIn("controller", artifacts.symbol_index.file_roles["src/Catalog.Api/Controllers/BonusController.cs"])
        self.assertIn("validator", artifacts.symbol_index.file_roles["src/Catalog.Application/Validators/GetBonusInfoQueryValidator.cs"])

        edge_pairs = {(edge.source, edge.target, edge.relation) for edge in artifacts.dependency_map.edges}
        self.assertIn(("tests/Catalog.Tests/BonusControllerTests.cs", "src/Catalog.Api/Controllers/BonusController.cs", "tests"), edge_pairs)
        self.assertIn(("src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs", "GetBonusInfoQuery", "mediatr_request"), edge_pairs)
        self.assertIn(("src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs", "BonusInfoResponse", "mediatr_response"), edge_pairs)
        self.assertIn(("src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs", "src/Catalog.Application/Services/BonusService.cs", "depends_on_service"), edge_pairs)
        self.assertIn(("src/Catalog.Application/Bonuses/GetBonusInfoHandler.cs", "src/Catalog.Application/Validators/GetBonusInfoQueryValidator.cs", "depends_on_validator"), edge_pairs)
        self.assertIn(("src/Catalog.Api/Catalog.Api.csproj", "../Catalog.Application/Catalog.Application.csproj", "project_reference"), edge_pairs)
        self.assertTrue(any(route.get("route") == "api/bonus/info" for route in artifacts.dependency_map.routes))
        route = next(route for route in artifacts.dependency_map.routes if route.get("route") == "api/bonus/info")
        self.assertEqual(route.get("controller"), "BonusController")
        self.assertEqual(route.get("action"), "GetInfo")
        self.assertEqual(route.get("http_method"), "GET")

        glossary_terms = {item.term for item in artifacts.glossary.terms}
        self.assertIn("bonus", glossary_terms)
        self.assertIn("expire", glossary_terms)
        self.assertNotIn("secret", glossary_terms)
        self.assertNotIn(".env", {item.file_path for item in artifacts.symbol_index.symbols})
        self.assertNotIn(".env", {entry.relative_path for entry in artifacts.file_index.files})


if __name__ == "__main__":
    unittest.main()
