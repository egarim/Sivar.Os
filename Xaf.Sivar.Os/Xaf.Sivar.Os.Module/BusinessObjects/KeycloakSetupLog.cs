using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xaf.Sivar.Os.Module.BusinessObjects
{
    /// <summary>
    /// Singleton object to log Keycloak setup operations.
    /// Accessible from navigation and opens directly as a DetailView.
    /// </summary>
    [DefaultClassOptions]
    [NavigationItem("System")]
    [DefaultProperty(nameof(Title))]
    [RuleObjectExists("AnotherKeycloakSetupLogExists", DefaultContexts.Save, "True", InvertResult = true,
        CustomMessageTemplate = "Another Keycloak Setup Log already exists.")]
    [RuleCriteria("CannotDeleteKeycloakSetupLog", DefaultContexts.Delete, "False",
        CustomMessageTemplate = "Cannot delete the Keycloak Setup Log.")]
    public class KeycloakSetupLog
    {
        private Guid _id;
        private string _logText = string.Empty;
        private DateTime? _lastOperationAt;
        private string _lastOperationSummary = string.Empty;
        private bool _realmCreated;
        private bool _clientsCreated;
        private bool _userProfileConfigured;
        private bool _tokenMapperConfigured;

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
        public virtual string Title => "Keycloak Setup Log";

        /// <summary>
        /// Large text field to log all Keycloak setup operations, results, and any exceptions.
        /// </summary>
        [FieldSize(FieldSizeAttribute.Unlimited)]
        public virtual string LogText
        {
            get => _logText;
            set => _logText = value ?? string.Empty;
        }

        /// <summary>
        /// Timestamp of the last operation
        /// </summary>
        public virtual DateTime? LastOperationAt
        {
            get => _lastOperationAt;
            set => _lastOperationAt = value;
        }

        /// <summary>
        /// Brief summary of the last operation
        /// </summary>
        [FieldSize(500)]
        public virtual string LastOperationSummary
        {
            get => _lastOperationSummary;
            set => _lastOperationSummary = value ?? string.Empty;
        }

        /// <summary>
        /// Whether the realm has been created
        /// </summary>
        public virtual bool RealmCreated
        {
            get => _realmCreated;
            set => _realmCreated = value;
        }

        /// <summary>
        /// Whether clients have been created
        /// </summary>
        public virtual bool ClientsCreated
        {
            get => _clientsCreated;
            set => _clientsCreated = value;
        }

        /// <summary>
        /// Whether user profile attributes are configured
        /// </summary>
        public virtual bool UserProfileConfigured
        {
            get => _userProfileConfigured;
            set => _userProfileConfigured = value;
        }

        /// <summary>
        /// Whether token mapper is configured
        /// </summary>
        public virtual bool TokenMapperConfigured
        {
            get => _tokenMapperConfigured;
            set => _tokenMapperConfigured = value;
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
            var icon = success ? "✅" : "❌";
            LogText = $"[{timestamp}] {icon} COMPLETED: {operationName} - {summary}\n{LogText}";
            LastOperationSummary = $"{operationName}: {summary}";
            LastOperationAt = DateTime.UtcNow;
        }
    }
}
