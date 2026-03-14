print("TEST START")

result = jira_search_issues("project = TEL order by created desc")

print("RESULT:", result)

print("TEST END")