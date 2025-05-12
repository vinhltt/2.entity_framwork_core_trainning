namespace LessonDemo06.Models
{
    public class UserProfile
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
        public virtual User User { get; set; }
    }
} 