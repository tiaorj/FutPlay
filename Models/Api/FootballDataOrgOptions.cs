namespace FutPlay.Models.Api
{
    public class FootballDataOrgOptions
    {
        public string BaseUrl { get; set; } = "https://api.football-data.org/v4";

        public string ApiKey { get; set; } = string.Empty;
    }
}
