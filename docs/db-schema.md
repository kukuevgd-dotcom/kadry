# Схема базы данных

SQLite, создаётся EF Core (Code First). Файл базы: `%LOCALAPPDATA%\Kadry\kadry.db`.

## Сущности и связи

- `Departments 1—M Employees`
- `Positions 1—M Employees`
- `Employees 1—M VacationRequests`
- `VacationTypes 1—M VacationRequests`
- `VacationRequests 1—1 VacationRecords` (утверждённое заявление порождает приказ)
- `VacationRecords` ссылается на `Employees` и `VacationTypes`
- `Users M—1 Employees` (руководитель привязан к сотруднику; у кадровика ссылки нет)

## DDL (эталон, с ним сверяется код)

```sql
CREATE TABLE Departments (
    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE
);

CREATE TABLE Positions (
    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE
);

CREATE TABLE VacationTypes (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Name          TEXT NOT NULL,
    YearLimitDays INTEGER NOT NULL DEFAULT 0,  -- 0 = без лимита
    IsUnpaid      INTEGER NOT NULL DEFAULT 0   -- 1 = без сохранения зарплаты
);

CREATE TABLE Employees (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName        TEXT NOT NULL,
    PersonnelNumber TEXT NOT NULL UNIQUE,
    DepartmentId    INTEGER NOT NULL REFERENCES Departments(Id),
    PositionId      INTEGER NOT NULL REFERENCES Positions(Id),
    HiredAt         TEXT NOT NULL,             -- ISO 8601: ГГГГ-ММ-ДД
    FiredAt         TEXT NULL                  -- NULL = работает
);
CREATE INDEX IX_Employees_DepartmentId ON Employees(DepartmentId);

CREATE TABLE VacationRequests (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    Number         INTEGER NOT NULL UNIQUE,
    EmployeeId     INTEGER NOT NULL REFERENCES Employees(Id),
    VacationTypeId INTEGER NOT NULL REFERENCES VacationTypes(Id),
    StartDate      TEXT NOT NULL,
    EndDate        TEXT NOT NULL,
    Status         INTEGER NOT NULL DEFAULT 0, -- 0 новое, 1 согласовано, 2 утверждено, 3 отклонено
    CreatedAt      TEXT NOT NULL,
    RefuseReason   TEXT NULL
);
CREATE INDEX IX_VacationRequests_Status ON VacationRequests(Status);
CREATE INDEX IX_VacationRequests_EmployeeId ON VacationRequests(EmployeeId);

CREATE TABLE VacationRecords (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    RequestId      INTEGER NOT NULL UNIQUE REFERENCES VacationRequests(Id),
    EmployeeId     INTEGER NOT NULL REFERENCES Employees(Id),
    VacationTypeId INTEGER NOT NULL REFERENCES VacationTypes(Id),
    StartDate      TEXT NOT NULL,
    EndDate        TEXT NOT NULL,
    Days           INTEGER NOT NULL
);
CREATE INDEX IX_VacationRecords_EmployeeId ON VacationRecords(EmployeeId);

CREATE TABLE Users (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Login        TEXT NOT NULL UNIQUE,
    PasswordHash BLOB NOT NULL,
    PasswordSalt BLOB NOT NULL,
    Role         INTEGER NOT NULL DEFAULT 0,   -- 0 кадровик, 1 руководитель
    EmployeeId   INTEGER NULL REFERENCES Employees(Id)
);

-- начальные данные
INSERT INTO Departments (Name) VALUES
    ('Администрация'), ('Бухгалтерия'), ('Отдел разработки'), ('Отдел продаж');
INSERT INTO Positions (Name) VALUES
    ('Директор'), ('Бухгалтер'), ('Инженер-программист'), ('Менеджер по продажам'), ('Кадровик');
INSERT INTO VacationTypes (Name, YearLimitDays, IsUnpaid) VALUES
    ('Основной ежегодный оплачиваемый', 28, 0),
    ('Дополнительный', 7, 0),
    ('Без сохранения заработной платы', 0, 1),
    ('Учебный', 0, 0);
```

## Решения по схеме

- Даты хранятся строками ISO 8601 (`YYYY-MM-DD`).
- Остаток отпускных дней **не хранится**: вычисляется как «лимит вида минус сумма дней утверждённых записей за рабочий год». Иначе остаток рассинхронизируется при исправлении приказа.
- Все связи — с запретом удаления: справочники и сотрудников, на которых ссылаются заявки, удалять нельзя (проверка на уровне сервисов и внешних ключей).
- `PRAGMA foreign_keys = ON`; режим WAL включается при открытии соединения.