namespace MovieAPI.Constants;

public static class CacheKeys
{
    public static string MovieById(int id) => $"movie_{id}";
    public static string MoviesByPage(int page, int pageSize) => $"movies_page_{page}_size_{pageSize}";
    public static string MoviesByAuditorium(int auditoriumId) => $"by_auditorium_{auditoriumId}";
}