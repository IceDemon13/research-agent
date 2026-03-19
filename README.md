# Research Agent

## РЈРєСЂР°С—РЅСЃСЊРєР°

### Р©Рѕ С†Рµ
Research Agent - С†Рµ СЂРµРїРѕР·РёС‚РѕСЂС–Р№РЅРѕ-РѕСЂС–С”РЅС‚РѕРІР°РЅРёР№ Р°РіРµРЅС‚ РґР»СЏ Р°РЅР°Р»С–Р·Сѓ РєРѕРґСѓ, РїС–РґРіРѕС‚РѕРІРєРё СЃРїРµС†РёС„С–РєР°С†С–Р№, change set С– draft-Р·РјС–РЅ. Р’С–РЅ РЅР°РјР°РіР°С”С‚СЊСЃСЏ РїСЂР°С†СЋРІР°С‚Рё РЅРµ "РІР·Р°РіР°Р»С– РїРѕ С‚РµРјС–", Р° РїРѕ СЂРµР°Р»СЊРЅРѕРјСѓ РєРѕРЅС‚РµРєСЃС‚Сѓ СЂРµРїРѕР·РёС‚РѕСЂС–СЋ: С„Р°Р№Р»Р°С…, СЃРёРјРІРѕР»Р°С…, Р·РЅР°Р№РґРµРЅРёС… С„СЂР°РіРјРµРЅС‚Р°С… РєРѕРґСѓ С‚Р° manifest-РґР°РЅРёС….

РЎРёСЃС‚РµРјР° РѕСЃРѕР±Р»РёРІРѕ РєРѕСЂРёСЃРЅР° РґР»СЏ:
- review С–СЃРЅСѓСЋС‡РѕС— СЂРµР°Р»С–Р·Р°С†С–С—
- РїС–РґРіРѕС‚РѕРІРєРё С‚РѕС‡РєРѕРІРёС… draft-Р·РјС–РЅ РґР»СЏ РєРѕРЅРєСЂРµС‚РЅРѕС— С„СѓРЅРєС†С–С— Р°Р±Рѕ С„Р°Р№Р»Сѓ
- РїРѕР±СѓРґРѕРІРё change set РїРµСЂРµРґ СЂРµР°Р»С–Р·Р°С†С–С”СЋ
- РїРѕР±СѓРґРѕРІРё repo-aware context РїРµСЂРµРґ Р°РЅР°Р»С–Р·РѕРј
- Р±РµР·РїРµС‡РЅРѕРіРѕ fallback РґР»СЏ СЂРёР·РёРєРѕРІР°РЅРёС… rewrite СЃРєР»Р°РґРЅРёС… С„СѓРЅРєС†С–Р№

### Р©Рѕ СЃРёСЃС‚РµРјР° РІРјС–С”
- Repository-aware review С–СЃРЅСѓСЋС‡РѕРіРѕ РєРѕРґСѓ
- Targeted drafts РґР»СЏ РєРѕРЅРєСЂРµС‚РЅРёС… СЃРёРјРІРѕР»С–РІ С– С„Р°Р№Р»С–РІ
- Change set generation РґР»СЏ РЅРѕРІРёС… helper/module Р·РјС–РЅ Р°Р±Рѕ С€РёСЂС€РёС… Р·РјС–РЅ
- Repo-aware context building РЅР° РѕСЃРЅРѕРІС– manifest, search С– symbol targeting
- Safe fallback suggestions, СЏРєС‰Рѕ rewrite СЂРёР·РёРєРѕРІР°РЅРёР№ Р°Р±Рѕ РЅРµ РїСЂРѕС…РѕРґРёС‚СЊ preservation validation

### РћСЃРЅРѕРІРЅС– СЂРµР¶РёРјРё С– РєРѕРјР°РЅРґРё

#### `/review`
РљРѕР»Рё РІРёРєРѕСЂРёСЃС‚РѕРІСѓРІР°С‚Рё:
- РїРѕС‚СЂС–Р±РЅРѕ РїРѕРґРёРІРёС‚РёСЃСЏ, С‰Рѕ РєРѕРґ already СЂРѕР±РёС‚СЊ
- РїРѕС‚СЂС–Р±РЅРѕ РѕС‚СЂРёРјР°С‚Рё practical review Р±РµР· draft generation
- РїРѕС‚СЂС–Р±РЅРѕ РїСЂРѕР°РЅР°Р»С–Р·СѓРІР°С‚Рё РєРѕРЅРєСЂРµС‚РЅСѓ С„СѓРЅРєС†С–СЋ Р°Р±Рѕ С„Р°Р№Р»

Р©Рѕ РїРѕРІРµСЂС‚Р°С”:
- Existing implementation
- Relevant files
- Findings
- Optional suggestions

РџСЂРёРєР»Р°Рґ:
```text
/review review existing search_in_repo implementation in repo_tools
```

#### `/drafts`
РљРѕР»Рё РІРёРєРѕСЂРёСЃС‚РѕРІСѓРІР°С‚Рё:
- РїРѕС‚СЂС–Р±РЅРѕ РїС–РґРіРѕС‚СѓРІР°С‚Рё draft РґР»СЏ С–СЃРЅСѓСЋС‡РѕС— С„СѓРЅРєС†С–С— Р°Р±Рѕ С„Р°Р№Р»Сѓ
- РїРѕС‚СЂС–Р±РЅРѕ Р·РјС–РЅРёС‚Рё РѕРґРЅСѓ С„СѓРЅРєС†С–СЋ С‚РѕС‡РєРѕРІРѕ
- РїРѕС‚СЂС–Р±РЅРѕ РґРѕРґР°С‚Рё logging Р°Р±Рѕ Р»РѕРєР°Р»СЊРЅС– РїСЂР°РІРєРё Р±РµР· broad rewrite

Р©Рѕ РїРѕРІРµСЂС‚Р°С”:
- Spec Result
- Code Plan Result
- Change Set Result
- Draft Set Result

РџСЂРёРєР»Р°Рґ:
```text
/drafts add more detailed logging to existing search_in_repo in tools/repo_tools.py
```

#### `/changes`
РљРѕР»Рё РІРёРєРѕСЂРёСЃС‚РѕРІСѓРІР°С‚Рё:
- РїРѕС‚СЂС–Р±РЅРѕ РїС–РґРіРѕС‚СѓРІР°С‚Рё change set РґР»СЏ РЅРѕРІРѕС— helper/module Р·РјС–РЅРё
- РїРѕС‚СЂС–Р±РµРЅ РїР»Р°РЅ Р·РјС–РЅ РґРѕ СЂРµР°Р»С–Р·Р°С†С–С— Р±РµР· РіРѕС‚РѕРІРѕРіРѕ draft file
- Р·РјС–РЅР° С€РёСЂС€Р° Р·Р° РѕРґРёРЅ СЃРёРјРІРѕР»

РџСЂРёРєР»Р°Рґ:
```text
/changes create helper to export repo manifest summary as markdown
```

#### `/spec`
РљРѕР»Рё РІРёРєРѕСЂРёСЃС‚РѕРІСѓРІР°С‚Рё:
- РїРѕС‚СЂС–Р±РЅРѕ РєРѕСЂРѕС‚РєРѕ Р№ С‡С–С‚РєРѕ РѕРїРёСЃР°С‚Рё Р·Р°РґР°С‡Сѓ РїРµСЂРµРґ СЂРµР°Р»С–Р·Р°С†С–С”СЋ
- РїРѕС‚СЂС–Р±РЅРѕ Р·Р°С„С–РєСЃСѓРІР°С‚Рё scope, acceptance criteria, risks
- РїРѕС‚СЂС–Р±РЅРѕ РїС–РґРіРѕС‚СѓРІР°С‚Рё task-oriented spec РґР»СЏ repo task

РџСЂРёРєР»Р°Рґ:
```text
/spec РїС–РґРіРѕС‚СѓР№ СЃРїРµС†РёС„С–РєР°С†С–СЋ РґР»СЏ РґРѕРґР°РІР°РЅРЅСЏ Р±С–Р»СЊС€ РґРµС‚Р°Р»СЊРЅРѕРіРѕ Р»РѕРіСѓРІР°РЅРЅСЏ РІ search_in_repo
```

#### `/pipeline`
РљРѕР»Рё РІРёРєРѕСЂРёСЃС‚РѕРІСѓРІР°С‚Рё:
- РїРѕС‚СЂС–Р±РЅРѕ РїСЂРѕР№С‚Рё spec -> code plan pipeline
- РїРѕС‚СЂС–Р±РЅРѕ РѕС‚СЂРёРјР°С‚Рё СЃРїРµС†РёС„С–РєР°С†С–СЋ С– РїР»Р°РЅ СЂРµР°Р»С–Р·Р°С†С–С— Р±РµР· change/draft stage

РџСЂРёРєР»Р°Рґ:
```text
/pipeline prepare plan for adding repo manifest export to markdown
```

### РџСЂРёРєР»Р°РґРё РІРёРєРѕСЂРёСЃС‚Р°РЅРЅСЏ РІ С‚РµСЂРјС–РЅР°Р»С–

#### Review С–СЃРЅСѓСЋС‡РѕС— СЂРµР°Р»С–Р·Р°С†С–С—
```text
/review review existing read_file_range implementation in tools/repo_tools.py
```

#### Draft РґР»СЏ РѕРґРЅРѕРіРѕ СЃРёРјРІРѕР»Сѓ РІ РѕРґРЅРѕРјСѓ С„Р°Р№Р»С–
```text
/drafts add input validation logging to read_file_range in tools/repo_tools.py
```

#### Change set РґР»СЏ РЅРѕРІРѕРіРѕ helper/module
```text
/changes create helper to export repo manifest summary as markdown
```

#### Repo-aware СЃРїРµС†РёС„С–РєР°С†С–СЏ
```text
/spec РїС–РґРіРѕС‚СѓР№ СЃРїРµС†РёС„С–РєР°С†С–СЋ РґР»СЏ РїРѕРєСЂР°С‰РµРЅРЅСЏ Р»РѕРіСѓРІР°РЅРЅСЏ РІ select_candidate_files
```

#### РџРµСЂРµРІС–СЂРёС‚Рё repo-aware РїРѕРІРµРґС–РЅРєСѓ РЅР° С–СЃРЅСѓСЋС‡С–Р№ С„СѓРЅРєС†С–С—
```text
/review inspect existing select_candidate_files scoring logic
```

### РџСЂРёРєР»Р°РґРё РІРёРєРѕСЂРёСЃС‚Р°РЅРЅСЏ РІ Telegram
- РїРѕРїСЂРѕСЃРёС‚Рё review С„СѓРЅРєС†С–С—:
  `/review review existing search_in_repo implementation in repo_tools`
- РїРѕРїСЂРѕСЃРёС‚Рё draft РґР»СЏ Р·РјС–РЅРё РІ РєРѕРЅРєСЂРµС‚РЅРѕРјСѓ С„Р°Р№Р»С–:
  `/drafts add more detailed logging to existing search_in_repo in tools/repo_tools.py`
- РїРѕРїСЂРѕСЃРёС‚Рё spec РґР»СЏ Р·Р°РґР°С‡С–:
  `/spec РїС–РґРіРѕС‚СѓР№ СЃРїРµС†РёС„С–РєР°С†С–СЋ РґР»СЏ РґРѕРґР°РІР°РЅРЅСЏ Р±С–Р»СЊС€ РґРµС‚Р°Р»СЊРЅРѕРіРѕ Р»РѕРіСѓРІР°РЅРЅСЏ РІ search_in_repo`
- РїРѕРїСЂРѕСЃРёС‚Рё change set РґР»СЏ РЅРѕРІРѕРіРѕ helper:
  `/changes create helper to export repo manifest summary as markdown`
- РѕС‚СЂРёРјР°С‚Рё safe fallback suggestion, СЏРєС‰Рѕ rewrite СЂРёР·РёРєРѕРІР°РЅРёР№:
  Р·Р°РїРёС‚Р°С‚Рё change/draft РґР»СЏ СЃРєР»Р°РґРЅРѕС— production-С„СѓРЅРєС†С–С— С– СЃРёСЃС‚РµРјР° РјРѕР¶Рµ РїРѕРІРµСЂРЅСѓС‚Рё safe fallback suggestion Р·Р°РјС–СЃС‚СЊ unsafe rewrite

### Р›РѕРіСѓРІР°РЅРЅСЏ С– РєРѕРЅС„С–РіСѓСЂР°С†С–СЏ
РћСЃРЅРѕРІРЅС– Р·РЅР°С‡РµРЅРЅСЏ Р±РµСЂСѓС‚СЊСЃСЏ Р· `config.py` С‚Р° `.env`.

#### `LOG_LEVEL_CONSOLE`
Р”РѕР·РІРѕР»РµРЅС– Р·РЅР°С‡РµРЅРЅСЏ:
- `off`
- `user`
- `steps`
- `info`
- `debug`
- `trace`

Р©Рѕ СЂРѕР±РёС‚СЊ:
- РєРµСЂСѓС” РґРµС‚Р°Р»С–Р·Р°С†С–С”СЋ Р»РѕРіС–РІ Сѓ РєРѕРЅСЃРѕР»С–
- Сѓ `debug` / `trace` РїРѕРєР°Р·СѓС” Р±С–Р»СЊС€Рµ РІРЅСѓС‚СЂС–С€РЅСЊРѕС— РґС–Р°РіРЅРѕСЃС‚РёРєРё

#### `LOG_LEVEL_FILE`
Р”РѕР·РІРѕР»РµРЅС– Р·РЅР°С‡РµРЅРЅСЏ:
- `off`
- `error`
- `warn`
- `info`
- `debug`
- `trace`

Р©Рѕ СЂРѕР±РёС‚СЊ:
- РєРµСЂСѓС” РґРµС‚Р°Р»С–Р·Р°С†С–С”СЋ Р»РѕРіС–РІ Сѓ С„Р°Р№Р»

#### `LOG_CONSOLE_MAX_CHARS`
Р©Рѕ СЂРѕР±РёС‚СЊ:
- РѕР±СЂС–Р·Р°С” РѕРєСЂРµРјРёР№ console log line, СЏРєС‰Рѕ РїРѕС‚СЂС–Р±РЅРѕ
- `0` РѕР·РЅР°С‡Р°С” Р±РµР· РѕР±СЂС–Р·Р°РЅРЅСЏ

#### `LOG_FILE_MAX_CHARS`
Р©Рѕ СЂРѕР±РёС‚СЊ:
- РѕР±СЂС–Р·Р°С” РѕРєСЂРµРјРёР№ СЂСЏРґРѕРє Сѓ file log
- `0` РѕР·РЅР°С‡Р°С” Р±РµР· РѕР±СЂС–Р·Р°РЅРЅСЏ

#### `PIPELINE_CONSOLE_OUTPUT_MODE`
Р”РѕР·РІРѕР»РµРЅС– Р·РЅР°С‡РµРЅРЅСЏ:
- `full`
- `summary`
- `off`

Р©Рѕ СЂРѕР±РёС‚СЊ:
- РєРµСЂСѓС” С‚РёРј, СЏРє СЂРµР·СѓР»СЊС‚Р°С‚Рё pipeline РїРѕРєР°Р·СѓСЋС‚СЊСЃСЏ РІ С‚РµСЂРјС–РЅР°Р»С–
- `summary` Р·Р°Р·РІРёС‡Р°Р№ РЅР°Р№Р·СЂСѓС‡РЅС–С€РёР№ РґР»СЏ С‰РѕРґРµРЅРЅРѕС— СЂРѕР±РѕС‚Рё

#### `PIPELINE_CONSOLE_PREVIEW_CHARS`
Р©Рѕ СЂРѕР±РёС‚СЊ:
- Р»С–РјС–С‚ РґР»СЏ summary preview Сѓ С‚РµСЂРјС–РЅР°Р»С–
- РґРѕРІРіС– code/file blocks compact-СЏС‚СЊСЃСЏ РїРµСЂРµРґ truncation, С‰РѕР± РІРёСЃРЅРѕРІРєРё Р±СѓР»Рѕ РІРёРґРЅРѕ РєСЂР°С‰Рµ

### Safe mode С– РѕР±РјРµР¶РµРЅРЅСЏ
- `review` С– `drafts` Р·Р°Р·РІРёС‡Р°Р№ РЅР°Р№Р±РµР·РїРµС‡РЅС–С€С– СЂРµР¶РёРјРё РґР»СЏ С‰РѕРґРµРЅРЅРѕС— СЂРѕР±РѕС‚Рё
- РґР»СЏ СЃРєР»Р°РґРЅРёС… С„СѓРЅРєС†С–Р№ СЃРёСЃС‚РµРјР° РјРѕР¶Рµ РїРѕРІРµСЂРЅСѓС‚Рё safe fallback suggestion Р·Р°РјС–СЃС‚СЊ СЂРёР·РёРєРѕРІР°РЅРѕРіРѕ rewrite
- Р°РІС‚РѕРјР°С‚РёС‡РЅРёР№ rewrite РІРµР»РёРєРёС… production-С„СѓРЅРєС†С–Р№ РјРѕР¶Рµ Р±СѓС‚Рё РѕР±РјРµР¶РµРЅРёР№ preservation rules
- РґР»СЏ locked modify requests СЃРёСЃС‚РµРјР° РЅР°РјР°РіР°С”С‚СЊСЃСЏ РїСЂР°С†СЋРІР°С‚Рё РїРѕ РєРѕРЅРєСЂРµС‚РЅРѕРјСѓ symbol/file scope
- СЏРєС‰Рѕ РєРѕРЅС‚РµРєСЃС‚ РЅРµРґРѕСЃС‚Р°С‚РЅС–Р№ Р°Р±Рѕ preservation validation РЅРµ РїСЂРѕР№РґРµРЅРёР№, СЃРёСЃС‚РµРјР° РєСЂР°С‰Рµ РїРѕРІРµСЂРЅРµ safer result, РЅС–Р¶ unsafe rewrite

### Р©Рѕ РѕС‡С–РєСѓРІР°С‚Рё РІС–Рґ safe fallback
РЇРєС‰Рѕ С„СѓРЅРєС†С–СЏ Р·Р°РЅР°РґС‚Рѕ СЃРєР»Р°РґРЅР° РґР»СЏ Р±РµР·РїРµС‡РЅРѕРіРѕ rewrite, СЃРёСЃС‚РµРјР° РјРѕР¶Рµ РїРѕРІРµСЂРЅСѓС‚Рё:
- file
- symbol
- failure reason
- preservation failure fields
- safe fallback suggestion Р· С‚РѕС‡РєР°РјРё РІСЃС‚Р°РІРєРё `log_line(...)`

Р¦Рµ Р·СЂРѕР±Р»РµРЅРѕ РЅР°РІРјРёСЃРЅРѕ, С‰РѕР± РЅРµ Р»Р°РјР°С‚Рё СЂРѕР±РѕС‡Сѓ Р»РѕРіС–РєСѓ СЃРєР»Р°РґРЅРёС… С„СѓРЅРєС†С–Р№.

### РџРѕС‚РѕС‡РЅС– РѕР±РјРµР¶РµРЅРЅСЏ
- СЏРєС–СЃС‚СЊ СЂРµР·СѓР»СЊС‚Р°С‚Сѓ СЃРёР»СЊРЅРѕ Р·Р°Р»РµР¶РёС‚СЊ РІС–Рґ С‚РѕРіРѕ, РЅР°СЃРєС–Р»СЊРєРё С‡С–С‚РєРѕ РІРєР°Р·Р°РЅРѕ target file Р°Р±Рѕ symbol
- РґР»СЏ РґСѓР¶Рµ РІРµР»РёРєРёС… Р°Р±Рѕ legacy-С„СѓРЅРєС†С–Р№ rewrite РјРѕР¶Рµ РїРµСЂРµР№С‚Рё Сѓ safe fallback
- РЅРµ РІСЃС– agent outputs РѕРґРЅР°РєРѕРІРѕ РїСЂРёРґР°С‚РЅС– РґР»СЏ "copy-paste Р±РµР· РїРµСЂРµРІС–СЂРєРё"
- СЃРєР»Р°РґРЅС– multi-file СЂРµС„Р°РєС‚РѕСЂРёРЅРіРё РєСЂР°С‰Рµ СЂРѕР±РёС‚Рё С‡РµСЂРµР· `/changes` Р°Р±Рѕ `/pipeline`, Р° РЅРµ С‡РµСЂРµР· РѕРґРёРЅ `/drafts`


## RAG

### РњРµС‚Р° РїСЂРѕРµРєС‚Сѓ
Р¦РµР№ СЂРµРїРѕР·РёС‚РѕСЂС–Р№ С‚РµРїРµСЂ С‚Р°РєРѕР¶ РІРєР»СЋС‡Р°С” РЅРµРІРµР»РёРєРёР№ Р»РѕРєР°Р»СЊРЅРёР№ РїСЂРѕРµРєС‚ RAG. Р™РѕРіРѕ РјРµС‚РѕСЋ С” РѕС‚СЂРёРјР°РЅРЅСЏ Р»РѕРєР°Р»СЊРЅРёС… РґРѕРєСѓРјРµРЅС‚С–РІ, СЃС‚РІРѕСЂРµРЅРЅСЏ С–РЅРґРµРєСЃСѓ Р·РЅР°РЅСЊ Р· РјРѕР¶Р»РёРІС–СЃС‚СЋ РїРѕС€СѓРєСѓ, РѕС‚СЂРёРјР°РЅРЅСЏ РЅР°Р№Р±С–Р»СЊС€ СЂРµР»РµРІР°РЅС‚РЅРёС… С„СЂР°РіРјРµРЅС‚С–РІ РґР»СЏ Р·Р°РїРёС‚Сѓ С‚Р° РІС–РґРїРѕРІС–РґС– РЅР° Р·Р°РїРёС‚Р°РЅРЅСЏ, С‰Рѕ Т‘СЂСѓРЅС‚СѓСЋС‚СЊСЃСЏ РЅР° С†РёС… РѕС‚СЂРёРјР°РЅРёС… С„СЂР°РіРјРµРЅС‚Р°С…, Р· РІРёРґРёРјРёРјРё РґР¶РµСЂРµР»Р°РјРё.

### РћРіР»СЏРґ Р°СЂС…С–С‚РµРєС‚СѓСЂРё
РџРѕС‚С–Рє RAG РЅР°РІРјРёСЃРЅРѕ РїСЂРѕСЃС‚РёР№:
- `config.py` Р·Р±РµСЂС–РіР°С” Р»РѕРєР°Р»СЊРЅС– РЅР°Р»Р°С€С‚СѓРІР°РЅРЅСЏ, Р·СЂСѓС‡РЅС– РґР»СЏ `.env`
- `ingest.py` Р·С‡РёС‚СѓС” РґРѕРєСѓРјРµРЅС‚Рё Р· `./data`, СЂРѕР·РґС–Р»СЏС” С—С… РЅР° С„СЂР°РіРјРµРЅС‚Рё, СЃС‚РІРѕСЂСЋС” РІР±СѓРґРѕРІСѓРІР°РЅРЅСЏ С‚Р° Р·Р°РїРёСЃСѓС” Р°СЂС‚РµС„Р°РєС‚Рё РІ `./index`
- `retriever.py` Р·Р°РІР°РЅС‚Р°Р¶СѓС” Р·Р±РµСЂРµР¶РµРЅРёР№ С–РЅРґРµРєСЃ FAISS С‚Р° СЃС…РѕРІРёС‰Рµ С„СЂР°РіРјРµРЅС‚С–РІ, РІРёРєРѕРЅСѓС” РіС–Р±СЂРёРґРЅРёР№ РїРѕС€СѓРє С‚Р° РїРµСЂРµСЂР°РЅР¶СѓС” СЂРµР·СѓР»СЊС‚Р°С‚Рё
- `tools.py` РЅР°РґР°С” `knowledge_search(query)` РґР»СЏ Р·СЂСѓС‡РЅРѕРіРѕ РґР»СЏ Р°РіРµРЅС‚С–РІ РІРёРІРѕРґСѓ РїРѕС€СѓРєСѓ
- `agent.py` РЅР°РґР°С” РЅРµРІРµР»РёРєРёР№, Р·СЂСѓС‡РЅРёР№ РґР»СЏ РґРµРјРѕРЅСЃС‚СЂР°С†С–С—, Р»РѕРєР°Р»СЊРЅРёР№ Р°РіРµРЅС‚ Р·РЅР°РЅСЊ
- `main.py` РЅР°РґР°С” РјС–РЅС–РјР°Р»СЊРЅРёР№ CLI РґР»СЏ РІРµРґРµРЅРЅСЏ РїРёС‚Р°РЅСЊ

### РџС–РґС‚СЂРёРјСѓРІР°РЅС– С„РѕСЂРјР°С‚Рё РґРѕРєСѓРјРµРЅС‚С–РІ
РџС–РґС‚СЂРёРјСѓРІР°РЅС– С„РѕСЂРјР°С‚Рё РІРІРµРґРµРЅРЅСЏ:
- `.md`
- `.txt`
- `.pdf`

Р”РѕРєСѓРјРµРЅС‚Рё Р·Р°РІР°РЅС‚Р°Р¶СѓСЋС‚СЊСЃСЏ СЂРµРєСѓСЂСЃРёРІРЅРѕ Р· `./data`.

### РЇРє РїСЂР°С†СЋС” Р·Р°РІР°РЅС‚Р°Р¶РµРЅРЅСЏ
`ingest.py` РІРёРєРѕРЅСѓС” РєРѕРЅРІРµС”СЂ Р·Р°РІР°РЅС‚Р°Р¶РµРЅРЅСЏ:
- СЃРєР°РЅСѓС” `./data` РЅР° РЅР°СЏРІРЅС–СЃС‚СЊ РїС–РґС‚СЂРёРјСѓРІР°РЅРёС… С„Р°Р№Р»С–РІ
- РІРёС‚СЏРіСѓС” РЅРµРѕР±СЂРѕР±Р»РµРЅРёР№ С‚РµРєСЃС‚ Р· РєРѕР¶РЅРѕРіРѕ С„Р°Р№Р»Сѓ
- СЂРѕР·РґС–Р»СЏС” С‚РµРєСЃС‚ РЅР° РїРµСЂРµРєСЂРёРІР°СЋС‡С– С„СЂР°РіРјРµРЅС‚Рё, РІРёРєРѕСЂРёСЃС‚РѕРІСѓСЋС‡Рё `CHUNK_SIZE` С‚Р° `CHUNK_OVERLAP`
- СЃС‚РІРѕСЂСЋС” РІР±СѓРґРѕРІСѓРІР°РЅРЅСЏ Р· `text-embedding-3-small` Р·Р° Р·Р°РјРѕРІС‡СѓРІР°РЅРЅСЏРј
- Р±СѓРґСѓС” РІРµРєС‚РѕСЂРЅРёР№ С–РЅРґРµРєСЃ FAISS
- Р·Р±РµСЂС–РіР°С” С–РЅРґРµРєСЃ FAISS РЅР° РґРёСЃРєСѓ РІ `./index/faiss.index`
- Р·Р±РµСЂС–РіР°С” РЅРµРѕР±СЂРѕР±Р»РµРЅРёР№ С‚РµРєСЃС‚ С„СЂР°РіРјРµРЅС‚Р° С‚Р° РјРµС‚Р°РґР°РЅС– РІ `./index/chunks.jsonl`
- Р·Р±РµСЂС–РіР°С” РЅРµРІРµР»РёРєРёР№ РјР°РЅС–С„РµСЃС‚ Р·Р°РІР°РЅС‚Р°Р¶РµРЅРЅСЏ РІ `./index/manifest.json`

Р—Р°РІР°РЅС‚Р°Р¶РµРЅРЅСЏ РјРѕР¶РЅР° РїРѕРІС‚РѕСЂРЅРѕ РІРёРєРѕРЅР°С‚Рё. РЇРєС‰Рѕ РІР°Рј РїРѕС‚СЂС–Р±РЅР° С‡РёСЃС‚Р° РїРµСЂРµР±СѓРґРѕРІР°, Р·Р°РїСѓСЃС‚С–С‚СЊ С—С— Р· `--rebuild`.

### РЇРє РїСЂР°С†СЋС” РіС–Р±СЂРёРґРЅРёР№ РїРѕС€СѓРє
`retriever.py` РІРёРєРѕСЂРёСЃС‚РѕРІСѓС” РґРІС– СЃС‚СЂР°С‚РµРіС–С— РїРѕС€СѓРєСѓ:
- СЃРµРјР°РЅС‚РёС‡РЅРёР№ РїРѕС€СѓРє:
- РІР±СѓРґРѕРІСѓС” Р·Р°РїРёС‚ РєРѕСЂРёСЃС‚СѓРІР°С‡Р° Р· С‚С–С”СЋ Р¶ РјРѕРґРµР»Р»СЋ РІР±СѓРґРѕРІСѓРІР°РЅРЅСЏ С‚Р° С€СѓРєР°С” Сѓ Р·Р±РµСЂРµР¶РµРЅРѕРјСѓ С–РЅРґРµРєСЃС– FAISS
- РїРѕС€СѓРє BM25:
- РѕС†С–РЅСЋС” Р»РµРєСЃРёС‡РЅС– Р·Р±С–РіРё Р±РµР·РїРѕСЃРµСЂРµРґРЅСЊРѕ Р·Р° Р·Р±РµСЂРµР¶РµРЅРёРјРё С‚РµРєСЃС‚Р°РјРё С„СЂР°РіРјРµРЅС‚С–РІ

РџРѕС‚С–Рј РїРѕС€СѓРєРѕРІРёР№ С–РЅСЃС‚СЂСѓРјРµРЅС‚ РѕР±'С”РґРЅСѓС” РѕР±РёРґРІР° РЅР°Р±РѕСЂРё РєР°РЅРґРёРґР°С‚С–РІ РІ РѕРґРёРЅ РіС–Р±СЂРёРґРЅРёР№ СЃРїРёСЃРѕРє СЂРµР·СѓР»СЊС‚Р°С‚С–РІ. РћС†С–РЅРєРё СЃРµРјР°РЅС‚РёС‡РЅРѕРіРѕ С‚Р° BM25 РµС‚Р°РїС–РІ СЃРїРѕС‡Р°С‚РєСѓ РЅРѕСЂРјР°Р»С–Р·СѓСЋС‚СЊСЃСЏ, Р° РїРѕС‚С–Рј РґРµС‚РµСЂРјС–РЅРѕРІР°РЅРѕ РѕР±'С”РґРЅСѓСЋС‚СЊСЃСЏ.

### РЇРє РїСЂР°С†СЋС” РїРµСЂРµСЂР°РЅР¶СѓРІР°РЅРЅСЏ
РџС–СЃР»СЏ РіС–Р±СЂРёРґРЅРѕРіРѕ РѕР±'С”РґРЅР°РЅРЅСЏ РїРѕС€СѓРєРѕРІРёР№ С–РЅСЃС‚СЂСѓРјРµРЅС‚ РІРёРєРѕРЅСѓС” Р»РµРіРєРёР№ РєСЂРѕРє РїРµСЂРµСЂР°РЅР¶СѓРІР°РЅРЅСЏ. РџРµСЂС€Р° РІРµСЂСЃС–СЏ РЅР°РІРјРёСЃРЅРѕ РїСЂРѕСЃС‚Р° С‚Р° РЅР°РґС–Р№РЅР°:
- РїРѕС‡РёРЅР°С”С‚СЊСЃСЏ Р· РѕР±'С”РґРЅР°РЅРѕРіРѕ РіС–Р±СЂРёРґРЅРѕРіРѕ СЂРµР·СѓР»СЊС‚Р°С‚Сѓ
- РґРѕРґР°С” РЅРµРІРµР»РёРєРµ РїС–РґРІРёС‰РµРЅРЅСЏ РґР»СЏ РїСЂСЏРјРѕРіРѕ РїРµСЂРµРєСЂРёС‚С‚СЏ С‚РµСЂРјС–РЅС–РІ Р·Р°РїРёС‚Сѓ
- РґРѕРґР°С” РЅРµРІРµР»РёРєРµ РїС–РґРІРёС‰РµРЅРЅСЏ РґР»СЏ РїРѕРєСЂРёС‚С‚СЏ Р·Р°РїРёС‚Сѓ РІСЃРµСЂРµРґРёРЅС– С„СЂР°РіРјРµРЅС‚Р°
- Р·Р±РµСЂС–РіР°С” РѕСЂРёРіС–РЅР°Р»СЊРЅС– РјРµС‚Р°РґР°РЅС– РґР¶РµСЂРµР»Р°, РїСЂРёС”РґРЅР°РЅС– РґРѕ РєРѕР¶РЅРѕРіРѕ СЂРµР·СѓР»СЊС‚Р°С‚Сѓ

Р¦Рµ РґРѕР·РІРѕР»СЏС” СѓРЅРёРєРЅСѓС‚Рё РІР°Р¶РєРёС… Р·Р°Р»РµР¶РЅРѕСЃС‚РµР№ РІС–Рґ РїРµСЂРµСЂР°РЅР¶СѓРІР°РЅРЅСЏ, РІРѕРґРЅРѕС‡Р°СЃ РїРѕРєСЂР°С‰СѓСЋС‡Рё РѕСЃС‚Р°С‚РѕС‡РЅРµ РІРїРѕСЂСЏРґРєСѓРІР°РЅРЅСЏ.

### РљСЂРѕРєРё РЅР°Р»Р°С€С‚СѓРІР°РЅРЅСЏ
1. РЎС‚РІРѕСЂС–С‚СЊ С‚Р° Р°РєС‚РёРІСѓР№С‚Рµ РІС–СЂС‚СѓР°Р»СЊРЅРµ СЃРµСЂРµРґРѕРІРёС‰Рµ.
2. Р’СЃС‚Р°РЅРѕРІС–С‚СЊ Р·Р°Р»РµР¶РЅРѕСЃС‚С– РїСЂРѕРµРєС‚Сѓ.
3. РџРµСЂРµРєРѕРЅР°Р№С‚РµСЃСЏ, С‰Рѕ `.env` РјС–СЃС‚РёС‚СЊ `OPENAI_API_KEY`.
4. Р”РѕРґР°Р№С‚Рµ РІРёС…С–РґРЅС– РґРѕРєСѓРјРµРЅС‚Рё РґРѕ `./data`.
5. Р—Р°РїСѓСЃС‚С–С‚СЊ РїСЂРёР№РѕРј РґР°РЅРёС… РґР»СЏ СЃС‚РІРѕСЂРµРЅРЅСЏ Р»РѕРєР°Р»СЊРЅРѕРіРѕ С–РЅРґРµРєСЃСѓ.

Р”Р»СЏ РїРѕС‚РѕРєСѓ RAG РѕСЃРЅРѕРІРЅРёРјРё РґРѕРґР°С‚РєРѕРІРёРјРё РїР°РєРµС‚Р°РјРё СЃРµСЂРµРґРѕРІРёС‰Р° РІРёРєРѕРЅР°РЅРЅСЏ С”:
- `openai`
- `numpy`
- `faiss-cpu`
- `pypdf`
- `pydantic-settings`

### Р’РёРєРѕРЅР°РЅРЅСЏ РєРѕРјР°РЅРґ
РЎС‚РІРѕСЂРµРЅРЅСЏ Р°Р±Рѕ РїРµСЂРµР±СѓРґРѕРІР° Р»РѕРєР°Р»СЊРЅРѕРіРѕ С–РЅРґРµРєСЃСѓ:

```bash
python ingest.py
python ingest.py --rebuild
```

Р’РёРєРѕРЅР°Р№С‚Рµ Р·Р°РїРёС‚ Р· РєРѕРјР°РЅРґРЅРѕРіРѕ СЂСЏРґРєР°:

```bash
python main.py "Р©Рѕ С‚Р°РєРµ repo_context?"
```

Р—Р°РїСѓСЃС‚С–С‚СЊ СЂРµС‚СЂРёРІРµСЂ Р±РµР·РїРѕСЃРµСЂРµРґРЅСЊРѕ:

```bash
python retriever.py "Р©Рѕ С‚Р°РєРµ repo_context?"
```

### РџСЂРёРєР»Р°РґРё Р·Р°РїРёС‚С–РІ
РџСЂРёРєР»Р°РґРё РїРёС‚Р°РЅСЊ РґР»СЏ Р»РѕРєР°Р»СЊРЅРѕС— Р±Р°Р·Рё Р·РЅР°РЅСЊ:
- `python main.py "Р©Рѕ С‚Р°РєРµ repo_context?"`
- `python main.py "РЇРє РїСЂР°С†СЋС” РіС–Р±СЂРёРґРЅРёР№ РїРѕС€СѓРє Сѓ С†СЊРѕРјСѓ РїСЂРѕРµРєС‚С–?"`
- `python main.py "РЇРєС– С„Р°Р№Р»Рё РІРёРєРѕСЂРёСЃС‚РѕРІСѓСЋС‚СЊСЃСЏ РґР»СЏ Р°СЂС‚РµС„Р°РєС‚С–РІ РїСЂРёР№РѕРјСѓ?"`
- `python main.py "РЇРє РїРµСЂРµСЂР°РЅР¶СѓРІР°РЅРЅСЏ РІРїР»РёРІР°С” РЅР° РєС–РЅС†РµРІС– СЂРµР·СѓР»СЊС‚Р°С‚Рё РїРѕС€СѓРєСѓ?"`

### РћР±РјРµР¶РµРЅРЅСЏ С‚Р° РјР°Р№Р±СѓС‚РЅС– РїРѕРєСЂР°С‰РµРЅРЅСЏ
РџРѕС‚РѕС‡РЅС– РѕР±РјРµР¶РµРЅРЅСЏ:
- РІС–РґРїРѕРІС–РґС– РЅР°СЃС‚С–Р»СЊРєРё Р¶ С…РѕСЂРѕС€С–, СЏРє С– Р»РѕРєР°Р»СЊРЅС– РґРѕРєСѓРјРµРЅС‚Рё РІ `./data`
- РїРµСЂС€Р° РІРµСЂСЃС–СЏ Р°РіРµРЅС‚Р° РїРѕРєР»Р°РґР°С”С‚СЊСЃСЏ Р»РёС€Рµ РЅР° РїРѕС€СѓРє Р»РѕРєР°Р»СЊРЅРёС… Р·РЅР°РЅСЊ
- РїРµСЂРµСЂР°РЅР¶СѓРІР°РЅРЅСЏ С” РµРІСЂРёСЃС‚РёС‡РЅРёРј, Р° РЅРµ РјРѕРґРµР»СЊРЅРёРј
- С„СЂР°РіРјРµРЅС‚Р°С†С–СЏ - С†Рµ РїСЂРѕСЃС‚Рµ С„СЂР°РіРјРµРЅС‚Р°С†С–СЏ РЅР° РѕСЃРЅРѕРІС– СЃРёРјРІРѕР»С–РІ, Р° РЅРµ СЃС‚СЂСѓРєС‚СѓСЂРЅРѕ-Р·Р°Р»РµР¶РЅРёР№ СЂРѕР·Р±С–СЂ
- СЏРєС–СЃС‚СЊ РїРѕС€СѓРєСѓ Р·Р°Р»РµР¶РёС‚СЊ РІС–Рґ РІСЃС‚Р°РЅРѕРІР»РµРЅРЅСЏ РЅРµРѕР±С…С–РґРЅРёС… РїР°РєРµС‚С–РІ С‚Р° РІР±СѓРґРѕРІР°РЅРѕРіРѕ С–РЅРґРµРєСЃСѓ РІ `./index`

РњРѕР¶Р»РёРІС– РјР°Р№Р±СѓС‚РЅС– РїРѕРєСЂР°С‰РµРЅРЅСЏ:
- РґРѕРґР°С‚Рё РєСЂР°С‰С– СЃС‚СЂР°С‚РµРіС–С— С„СЂР°РіРјРµРЅС‚Р°С†С–С— РґР»СЏ Р·Р°РіРѕР»РѕРІРєС–РІ, СЂРѕР·РґС–Р»С–РІ С‚Р° Р±Р»РѕРєС–РІ РєРѕРґСѓ
- РґРѕРґР°С‚Рё Р±Р°РіР°С‚С€С– РїРѕР»СЏ РјРµС‚Р°РґР°РЅРёС…, С‚Р°РєС– СЏРє РЅРѕРјРµСЂРё СЃС‚РѕСЂС–РЅРѕРє Р°Р±Рѕ РЅР°Р·РІРё СЂРѕР·РґС–Р»С–РІ
- РїРѕРєСЂР°С‰РёС‚Рё СЃРёРЅС‚РµР· РІС–РґРїРѕРІС–РґРµР№, С‰РѕР± Р°РіРµРЅС‚ РїС–РґСЃСѓРјРѕРІСѓРІР°РІ РѕС‚СЂРёРјР°РЅС– С„СЂР°РіРјРµРЅС‚Рё Р±С–Р»СЊС€ РїСЂРёСЂРѕРґРЅРёРј С‡РёРЅРѕРј
- РґРѕРґР°С‚Рё РґРѕРґР°С‚РєРѕРІРµ РїРµСЂРµСЂР°РЅР¶СѓРІР°РЅРЅСЏ РЅР° РѕСЃРЅРѕРІС– РјРѕРґРµР»С–
- РґРѕРґР°С‚Рё СЃРєСЂРёРїС‚Рё РѕС†С–РЅРєРё СЏРєРѕСЃС‚С– РїРѕС€СѓРєСѓ С‚Р° РѕР±Т‘СЂСѓРЅС‚СѓРІР°РЅРЅСЏ РІС–РґРїРѕРІС–РґРµР№

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
GitHub Actions runs the safe local Python unittest suite on every push and pull request via [`.github/workflows/python-tests.yml`](/c:/СЂР°Р±РѕС‚Р°/my%20repositories/research-agent/.github/workflows/python-tests.yml).

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
This homework MVP ingests local project documents, builds a persistent searchable index, retrieves relevant chunks with a simple hybrid retriever, and answers questions in a grounded, demo-friendly format with visible sources.

### Supported Document Formats
- `.md`
- `.txt`
- `.pdf`

Documents are loaded recursively from `./data`.

### Ingestion Pipeline
`ingest.py` performs the ingestion pipeline:
- scans `./data` for supported files
- extracts raw text from each file
- splits text into overlapping chunks using `CHUNK_SIZE` and `CHUNK_OVERLAP`
- assigns simple language metadata per chunk (`uk`, `en`, or `mixed`)
- creates embeddings with `text-embedding-3-small`
- builds a FAISS vector index
- stores the FAISS index on disk in `./index/faiss.index`
- stores raw chunk text and metadata in `./index/chunks.jsonl`
- stores a small ingestion manifest in `./index/manifest.json`

Ingestion is rerunnable. If you want a clean rebuild, run it with `--rebuild`.

### Local Index Persistence
The local knowledge index is stored in `./index`:
- `faiss.index` for vector search
- `chunks.jsonl` for raw chunk text and metadata
- `manifest.json` for a small ingestion summary

### Semantic Retrieval
`retriever.py` embeds the query with the same embedding model and searches the stored FAISS index for semantically similar chunks.

### BM25 Retrieval
The retriever also runs lexical search over the stored chunk texts with `rank-bm25`. This helps when the query depends on exact words, commands, or file names.

### Hybrid Ranking
Semantic and BM25 candidate sets are merged into one deterministic result list. The implementation normalizes both score types, adds a small language-preference bonus, and keeps source metadata attached to every candidate.

### Lightweight Reranking
After merge, results are reranked with a simple heuristic that prefers:
- stronger combined retrieval score
- higher keyword overlap with the query
- stable deterministic ordering for demo use

This keeps the reranking easy to explain and avoids heavy extra dependencies.

### Answer Layer
`agent.py` adds a lightweight answer/orchestration layer on top of retrieval. It currently recognizes at least:
- command questions
- repository capability / "what can you do" questions
- `what is ...` questions
- workflow / test questions
- generic fallback questions

Current answer behavior:
- broad capability questions aggregate multiple retrieved chunks into a short structured overview
- SDD command questions return `/spec`, `/changes`, and `/drafts` with short explanations and examples
- Telegram command questions are treated as Telegram usage of the same command set when local Telegram examples are present
- answers follow the detected query language when possible
- low-confidence retrieval uses an explicit safe fallback instead of guessing

### Local Tool And Partial Web Fallback
The primary homework path is local knowledge retrieval:
- `knowledge_search(query)` uses the stored chunks and FAISS index
- the agent prefers local knowledge for project-specific questions

Partial web fallback also exists in the current implementation:
- `web_search(query)` uses `ddgs`
- `read_url(url)` uses `trafilatura`
- the agent may try web search for clearly external questions or weak local matches

This web path is best-effort only and depends on optional packages, network availability, and usable search results. It should be treated as partial support rather than the main homework path.

### Setup
1. Create and activate a virtual environment.
2. Install project dependencies.
3. Make sure `.env` contains `OPENAI_API_KEY`.
4. Add source documents into `./data`.
5. Run ingestion to build the local index.

### CLI Commands
Build or rebuild the local index:

```bash
python ingest.py
python ingest.py --rebuild
```

Run a query from the command line:

```bash
python main.py "What is repo_context?"
```

Run with retrieval debug output:

```bash
python main.py "What is repo_context?" --debug
```

Run the retriever directly:

```bash
python retriever.py "What is repo_context?"
```

### Example Queries
- `python main.py "What is repo_context?"`
- `python main.py "How does hybrid retrieval work in this project?"`
- `python main.py "What tests should be run after repo-context changes?"`
- `python main.py "what commands do you have for sdd?"`
- `python main.py "які в тебе є команди і що вони роблять для sdd?"`
- `python main.py "що ти можеш робити з репозиторієм?"`
- `python main.py "What is retrieval augmented generation?"`

### Current Limitations
- answers are only as good as the local documents in `./data`
- chunking is simple and not structure-aware
- reranking is heuristic rather than model-based
- semantic retrieval at query time depends on an available embeddings API call; BM25 still provides a lexical fallback when semantic embedding fails
- web fallback is partial and depends on optional web-search dependencies and network availability
- low-confidence handling is intentionally conservative and may decline borderline questions
- retrieval quality depends on having the required packages installed and a built index in `./index`
- command and capability answer formatting is lightweight and heuristic rather than model-based

### Future Improvements
- add more focused source documents for broad capability overviews and Telegram usage examples
- improve structure-aware chunking for headings, sections, and code blocks
- add richer metadata fields such as section titles or page numbers
- replace heuristic reranking with an optional model-based reranker
- add evaluation scripts for retrieval quality, grounding, and answer formatting

### Repository Registry
- Registered repositories are stored in `artifacts/repos/registry.json`
- The new registry foundation supports explicit `repo_id`, `root_path`, `display_name`, `default_branch`, `indexed_at`, and `status`
- Current single-repo flows can map cleanly to a default `self` repo entry through the repository registry service

Example usage:

```python
from services.repo_registry import ensure_default_repo, register_repo

ensure_default_repo(root_path=".", display_name="Research Agent")
repo = register_repo(
    root_path="C:/work/another-repo",
    display_name="Another Repo",
)
print(repo.repo_id)
```

### Persistent Repo Indexing
- Persistent repo artifacts live under `artifacts/repos/<repo_id>/`
- `repo_manifest.json` stores a typed repo summary plus a file list compatible with the current repo-aware flow
- `file_index.json` stores per-file metadata such as `relative_path`, `language`, `file_size`, `content_hash`, and `last_indexed_at`
- For the current self-repo flow, indexing also syncs `output/repo_manifest.json` so the existing review/draft pipeline keeps working

Repo-aware CLI example:

```bash
python main.py "review existing run implementation in src/app.py" --repo-id another-repo
```

### Safe Apply Boundary
- `services/apply_service.py` is the canonical future source-file apply boundary.
- It accepts typed `ApplyInput` operations and defaults to `dry_run=True`.
- Real writes require both `dry_run=False` and an explicit `allow_real_writes=True` override when calling the service directly.
- Current pipeline stages still remain read-only; the apply service is not wired into `agents/root_agent.py` yet.

### Validation Boundary
- `services/validation_service.py` is the canonical validation command runner.
- It resolves `repo_id` to the registered repo root before executing validation steps.
- Commands can come from config through `validation_build_command`, `validation_lint_command`, and `validation_test_command`.
- Without explicit commands, it uses simple fallback detection for Python and `package.json` repos, and bounds stdout/stderr in structured `ValidationResult` output.
- By default it continues through all steps after a failure; callers can opt into stop-on-failure behavior.

### Temp Workspace Validation
- Implementation mode now validates a candidate post-change workspace before any real apply to the original repo.
- The candidate workspace is created by `services/temp_workspace_service.py` as an isolated temporary repo copy with its own local registry file.
- Candidate changes are still applied through `services/apply_service.py`; only the repo registry target changes.
- The original repo remains untouched until implementation mode explicitly requests real apply and candidate validation succeeds.
- Real apply and publication are also gated by config permissions:
  - `allow_real_apply`
  - `allow_pr_creation`
  - `allow_review_creation`
- All three are deny-by-default and must be explicitly enabled for internal implementation flows.

### SCM Boundary
- `services/scm_service.py` is the canonical local git service.
- It supports git repo detection, branch naming/creation, checkout, remote lookup, status, diff, add, commit, and push.
- Git operations stay isolated in the SCM layer; HTTP PR creation is handled separately in the Bitbucket service.

### Bitbucket PR Integration
- Bitbucket PR creation lives in `services/bitbucket_service.py`.
- Implementation mode can optionally create a PR only after validated real apply succeeds.
- Required config:
  - `bitbucket_api_base_url`
  - `bitbucket_username` and `bitbucket_app_password`, or `bitbucket_api_token`
- Example CLI flow:

```bash
python main.py "/implement update src/app.py" --repo-id sample --implementation --apply --create-pr
```

- PR title format: `AI: {goal}`
- PR description includes:
  - artifact summary
  - changed files
  - validation status
  - short diff summary

### Crucible Review Integration
- Crucible review creation lives in `services/crucible_service.py`.
- Implementation mode can optionally create a Crucible review after validated real apply and SCM publication succeed.
- Required config:
  - `crucible_base_url`
  - `crucible_project_key`
  - `crucible_username` and `crucible_password`, or `crucible_api_token`
  - `crucible_default_reviewers`
- Review title format: `AI Review: {goal}`
- Review description includes:
  - change summary
  - validation result
  - Bitbucket PR link if available
  - changed files

### Run Tracking
- Structured implementation-mode run tracking lives in `services/run_service.py`.
- `agents/root_agent.py` records step-level status for:
  - draft
  - validation
  - apply
  - commit/push
  - pull request
  - review
- Use `--run-log` to persist the run record as JSON under `artifacts/runs/`.
