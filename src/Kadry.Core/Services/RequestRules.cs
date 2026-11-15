using Kadry.Core.Domain;

namespace Kadry.Core.Services;

/// <summary>Бизнес-правила отпусков: дни периода, статусы, остатки, пересечения.</summary>
public static class RequestRules
{
    /// <summary>Число календарных дней периода включительно.</summary>
    public static int DaysCount(DateTime start, DateTime end)
    {
        var s = start.Date;
        var e = end.Date;
        if (e < s)
            throw new ArgumentException("Дата окончания не может быть раньше даты начала.");
        return (e - s).Days + 1;
    }

    /// <summary>Допустим ли переход статуса по матрице.</summary>
    public static bool CanTransition(RequestStatus from, RequestStatus to) => from switch
    {
        RequestStatus.New => to is RequestStatus.Agreed or RequestStatus.Refused,
        RequestStatus.Agreed => to is RequestStatus.Approved or RequestStatus.Refused,
        _ => false
    };

    /// <summary>Проверяет переход статуса, иначе бросает исключение с понятным текстом.</summary>
    public static void EnsureCanTransition(RequestStatus from, RequestStatus to)
    {
        if (!CanTransition(from, to))
            throw new InvalidOperationException(
                $"Переход статуса «{NameOf(from)}» → «{NameOf(to)}» недопустим.");
    }

    /// <summary>Остаток дней по виду отпуска: лимит минус дни записей за год.</summary>
    public static int RemainingDays(int yearLimitDays, IEnumerable<VacationRecord> records, int year)
    {
        if (yearLimitDays <= 0)
            return 0;

        int used = records
            .Where(r => r.StartDate.Year == year)
            .Sum(r => r.Days);

        return Math.Max(0, yearLimitDays - used);
    }

    /// <summary>Пересекается ли период с одним из перечисленных.</summary>
    public static bool OverlapsAny(IEnumerable<(DateTime Start, DateTime End)> periods,
                                   DateTime start, DateTime end)
    {
        var s = start.Date;
        var e = end.Date;
        return periods.Any(p => s <= p.End.Date && e >= p.Start.Date);
    }

    /// <summary>Название статуса для интерфейса.</summary>
    public static string NameOf(RequestStatus status) => status switch
    {
        RequestStatus.New => "Новое",
        RequestStatus.Agreed => "Согласовано",
        RequestStatus.Approved => "Утверждено",
        RequestStatus.Refused => "Отклонено",
        _ => "Неизвестно"
    };
}
