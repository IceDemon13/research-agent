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
