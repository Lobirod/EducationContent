namespace Shared.SharedKernel;

public static class EducationErrors
{
    public static Error TitleConflict(string title) =>
        Error.Conflict("lesson.title.conflict", $"Урок с заголовком {title} уже существует");

    public static Error DatabaseError() =>
        Error.Failure("education.database.error", "Ошибка базы данных при работе с сервисом - education");

    public static Error OperationCancelled() =>
        Error.Failure("education.operation.cancelled", "Операция была отменена");

}
