namespace Domain.Enums;

public enum TaskAccountStatus
{
    InQueue = 1,    // В очереди
    Active = 2,     // Активно
    InWork = 3,     // В работе
    Completed = 4,  // Выполнено
    Error = 5       // Ошибка
}
