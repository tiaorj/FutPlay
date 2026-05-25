namespace FutPlay.Settings
{
    public class EmailOptions
    {
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; } = 587;

        public bool EnableSsl { get; set; } = true;

        public string UserName { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FromEmail { get; set; } = string.Empty;

        public string FromName { get; set; } = "Meu Pitaco FC";

        public bool EstaConfigurado =>
            !string.IsNullOrWhiteSpace(Host) &&
            !string.IsNullOrWhiteSpace(FromEmail);
    }
}
