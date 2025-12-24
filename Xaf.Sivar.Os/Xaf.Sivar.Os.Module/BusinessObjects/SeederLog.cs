using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xaf.Sivar.Os.Module.BusinessObjects
{
    /// <summary>
    /// Singleton object to log seeder operations.
    /// Accessible from navigation and opens directly as a DetailView.
    /// </summary>
    [DefaultClassOptions]
    [NavigationItem("System")]
    [DefaultProperty(nameof(Title))]
    [RuleObjectExists("AnotherSeederLogExists", DefaultContexts.Save, "True", InvertResult = true,
        CustomMessageTemplate = "Another Seeder Log already exists.")]
    [RuleCriteria("CannotDeleteSeederLog", DefaultContexts.Delete, "False",
        CustomMessageTemplate = "Cannot delete the Seeder Log.")]
    public class SeederLog
    {
        private Guid _id;
        private string _logText = string.Empty;
        private DateTime? _lastOperationAt;
        private string _lastOperationSummary = string.Empty;
        private int _keycloakUsersSynced;
        private int _profilesSeeded;
        private int _profilesLinked;

        [Key]
        [Browsable(false)]
        public virtual Guid Id
        {
            get => _id;
            set => _id = value;
        }

        /// <summary>
        /// Display title for the singleton
        /// </summary>
        [NotMapped]
        public virtual string Title => "Seeder Log";

        /// <summary>
        /// Large text field to log all seeder operations, results, and any exceptions.
        /// </summary>
        [FieldSize(FieldSizeAttribute.Unlimited)]
        public virtual string LogText
        {
            get => _logText;
            set => _logText = value ?? string.Empty;
        }

        /// <summary>
        /// Timestamp of the last seeding operation
        /// </summary>
        public virtual DateTime? LastOperationAt
        {
            get => _lastOperationAt;
            set => _lastOperationAt = value;
        }

        /// <summary>
        /// Summary of the last operation performed
        /// </summary>
        [FieldSize(500)]
        public virtual string LastOperationSummary
        {
            get => _lastOperationSummary;
            set => _lastOperationSummary = value ?? string.Empty;
        }

        /// <summary>
        /// Total number of users synced to Keycloak
        /// </summary>
        public virtual int KeycloakUsersSynced
        {
            get => _keycloakUsersSynced;
            set => _keycloakUsersSynced = value;
        }

        /// <summary>
        /// Total number of profiles seeded
        /// </summary>
        public virtual int ProfilesSeeded
        {
            get => _profilesSeeded;
            set => _profilesSeeded = value;
        }

        /// <summary>
        /// Total number of profiles linked to users
        /// </summary>
        public virtual int ProfilesLinked
        {
            get => _profilesLinked;
            set => _profilesLinked = value;
        }

        /// <summary>
        /// Append a log entry with timestamp
        /// </summary>
        public void AppendLog(string message)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            LogText = $"[{timestamp}] {message}\n{LogText}";
            LastOperationAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Append an exception to the log
        /// </summary>
        public void AppendException(Exception ex, string context)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            LogText = $"[{timestamp}] ❌ ERROR in {context}:\n{ex.Message}\n{ex.StackTrace}\n\n{LogText}";
            LastOperationAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Clear the log and reset counters
        /// </summary>
        public void ClearLog()
        {
            LogText = string.Empty;
            LastOperationSummary = "Log cleared";
            LastOperationAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Start a new operation section in the log
        /// </summary>
        public void StartOperation(string operationName)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            var separator = new string('=', 60);
            LogText = $"\n{separator}\n[{timestamp}] 🚀 STARTING: {operationName}\n{separator}\n{LogText}";
        }

        /// <summary>
        /// End an operation section in the log
        /// </summary>
        public void EndOperation(string operationName, bool success, string summary)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            var status = success ? "✅ COMPLETED" : "❌ FAILED";
            var separator = new string('-', 60);
            LogText = $"[{timestamp}] {status}: {operationName}\n{summary}\n{separator}\n\n{LogText}";
            LastOperationSummary = $"{(success ? "✅" : "❌")} {operationName}: {summary}";
            LastOperationAt = DateTime.UtcNow;
        }
    }
}
