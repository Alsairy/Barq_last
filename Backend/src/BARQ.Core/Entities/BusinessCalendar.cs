using BARQ.Core.Entities;

namespace BARQ.Core.Entities;

public class BusinessCalendar : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "UTC";
    public TimeSpan WorkDayStart { get; set; } = new TimeSpan(9, 0, 0);
    public TimeSpan WorkDayEnd { get; set; } = new TimeSpan(17, 0, 0);
    public string WorkDays { get; set; } = "1,2,3,4,5"; // Monday-Friday
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    
    public virtual ICollection<SlaPolicy> SlaPolicies { get; set; } = new List<SlaPolicy>();
    public virtual ICollection<BusinessCalendarHoliday> Holidays { get; set; } = new List<BusinessCalendarHoliday>();
}

public class BusinessCalendarHoliday : TenantEntity
{
    public Guid BusinessCalendarId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    
    public virtual BusinessCalendar BusinessCalendar { get; set; } = null!;
}
