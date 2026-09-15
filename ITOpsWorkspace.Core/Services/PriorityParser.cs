using ITOpsWorkspace.Core.Enums;

namespace ITOpsWorkspace.Core.Services;

public static class PriorityParser
{
    public static ServiceNowPriority Parse(string priorityValue)
    {
        // priorityValue is expected to be the raw ServiceNow priority number as a string ("1", "2", "3", "4")
        return priorityValue switch
        {
            "1" => ServiceNowPriority.Critical,
            "2" => ServiceNowPriority.High,
            "3" => ServiceNowPriority.Moderate,
            "4" => ServiceNowPriority.Low,
            _ => ServiceNowPriority.Unknown
        };
    }
}