namespace Telemart.Client.Helpers
{
    public static class RobotHelper
    {
        public static string GetFullScript(string script, string parameters)
        {
            return $"{parameters}\n{script}";
        }
    }
}
