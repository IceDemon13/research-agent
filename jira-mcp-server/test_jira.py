from jira_client import search_issues

result = search_issues("project = TEL order by created desc", 3)

print("TOTAL:", result.get("total"))

for issue in result.get("issues", []):
    print(issue.get("key"), "-", issue.get("fields", {}).get("summary"))