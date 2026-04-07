using System;

namespace Telemart.Client.Helpers
{
    public static class OrderCommentHelper
    {
        public static string GetJoinedComment(string customerComment, string employeeComment, string systemComment)
        {
            string twoNewLines = $"{Environment.NewLine}{Environment.NewLine}";

            if (customerComment is null && employeeComment is null && systemComment is null)
            {
                return null;
            }

            employeeComment = string.IsNullOrWhiteSpace(employeeComment) ? null : $"Сотрудник: {employeeComment}";
            customerComment = string.IsNullOrWhiteSpace(customerComment) ? null : $"Клиент: {customerComment}";
            systemComment = string.IsNullOrWhiteSpace(systemComment) ? null : $"Система: {systemComment}";

            string[] array = new[] { employeeComment, customerComment, systemComment };

            string comment = string.Join(twoNewLines, array).Trim();

            comment = comment.Replace($"{twoNewLines}{twoNewLines}", twoNewLines);

            return comment;
        }
    }
}