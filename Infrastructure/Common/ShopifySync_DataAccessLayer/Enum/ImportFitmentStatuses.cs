using System;

namespace ShopifySync_DataAccessLayer.Enum
{
    /// <summary>
    /// Centralized string statuses used by ImportFitment entities.
    /// Use these constants instead of hard-coded literals or enum.ToString().
    /// </summary>
    public static class ImportFitmentStatuses
    {
        public const string Imported = "Imported";
        public const string Preprocessing = "Preprocessing"; // active processing state
        public const string PreprocessingQueued = "PreprocessingQueued"; // waiting in queue
        public const string Preprocessed = "Preprocessed";
        public const string Finalizing = "Finalizing";       // active processing state
        public const string FinalizingQueued = "FinalizingQueued"; // waiting in queue
        public const string Completed = "Completed";
        public const string Error = "Error";
    }
}
