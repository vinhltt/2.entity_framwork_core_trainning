namespace LessonDemo06.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public virtual UserProfile Profile { get; set; }
    }
} 