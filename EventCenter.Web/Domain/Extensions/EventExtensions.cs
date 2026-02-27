using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Domain.Enums;
using EventCenter.Web.Infrastructure.Helpers;

namespace EventCenter.Web.Domain.Extensions;

public static class EventExtensions
{
    public static EventState GetCurrentState(this Event evt)
    {
        if (!evt.IsPublished)
            return EventState.NotPublished;

        var nowUtc = DateTime.UtcNow;

        if (evt.EndDateUtc < nowUtc)
            return EventState.Finished;

        var deadlineCet = TimeZoneHelper.ConvertUtcToCet(
            DateTime.SpecifyKind(evt.RegistrationDeadlineUtc, DateTimeKind.Utc));
        var deadlineEndOfDayUtc = TimeZoneHelper.GetEndOfDayCetAsUtc(deadlineCet);

        if (nowUtc > deadlineEndOfDayUtc)
            return EventState.DeadlineReached;

        return EventState.Public;
    }

    public static int GetCurrentRegistrationCount(this Event evt)
    {
        return evt.Registrations?.Count(r => !r.IsCancelled) ?? 0;
    }
}
