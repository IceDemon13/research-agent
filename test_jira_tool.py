from mcp_jira_tools import jira_search_issues

print("TEST START")

result = jira_search_issues.invoke({
    "jql": "project = TEL order by created desc"
})

print("RESULT:")
print(result)

print("TEST END")