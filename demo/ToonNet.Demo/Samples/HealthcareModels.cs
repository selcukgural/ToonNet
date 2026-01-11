namespace ToonNet.Demo.Samples;

/// <summary>
/// Represents a patient's electronic medical record
/// </summary>
public class PatientRecord
{
    /// <summary>
    /// Medical record number (unique patient identifier)
    /// </summary>
    public string PatientId { get; set; } = string.Empty;
    
    /// <summary>
    /// Electronic medical record number
    /// </summary>
    public string RecordNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Date and time of hospital admission
    /// </summary>
    public DateTime AdmissionDate { get; set; }
    
    /// <summary>
    /// Date and time of hospital discharge (null if still admitted)
    /// </summary>
    public DateTime? DischargeDate { get; set; }
    
    /// <summary>
    /// Current patient status
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Patient demographic and contact information
    /// </summary>
    public PatientInfo PatientInfo { get; set; } = new();
    
    /// <summary>
    /// Collection of vital signs measurements
    /// </summary>
    public List<VitalSigns> VitalSigns { get; set; } = new();
    
    /// <summary>
    /// Medical diagnoses
    /// </summary>
    public List<Diagnosis> Diagnoses { get; set; } = new();
    
    /// <summary>
    /// Current and past medications
    /// </summary>
    public List<Medication> Medications { get; set; } = new();
    
    /// <summary>
    /// Laboratory test results
    /// </summary>
    public List<LabResult> LabResults { get; set; } = new();
    
    /// <summary>
    /// Medical procedures performed
    /// </summary>
    public List<Procedure> Procedures { get; set; } = new();
    
    /// <summary>
    /// Physicians assigned to patient care
    /// </summary>
    public List<Physician> AttendingPhysicians { get; set; } = new();
    
    /// <summary>
    /// Patient allergies and sensitivities
    /// </summary>
    public List<Allergy> Allergies { get; set; } = new();
}

/// <summary>
/// Patient demographic and personal information
/// </summary>
public class PatientInfo
{
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public string SSN { get; set; } = string.Empty;
    public EmergencyContact EmergencyContact { get; set; } = new();
    public Insurance Insurance { get; set; } = new();
}

/// <summary>
/// Emergency contact information
/// </summary>
public class EmergencyContact
{
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Insurance coverage information
/// </summary>
public class Insurance
{
    public string Provider { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string GroupNumber { get; set; } = string.Empty;
    public DateTime CoverageStartDate { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Vital signs measurement at a specific point in time
/// </summary>
public class VitalSigns
{
    public DateTime Timestamp { get; set; }
    public double Temperature { get; set; }
    public string TemperatureUnit { get; set; } = string.Empty;
    public BloodPressure BloodPressure { get; set; } = new();
    public int HeartRate { get; set; }
    public int RespiratoryRate { get; set; }
    public int OxygenSaturation { get; set; }
}

/// <summary>
/// Blood pressure reading
/// </summary>
public class BloodPressure
{
    public int Systolic { get; set; }
    public int Diastolic { get; set; }
    public string Unit { get; set; } = string.Empty;
}

/// <summary>
/// Medical diagnosis with ICD code
/// </summary>
public class Diagnosis
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime DateDiagnosed { get; set; }
    public string Severity { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; }
}

/// <summary>
/// Prescribed or administered medication
/// </summary>
public class Medication
{
    public string Name { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string PrescribedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> SideEffects { get; set; } = new();
}

/// <summary>
/// Laboratory test result
/// </summary>
public class LabResult
{
    public string TestName { get; set; } = string.Empty;
    public string TestCode { get; set; } = string.Empty;
    public DateTime OrderedDate { get; set; }
    public DateTime ResultDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, string> Results { get; set; } = new();
    public Dictionary<string, string>? Units { get; set; }
    public bool IsAbnormal { get; set; }
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Medical procedure performed on patient
/// </summary>
public class Procedure
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime PerformedDate { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Physician information
/// </summary>
public class Physician
{
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// Patient allergy or sensitivity
/// </summary>
public class Allergy
{
    public string Allergen { get; set; } = string.Empty;
    public string Reaction { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime OnsetDate { get; set; }
}
