namespace Domain.Enums
{
    public enum TaskType
    {
        Comment    = 1,
        Like       = 2,
        // 3 (Dislike) и 4 (Report) работали только для otvet.mail.ru и удалены.
        AutoReply  = 5,
        Monitoring = 6,
    }
}
