using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Enums
{
    public enum UserTaskStatus
    {
        Pending = 1,    // Ожидает выполнения
        Processing = 2, // В процессе
        Completed = 3,  // Завершено успешно
        Failed = 4,     // Завершено с ошибкой
        Canceled = 5    // Отменено пользователем
    }
}
