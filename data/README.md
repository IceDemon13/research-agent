# Research Agent

## Українська

### Що це
Research Agent - це репозиторійно-орієнтований агент для аналізу коду, підготовки специфікацій, change set і draft-змін. Він намагається працювати не "взагалі по темі", а по реальному контексту репозиторію: файлах, символах, знайдених фрагментах коду та manifest-даних.

Система особливо корисна для:
- review існуючої реалізації
- підготовки точкових draft-змін для конкретної функції або файлу
- побудови change set перед реалізацією
- побудови repo-aware context перед аналізом
- безпечного fallback для ризикованих rewrite складних функцій

### Що система вміє
- Repository-aware review існуючого коду
- Targeted drafts для конкретних символів і файлів
- Change set generation для нових helper/module змін або ширших змін
- Repo-aware context building на основі manifest, search і symbol targeting
- Safe fallback suggestions, якщо rewrite ризикований або не проходить preservation validation

### Основні режими і команди

#### `/review`
Коли використовувати:
- потрібно подивитися, що код already робить
- потрібно отримати practical review без draft generation
- потрібно проаналізувати конкретну функцію або файл

Що повертає:
- Existing implementation
- Relevant files
- Findings
- Optional suggestions

Приклад:
```text
/review review existing search_in_repo implementation in repo_tools
```

#### `/drafts`
Коли використовувати:
- потрібно підготувати draft для існуючої функції або файлу
- потрібно змінити одну функцію точково
- потрібно додати logging або локальні правки без broad rewrite

Що повертає:
- Spec Result
- Code Plan Result
- Change Set Result
- Draft Set Result

Приклад:
```text
/drafts add more detailed logging to existing search_in_repo in tools/repo_tools.py
```

#### `/changes`
Коли використовувати:
- потрібно підготувати change set для нової helper/module зміни
- потрібен план змін до реалізації без готового draft file
- зміна ширша за один символ

Приклад:
```text
/changes create helper to export repo manifest summary as markdown
```

#### `/spec`
Коли використовувати:
- потрібно коротко й чітко описати задачу перед реалізацією
- потрібно зафіксувати scope, acceptance criteria, risks
- потрібно підготувати task-oriented spec для repo task

Приклад:
```text
/spec підготуй специфікацію для додавання більш детального логування в search_in_repo
```

#### `/pipeline`
Коли використовувати:
- потрібно пройти spec -> code plan pipeline
- потрібно отримати специфікацію і план реалізації без change/draft stage

Приклад:
```text
/pipeline prepare plan for adding repo manifest export to markdown
```

### Приклади використання в терміналі

#### Review існуючої реалізації
```text
/review review existing read_file_range implementation in tools/repo_tools.py
```

#### Draft для одного символу в одному файлі
```text
/drafts add input validation logging to read_file_range in tools/repo_tools.py
```

#### Change set для нового helper/module
```text
/changes create helper to export repo manifest summary as markdown
```

#### Repo-aware специфікація
```text
/spec підготуй специфікацію для покращення логування в select_candidate_files
```

#### Перевірити repo-aware поведінку на існуючій функції
```text
/review inspect existing select_candidate_files scoring logic
```

### Приклади використання в Telegram
- попросити review функції:
  `/review review existing search_in_repo implementation in repo_tools`
- попросити draft для зміни в конкретному файлі:
  `/drafts add more detailed logging to existing search_in_repo in tools/repo_tools.py`
- попросити spec для задачі:
  `/spec підготуй специфікацію для додавання більш детального логування в search_in_repo`
- попросити change set для нового helper:
  `/changes create helper to export repo manifest summary as markdown`
- отримати safe fallback suggestion, якщо rewrite ризикований:
  запитати change/draft для складної production-функції і система може повернути safe fallback suggestion замість unsafe rewrite

### Логування і конфігурація
Основні значення беруться з `config.py` та `.env`.

#### `LOG_LEVEL_CONSOLE`
Дозволені значення:
- `off`
- `user`
- `steps`
- `info`
- `debug`
- `trace`

Що робить:
- керує деталізацією логів у консолі
- у `debug` / `trace` показує більше внутрішньої діагностики

#### `LOG_LEVEL_FILE`
Дозволені значення:
- `off`
- `error`
- `warn`
- `info`
- `debug`
- `trace`

Що робить:
- керує деталізацією логів у файл

#### `LOG_CONSOLE_MAX_CHARS`
Що робить:
- обрізає окремий console log line, якщо потрібно
- `0` означає без обрізання

#### `LOG_FILE_MAX_CHARS`
Що робить:
- обрізає окремий рядок у file log
- `0` означає без обрізання

#### `PIPELINE_CONSOLE_OUTPUT_MODE`
Дозволені значення:
- `full`
- `summary`
- `off`

Що робить:
- керує тим, як результати pipeline показуються в терміналі
- `summary` зазвичай найзручніший для щоденної роботи

#### `PIPELINE_CONSOLE_PREVIEW_CHARS`
Що робить:
- ліміт для summary preview у терміналі
- довгі code/file blocks compact-яться перед truncation, щоб висновки було видно краще

### Safe mode і обмеження
- `review` і `drafts` зазвичай найбезпечніші режими для щоденної роботи
- для складних функцій система може повернути safe fallback suggestion замість ризикованого rewrite
- автоматичний rewrite великих production-функцій може бути обмежений preservation rules
- для locked modify requests система намагається працювати по конкретному symbol/file scope
- якщо контекст недостатній або preservation validation не пройдений, система краще поверне safer result, ніж unsafe rewrite

### Що очікувати від safe fallback
Якщо функція занадто складна для безпечного rewrite, система може повернути:
- file
- symbol
- failure reason
- preservation failure fields
- safe fallback suggestion з точками вставки `log_line(...)`

Це зроблено навмисно, щоб не ламати робочу логіку складних функцій.

### Поточні обмеження
- якість результату сильно залежить від того, наскільки чітко вказано target file або symbol
- для дуже великих або legacy-функцій rewrite може перейти у safe fallback
- не всі agent outputs однаково придатні для "copy-paste без перевірки"
- складні multi-file рефакторинги краще робити через `/changes` або `/pipeline`, а не через один `/drafts`


## RAG

### Мета проекту
Цей репозиторій тепер також включає невеликий локальний проект RAG. Його метою є отримання локальних документів, створення індексу знань з можливістю пошуку, отримання найбільш релевантних фрагментів для запиту та відповіді на запитання, що ґрунтуються на цих отриманих фрагментах, з видимими джерелами.

### Огляд архітектури
Потік RAG навмисно простий:
- `config.py` зберігає локальні налаштування, зручні для `.env`
- `ingest.py` зчитує документи з `./data`, розділяє їх на фрагменти, створює вбудовування та записує артефакти в `./index`
- `retriever.py` завантажує збережений індекс FAISS та сховище фрагментів, виконує гібридний пошук та переранжує результати
- `tools.py` надає `knowledge_search(query)` для зручного для агентів виводу пошуку
- `agent.py` надає невеликий, зручний для демонстрації, локальний агент знань
- `main.py` надає мінімальний CLI для ведення питань

### Підтримувані формати документів
Підтримувані формати введення:
- `.md`
- `.txt`
- `.pdf`

Документи завантажуються рекурсивно з `./data`.

### Як працює завантаження
`ingest.py` виконує конвеєр завантаження:
- сканує `./data` на наявність підтримуваних файлів
- витягує необроблений текст з кожного файлу
- розділяє текст на перекриваючі фрагменти, використовуючи `CHUNK_SIZE` та `CHUNK_OVERLAP`
- створює вбудовування з `text-embedding-3-small` за замовчуванням
- будує векторний індекс FAISS
- зберігає індекс FAISS на диску в `./index/faiss.index`
- зберігає необроблений текст фрагмента та метадані в `./index/chunks.jsonl`
- зберігає невеликий маніфест завантаження в `./index/manifest.json`

Завантаження можна повторно виконати. Якщо вам потрібна чиста перебудова, запустіть її з `--rebuild`.

### Як працює гібридний пошук
`retriever.py` використовує дві стратегії пошуку:
- семантичний пошук:
- вбудовує запит користувача з тією ж моделлю вбудовування та шукає у збереженому індексі FAISS
- пошук BM25:
- оцінює лексичні збіги безпосередньо за збереженими текстами фрагментів

Потім пошуковий інструмент об'єднує обидва набори кандидатів в один гібридний список результатів. Оцінки семантичного та BM25 етапів спочатку нормалізуються, а потім детерміновано об'єднуються.

### Як працює переранжування
Після гібридного об'єднання пошуковий інструмент виконує легкий крок переранжування. Перша версія навмисно проста та надійна:
- починається з об'єднаного гібридного результату
- додає невелике підвищення для прямого перекриття термінів запиту
- додає невелике підвищення для покриття запиту всередині фрагмента
- зберігає оригінальні метадані джерела, приєднані до кожного результату

Це дозволяє уникнути важких залежностей від переранжування, водночас покращуючи остаточне впорядкування.

### Кроки налаштування
1. Створіть та активуйте віртуальне середовище.
2. Встановіть залежності проекту.
3. Переконайтеся, що `.env` містить `OPENAI_API_KEY`.
4. Додайте вихідні документи до `./data`.
5. Запустіть прийом даних для створення локального індексу.

Для потоку RAG основними додатковими пакетами середовища виконання є:
- `openai`
- `numpy`
- `faiss-cpu`
- `pypdf`
- `pydantic-settings`

### Виконання команд
Створення або перебудова локального індексу:

```bash
python ingest.py
python ingest.py --rebuild
```

Виконайте запит з командного рядка:

```bash
python main.py "Що таке repo_context?"
```

Запустіть ретривер безпосередньо:

```bash
python retriever.py "Що таке repo_context?"
```

### Приклади запитів
Приклади питань для локальної бази знань:
- `python main.py "Що таке repo_context?"`
- `python main.py "Як працює гібридний пошук у цьому проекті?"`
- `python main.py "Які файли використовуються для артефактів прийому?"`
- `python main.py "Як переранжування впливає на кінцеві результати пошуку?"`

### Обмеження та майбутні покращення
Поточні обмеження:
- відповіді настільки ж хороші, як і локальні документи в `./data`
- перша версія агента покладається лише на пошук локальних знань
- переранжування є евристичним, а не модельним
- фрагментація - це просте фрагментація на основі символів, а не структурно-залежний розбір
- якість пошуку залежить від встановлення необхідних пакетів та вбудованого індексу в `./index`

Можливі майбутні покращення:
- додати кращі стратегії фрагментації для заголовків, розділів та блоків коду
- додати багатші поля метаданих, такі як номери сторінок або назви розділів
- покращити синтез відповідей, щоб агент підсумовував отримані фрагменти більш природним чином
- додати додаткове переранжування на основі моделі
- додати скрипти оцінки якості пошуку та обґрунтування відповідей

---

## English

### What It Is
Research Agent is a repository-aware assistant for code review, task specs, change sets, and targeted draft generation. It tries to stay grounded in the actual repository context: files, symbols, retrieved code snippets, and manifest data.

It is especially useful for:
- reviewing existing implementation
- preparing targeted drafts for a specific function or file
- generating a change set before implementation
- building repository-aware context before analysis
- returning a safe fallback suggestion when a rewrite would be risky

### Repo Context Pipeline
`repo_context` is the internal repository snapshot passed between pipeline stages and downstream agents. It exists to keep review, spec, change, and draft flows grounded in the same deterministic view of the repo instead of letting each stage guess its own file set.

The structure includes:
- `parsed_query`: normalized task intent, path hints, symbol hints, and keywords
- `resolved_target_files`: the strongest current file targets
- `resolved_symbols`: symbol-to-file resolution results
- `file_selection`: why each surviving file was kept
- `files_used`: the final working file set
- `chunks`: retrieved code snippets with path, reason, and snippet text
- `total_chunks`: the final chunk count
- `debug`: optional internal metadata about applied rules, removed paths, forced paths, and notes

Sanitization exists because raw repo search is noisy. Without filtering, implementation-focused requests can drift into docs, entrypoints, test files, output artifacts, or agent internals and produce weaker specs or reviews. The pipeline therefore removes forbidden or low-value paths first, applies mode-specific overrides next, then runs a consistency finalizer so the parallel structures do not drift apart.

Deterministic rule ordering matters. The shaping pipeline always follows the same sequence:
- normalize the incoming context
- remove forbidden or noise paths for the current request
- apply command-mode overrides such as repo-helper create anchoring or implementation-focused spec shaping
- finalize structural consistency across files, chunks, and file-selection metadata
- finalize debug metadata from the original-vs-final context diff

Mode differences:
- `spec`: implementation-focused, but broad enough to retain nearby repo context for planning
- `changes`: strongest implementation anchoring, especially for create requests that should land in repo tooling
- `drafts`: prefers symbol-only or patch-only fallback behavior for tightly scoped edits
- `review`: the strictest implementation focus, with minimal tolerance for repo drift

Consistency finalization exists because `repo_context` has multiple parallel structures. After shaping, the finalizer dedupes and normalizes paths, drops stale `file_selection` entries, keeps `resolved_target_files` aligned with the surviving context, and recomputes `total_chunks`.

Debug metadata exists so internal shaping decisions remain explainable. When rules remove or force files, the pipeline records which rules fired and which paths were removed or forced without changing the public repo-context contract.

Examples:
- `create helper to export repo manifest summary as markdown`
  The pipeline strips docs and entrypoint drift, then forces repo-domain implementation targets such as `tools/repo_tools.py` or `tools/registry.py`.
- `review existing search_in_repo implementation in repo_tools`
  The pipeline keeps the implementation target, removes `output/*`, `agents/*`, and other noise, and passes a review-focused repo context downstream.
- `add completion log to search_in_repo`
  The pipeline prefers a symbol-only context, removes README drift, and keeps the draft request locked to the resolved implementation file.

### Running Tests
Use the repo-local PowerShell wrappers so the intended test scope is explicit and easy to discover.

Repo-context suite:
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run_repo_context_tests.ps1
```

Full unittest suite:
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run_all_tests.ps1
```

Single targeted unittest:
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run_test.ps1 -TestPath "tests.test_repo_commands.RepoContextShapingTests.test_impl_focused_trace_contains_key_structured_steps"
```

Quality gate:
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\quality_gate.ps1
```

The equivalent raw unittest commands are:

```powershell
.\.venv\Scripts\python.exe -m unittest tests.test_repo_commands
.\.venv\Scripts\python.exe -m unittest
```

If the raw commands fail before Python starts, the local virtualenv launcher is likely broken. In this repository, `.venv\pyvenv.cfg` or `venv\pyvenv.cfg` may point at an unusable Windows Store base interpreter. The scripts above keep the expected workflow explicit even when that environment issue needs to be repaired.

If direct `./scripts/*.ps1` execution is blocked, that is a PowerShell execution-policy issue rather than a repo test failure, which is why the documented commands above use `powershell -ExecutionPolicy Bypass -File ...`.

`quality_gate.ps1` is the recommended pre-commit or pre-push entrypoint. It runs the repo-context suite first, then the full unittest suite, and stops on the first failure.

For the recommended agent-oriented test loop, see `docs/TEST_WORKFLOW.md`.
### CI Coverage
GitHub Actions runs the safe local Python unittest suite on every push and pull request via [`.github/workflows/python-tests.yml`](/c:/работа/my%20repositories/research-agent/.github/workflows/python-tests.yml).

CI runs:

```bash
python -m unittest tests.test_repo_commands
python -m unittest
```

The workflow is intentionally minimal:
- set up Python 3.12
- install `requirements.txt`
- run the repo-context suite
- run the full unittest suite

If future tests require local secrets, live services, or machine-specific setup, they should stay out of this workflow or be split into a separate opt-in job. The current CI job is meant to cover only the safe local suite that should run deterministically in automation.

Recommended recovery:
- recreate `.venv` from a working local Python installation
- verify `.\.venv\Scripts\python.exe --version`
- rerun the commands above

### What The System Can Do
- Repository-aware review of existing code
- Targeted drafts for existing symbols and files
- Change set generation for new helpers/modules or broader changes
- Repo-aware context building using manifest, search, and symbol targeting
- Safe fallback suggestions for risky complex-function rewrites

### Main Modes And Commands

#### `/review`
Use it when:
- you want to understand what the code already does
- you want practical review output without draft generation
- you want to inspect a specific function or file

Returns:
- Existing implementation
- Relevant files
- Findings
- Optional suggestions

Example:
```text
/review review existing search_in_repo implementation in repo_tools
```

#### `/drafts`
Use it when:
- you want a draft for an existing function or file
- you want a precise symbol-level change
- you want logging or other small local edits without a broad rewrite

Example:
```text
/drafts add more detailed logging to existing search_in_repo in tools/repo_tools.py
```

#### `/changes`
Use it when:
- you want a change set for a new helper/module
- you want an implementation change plan without a full draft file
- the task is broader than one symbol

Example:
```text
/changes create helper to export repo manifest summary as markdown
```

#### `/spec`
Use it when:
- you want a concise task specification before implementation
- you want to capture scope, acceptance criteria, and risks
- you want a repo-task-oriented spec

Example:
```text
/spec prepare spec for adding detailed logging to search_in_repo
```

#### `/pipeline`
Use it when:
- you want the spec -> code plan pipeline
- you want specification plus implementation planning without change/draft stages

Example:
```text
/pipeline prepare plan for adding repo manifest export to markdown
```

### Terminal Use Cases

#### Review existing implementation
```text
/review review existing read_file_range implementation in tools/repo_tools.py
```

#### Generate a draft for one symbol in one file
```text
/drafts add input validation logging to read_file_range in tools/repo_tools.py
```

#### Create a helper/module
```text
/changes create helper to export repo manifest summary as markdown
```

#### Inspect repository-aware behavior
```text
/review inspect existing select_candidate_files scoring logic
```

### Telegram Use Cases
- ask for a function review:
  `/review review existing search_in_repo implementation in repo_tools`
- request a draft change in a specific file:
  `/drafts add more detailed logging to existing search_in_repo in tools/repo_tools.py`
- prepare a spec for a task:
  `/spec prepare spec for adding detailed logging to search_in_repo`
- request a change set for a new helper:
  `/changes create helper to export repo manifest summary as markdown`
- get a safe fallback suggestion when rewrite is risky:
  ask for a risky draft/rewrite on a complex production function and the system may return a safe fallback suggestion instead of an unsafe rewrite

### Logging And Configuration
The main settings come from `config.py` and `.env`.

#### `LOG_LEVEL_CONSOLE`
Allowed values:
- `off`
- `user`
- `steps`
- `info`
- `debug`
- `trace`

Controls:
- how much detail is shown in console logs
- `debug` / `trace` expose more internal diagnostics

#### `LOG_LEVEL_FILE`
Allowed values:
- `off`
- `error`
- `warn`
- `info`
- `debug`
- `trace`

Controls:
- how much detail is written to file logs

#### `LOG_CONSOLE_MAX_CHARS`
Controls:
- max length of one console log line
- `0` means unlimited

#### `LOG_FILE_MAX_CHARS`
Controls:
- max length of one file log line
- `0` means unlimited

#### `PIPELINE_CONSOLE_OUTPUT_MODE`
Allowed values:
- `full`
- `summary`
- `off`

Controls:
- how pipeline results are shown in the terminal
- `summary` is usually the most practical default

#### `PIPELINE_CONSOLE_PREVIEW_CHARS`
Controls:
- preview length in terminal summary mode
- long code/file blocks are compacted before truncation so conclusions remain visible

### Safe Mode And Limitations
- `review` and `drafts` are usually the safest modes
- for complex functions the system may return a safe fallback suggestion instead of a risky rewrite
- automatic rewrite of large production functions may be limited by preservation rules
- locked modify requests try to stay within one symbol/file target
- if context is weak or preservation validation fails, the system prefers a safer result over an unsafe rewrite

### What Safe Fallback Means
When a function is too complex for a safe rewrite, the system may return:
- file
- symbol
- failure reason
- preservation failure fields
- a safe fallback suggestion with exact `log_line(...)` insertion guidance

This is intentional: it avoids breaking working logic in complex functions.

### Current Limitations
- result quality depends heavily on how clearly the target file or symbol is specified
- very large or legacy functions may fall back to safe insertion guidance
- not every agent output should be treated as copy-paste-ready without review
- broader multi-file refactors are better handled through `/changes` or `/pipeline` than a single `/drafts` request

---

## RAG 

### Project Goal
This repository now also includes a small local RAG project. Its goal is to ingest local documents, build a searchable knowledge index, retrieve the most relevant chunks for a query, and answer questions grounded in those retrieved chunks with visible sources.

### Architecture Overview
The RAG flow is intentionally simple:
- `config.py` stores local `.env`-friendly settings
- `ingest.py` reads documents from `./data`, chunks them, creates embeddings, and writes artifacts to `./index`
- `retriever.py` loads the stored FAISS index and chunk store, runs hybrid retrieval, and reranks results
- `tools.py` exposes `knowledge_search(query)` for agent-friendly retrieval output
- `agent.py` provides a small demo-friendly local knowledge agent
- `main.py` provides a minimal CLI for asking questions

### Supported Document Formats
Supported input formats:
- `.md`
- `.txt`
- `.pdf`

Documents are loaded recursively from `./data`.

### How Ingestion Works
`ingest.py` performs the ingestion pipeline:
- scans `./data` for supported files
- extracts raw text from each file
- splits text into overlapping chunks using `CHUNK_SIZE` and `CHUNK_OVERLAP`
- creates embeddings with `text-embedding-3-small` by default
- builds a FAISS vector index
- stores the FAISS index on disk in `./index/faiss.index`
- stores raw chunk text and metadata in `./index/chunks.jsonl`
- stores a small ingestion manifest in `./index/manifest.json`

Ingestion is rerunnable. If you want a clean rebuild, run it with `--rebuild`.

### How Hybrid Retrieval Works
`retriever.py` uses two retrieval strategies:
- semantic retrieval:
  embeds the user query with the same embedding model and searches the saved FAISS index
- BM25 retrieval:
  scores lexical matches directly over the stored chunk texts

The retriever then merges both candidate sets into one hybrid result list. Scores from the semantic and BM25 stages are normalized first, then combined deterministically.

### How Reranking Works
After hybrid merge, the retriever performs a lightweight reranking step. The first version is intentionally simple and reliable:
- starts from the merged hybrid score
- adds a small boost for direct query-term overlap
- adds a small boost for query coverage inside the chunk
- keeps the original source metadata attached to every result

This avoids heavy reranker dependencies while still improving the final ordering.

### Setup Steps
1. Create and activate a virtual environment.
2. Install project dependencies.
3. Make sure `.env` contains `OPENAI_API_KEY`.
4. Add source documents into `./data`.
5. Run ingestion to build the local index.

For the RAG flow, the main extra runtime packages are:
- `openai`
- `numpy`
- `faiss-cpu`
- `pypdf`
- `pydantic-settings`

### Run Commands
Build or rebuild the local index:

```bash
python ingest.py
python ingest.py --rebuild
```

Run a query from the command line:

```bash
python main.py "What is repo_context?"
```

Run the retriever directly:

```bash
python retriever.py "What is repo_context?"
```

### Example Queries
Example questions for the local knowledge base:
- `python main.py "What is repo_context?"`
- `python main.py "How does hybrid retrieval work in this project?"`
- `python main.py "What files are used for ingestion artifacts?"`
- `python main.py "How does reranking affect final search results?"`

### Limitations And Future Improvements
Current limitations:
- answers are only as good as the local documents in `./data`
- the first agent version relies only on local knowledge search
- reranking is heuristic rather than model-based
- chunking is simple character-based chunking, not structure-aware parsing
- retrieval quality depends on having the required packages installed and a built index in `./index`

Possible future improvements:
- add better chunking strategies for headings, sections, and code blocks
- add richer metadata fields such as page numbers or section titles
- improve answer synthesis so the agent summarizes retrieved chunks more naturally
- add optional model-based reranking
- add evaluation scripts for retrieval quality and answer grounding

