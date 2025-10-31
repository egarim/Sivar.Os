
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Xaf.Sivar.Os.Module.BusinessObjects
{
    /// <summary>
    /// SQL Script entity for database-driven script execution system
    /// Allows flexible ordering using decimal values (1.0, 1.5, 2.0, etc.)
    /// </summary>
    [DefaultClassOptions]
    [DefaultProperty(nameof(Name))]
    public class SqlScript : BaseObject
    {
        // Identification
        [Required]
        [MaxLength(200)]
        public virtual string Name { get; set; }

        [Required]
        public virtual string Description { get; set; }

        // SQL Content
        [Required]
        [FieldSize(FieldSizeAttribute.Unlimited)]
        [DevExpress.ExpressApp.Model.ModelDefault("RowCount", "15")]
        //[EditorAlias("HtmlPropertyEditor")]
        public virtual string SqlText { get; set; }

        // Execution Control
        [Required]
        public virtual decimal ExecutionOrder { get; set; }

        [Required]
        [MaxLength(100)]
        public virtual string BatchName { get; set; }

        [Required]
        public virtual bool IsActive { get; set; } = true;

        [Required]
        public virtual bool RunOnce { get; set; } = true;

        // Execution Tracking
        public virtual DateTime? LastExecutedAt { get; set; }

        [Required]
        public virtual int ExecutionCount { get; set; } = 0;

        public virtual string LastExecutionError { get; set; }

        // Override ToString for better display in XAF
        public override string ToString()
        {
            return $"{ExecutionOrder:0.0} - {Name} ({BatchName})";
        }
    }

    /// <summary>
    /// Batch name constants for SqlScript execution points
    /// </summary>
    public static class SqlScriptBatches
    {
        public const string BeforeSchemaUpdate = "BeforeSchemaUpdate";
        public const string AfterSchemaUpdate = "AfterSchemaUpdate";
        public const string CustomMaintenance = "CustomMaintenance";
    }
}
