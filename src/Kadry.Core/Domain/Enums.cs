namespace Kadry.Core.Domain;

/// <summary>Статус заявления на отпуск.</summary>
public enum RequestStatus
{
    /// <summary>Новое: зарегистрировано кадровиком.</summary>
    New = 0,

    /// <summary>Согласовано руководителем (остаток проверен).</summary>
    Agreed = 1,

    /// <summary>Утверждено: издан приказ, дни зарезервированы.</summary>
    Approved = 2,

    /// <summary>Отклонено с указанием причины.</summary>
    Refused = 3
}

/// <summary>Роль пользователя приложения.</summary>
public enum UserRole
{
    /// <summary>Кадровик: полный доступ.</summary>
    HrOfficer = 0,

    /// <summary>Руководитель подразделения: заявления и график своего отдела.</summary>
    Manager = 1
}
