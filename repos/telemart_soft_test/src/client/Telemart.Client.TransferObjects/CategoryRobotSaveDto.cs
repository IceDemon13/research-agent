using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CategoryRobotSaveDto
    {
        public CategoryRobotSaveDto(int id, string robotScript, string robotScriptParameters)
        {
            Id = id;
            RobotScript = robotScript;
            RobotScriptParameters = robotScriptParameters;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("robot_script")]
        public string RobotScript { get; set; }

        [JsonProperty("robot_script_parameters")]
        public string RobotScriptParameters { get; set; }
    }
}