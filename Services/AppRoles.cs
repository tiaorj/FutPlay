namespace FutPlay.Services
{
    public static class AppRoles
    {
        public const string Administrador = "Administrador";
        public const string Participante = "Participante";
        public const string AdministradorOuParticipante = Administrador + "," + Participante;
    }
}
