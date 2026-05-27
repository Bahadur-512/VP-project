using System.Text.Json;

namespace CivicPulse.Web.Services;

public class LocalizationService
{
    private Dictionary<string, Dictionary<string, string>> _translations = new();

    private static readonly Dictionary<string, string> _englishFallback = new()
    {
        ["app.name"] = "Civic Pulse",
        ["app.tagline"] = "Your Voice. Your City.",
        ["nav.login"] = "Log In",
        ["nav.register"] = "Register",
        ["nav.dashboard"] = "Dashboard",
        ["nav.submit"] = "Submit Complaint",
        ["nav.my.complaints"] = "My Complaints",
        ["nav.notifications"] = "Notifications",
        ["nav.profile"] = "My Profile",
        ["nav.logout"] = "Logout",
        ["nav.admin.dashboard"] = "Admin Dashboard",
        ["nav.manage.complaints"] = "Manage Complaints",
        ["nav.sla"] = "SLA Tracking",
        ["nav.users"] = "User Management",
        ["nav.categories"] = "Categories",
        ["nav.audit"] = "Audit Log",
        ["welcome.back"] = "Welcome back",
        ["stats.total"] = "Total Complaints",
        ["stats.pending"] = "Pending",
        ["stats.inprogress"] = "In Progress",
        ["stats.resolved"] = "Resolved",
        ["stats.closed"] = "Closed",
        ["stats.rejected"] = "Rejected",
        ["title.my.complaints"] = "My Recent Complaints",
        ["title.notifications"] = "Recent Notifications",
        ["title.quick.actions"] = "Quick Actions",
        ["complaint.number"] = "Complaint #",
        ["complaint.title"] = "Title",
        ["complaint.category"] = "Category",
        ["complaint.status"] = "Status",
        ["complaint.priority"] = "Priority",
        ["complaint.sla"] = "SLA",
        ["complaint.submitted"] = "Submitted",
        ["complaint.actions"] = "Actions",
        ["form.status"] = "Status",
        ["form.search"] = "Search...",
        ["btn.view"] = "View",
        ["btn.view.all"] = "View All",
        ["btn.submit"] = "Submit",
        ["btn.save"] = "Save",
        ["btn.cancel"] = "Cancel",
        ["btn.delete"] = "Delete",
        ["btn.search"] = "Search",
        ["btn.clear"] = "Clear",
        ["btn.export.csv"] = "Export CSV",
        ["btn.reopen"] = "Reopen",
        ["btn.login"] = "Sign In",
        ["btn.logout"] = "Logout",
        ["status.pending"] = "Pending",
        ["status.under.review"] = "Under Review",
        ["status.in.progress"] = "In Progress",
        ["status.resolved"] = "Resolved",
        ["status.closed"] = "Closed",
        ["status.rejected"] = "Rejected",
        ["status.reopened"] = "Reopened",
        ["priority.critical"] = "Critical",
        ["priority.high"] = "High",
        ["priority.medium"] = "Medium",
        ["priority.low"] = "Low",
        ["sla.remaining"] = "remaining",
        ["sla.overdue"] = "Overdue",
        ["sla.breached"] = "SLA Breached",
        ["empty.no.complaints"] = "No complaints found",
        ["empty.no.notifications"] = "No notifications yet",
        ["error.login.failed"] = "Invalid email or password.",
        ["error.generic"] = "Something went wrong. Please try again.",
    };

    public async Task InitializeAsync()
    {
        if (_translations.Count > 0) return;

        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "i18n");
        var enDict = new Dictionary<string, string>();
        var urDict = new Dictionary<string, string>();

        try
        {
            var enPath = Path.Combine(basePath, "en.json");
            if (File.Exists(enPath))
            {
                var enJson = await File.ReadAllTextAsync(enPath);
                enDict = JsonSerializer.Deserialize<Dictionary<string, string>>(enJson) ?? new();
            }

            var urPath = Path.Combine(basePath, "ur.json");
            if (File.Exists(urPath))
            {
                var urJson = await File.ReadAllTextAsync(urPath);
                urDict = JsonSerializer.Deserialize<Dictionary<string, string>>(urJson) ?? new();
            }
        }
        catch { }

        // Merge fallback into English to fill any gaps
        foreach (var kvp in _englishFallback)
        {
            if (!enDict.ContainsKey(kvp.Key))
                enDict[kvp.Key] = kvp.Value;
        }

        _translations["en"] = enDict;
        _translations["ur"] = urDict;
    }

    public string Get(string key, string language = "en")
    {
        if (_translations.TryGetValue(language, out var dict) && dict.TryGetValue(key, out var value))
            return value;

        if (language != "en" && _translations.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out var enValue))
            return enValue;

        if (_englishFallback.TryGetValue(key, out var fallback))
            return fallback;

        var parts = key.Split('.');
        return string.Join(" ", parts.Select(p => char.ToUpper(p[0]) + p.Substring(1)));
    }

    public string this[string key] => Get(key);
    public string this[string key, string lang] => Get(key, lang);

    public bool HasKey(string key, string language)
    {
        return _translations.TryGetValue(language, out var dict) && dict.ContainsKey(key);
    }
}
