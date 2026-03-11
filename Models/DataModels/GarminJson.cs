using System.Text.Json.Serialization;

public class GarminJson
{
    [JsonPropertyName("activityId")]
    public long ActivityId { get; set; }

    [JsonPropertyName("activityUUID")]
    public ActivityUuid? ActivityUuid { get; set; }

    [JsonPropertyName("activityName")]
    public string? ActivityName { get; set; }

    [JsonPropertyName("userProfileId")]
    public long UserProfileId { get; set; }

    [JsonPropertyName("isMultiSportParent")]
    public bool IsMultiSportParent { get; set; }

    [JsonPropertyName("activityTypeDTO")]
    public TypeDto? ActivityTypeDto { get; set; }

    [JsonPropertyName("eventTypeDTO")]
    public TypeDto? EventTypeDto { get; set; }

    [JsonPropertyName("accessControlRuleDTO")]
    public AccessControlRuleDto? AccessControlRuleDto { get; set; }

    [JsonPropertyName("timeZoneUnitDTO")]
    public TimeZoneUnitDto? TimeZoneUnitDto { get; set; }

    [JsonPropertyName("metadataDTO")]
    public MetadataDto? MetadataDto { get; set; }

    [JsonPropertyName("summaryDTO")]
    public SummaryDto? SummaryDto { get; set; }

    [JsonPropertyName("locationName")]
    public string? LocationName { get; set; }
}

public class AccessControlRuleDto
{
    [JsonPropertyName("typeId")]
    public long TypeId { get; set; }

    [JsonPropertyName("typeKey")]
    public string? TypeKey { get; set; }
}

public class TypeDto
{
    [JsonPropertyName("typeId")]
    public long TypeId { get; set; }

    [JsonPropertyName("typeKey")]
    public string? TypeKey { get; set; }

    [JsonPropertyName("parentTypeId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ParentTypeId { get; set; }

    [JsonPropertyName("sortOrder")]
    public long? SortOrder { get; set; }
}

public class ActivityUuid
{
    [JsonPropertyName("uuid")]
    public Guid Uuid { get; set; }
}

public class MetadataDto
{
    [JsonPropertyName("isOriginal")]
    public bool IsOriginal { get; set; }

    [JsonPropertyName("deviceApplicationInstallationId")]
    public long DeviceApplicationInstallationId { get; set; }

    [JsonPropertyName("agentApplicationInstallationId")]
    public object? AgentApplicationInstallationId { get; set; }

    [JsonPropertyName("agentString")]
    public object? AgentString { get; set; }

    [JsonPropertyName("fileFormat")]
    public FileFormat? FileFormat { get; set; }

    [JsonPropertyName("associatedCourseId")]
    public long? AssociatedCourseId { get; set; }

    [JsonPropertyName("lastUpdateDate")]
    public DateTimeOffset LastUpdateDate { get; set; }

    [JsonPropertyName("uploadedDate")]
    public DateTimeOffset UploadedDate { get; set; }

    [JsonPropertyName("videoUrl")]
    public object? VideoUrl { get; set; }

    [JsonPropertyName("hasPolyline")]
    public bool HasPolyline { get; set; }

    [JsonPropertyName("hasChartData")]
    public bool HasChartData { get; set; }

    [JsonPropertyName("hasHrTimeInZones")]
    public bool HasHrTimeInZones { get; set; }

    [JsonPropertyName("hasPowerTimeInZones")]
    public bool HasPowerTimeInZones { get; set; }

    [JsonPropertyName("userInfoDto")]
    public UserInfoDto? UserInfoDto { get; set; }

    [JsonPropertyName("chartAvailability")]
    public ChartAvailability? ChartAvailability { get; set; }

    [JsonPropertyName("childIds")]
    public object[]? ChildIds { get; set; }

    [JsonPropertyName("childActivityTypes")]
    public object[]? ChildActivityTypes { get; set; }

    [JsonPropertyName("sensors")]
    public Sensor[]? Sensors { get; set; }

    [JsonPropertyName("activityImages")]
    public object[]? ActivityImages { get; set; }

    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; set; }

    [JsonPropertyName("diveNumber")]
    public object? DiveNumber { get; set; }

    [JsonPropertyName("lapCount")]
    public long LapCount { get; set; }

    [JsonPropertyName("associatedWorkoutId")]
    public object? AssociatedWorkoutId { get; set; }

    [JsonPropertyName("isAtpActivity")]
    public object? IsAtpActivity { get; set; }

    [JsonPropertyName("deviceMetaDataDTO")]
    public DeviceMetaDataDto? DeviceMetaDataDto { get; set; }

    [JsonPropertyName("hasIntensityIntervals")]
    public bool HasIntensityIntervals { get; set; }

    [JsonPropertyName("hasSplits")]
    public bool HasSplits { get; set; }

    [JsonPropertyName("personalRecord")]
    public bool PersonalRecord { get; set; }

    [JsonPropertyName("elevationCorrected")]
    public bool ElevationCorrected { get; set; }

    [JsonPropertyName("trimmed")]
    public bool Trimmed { get; set; }

    [JsonPropertyName("favorite")]
    public bool Favorite { get; set; }

    [JsonPropertyName("manualActivity")]
    public bool ManualActivity { get; set; }

    [JsonPropertyName("autoCalcCalories")]
    public bool AutoCalcCalories { get; set; }

    [JsonPropertyName("gcj02")]
    public bool Gcj02 { get; set; }
}

public class ChartAvailability
{
    [JsonPropertyName("showDistance")]
    public bool ShowDistance { get; set; }

    [JsonPropertyName("showDuration")]
    public bool ShowDuration { get; set; }

    [JsonPropertyName("showElevation")]
    public bool ShowElevation { get; set; }

    [JsonPropertyName("showHeartRate")]
    public bool ShowHeartRate { get; set; }

    [JsonPropertyName("showMovingDuration")]
    public bool ShowMovingDuration { get; set; }

    [JsonPropertyName("showMovingSpeed")]
    public bool ShowMovingSpeed { get; set; }

    [JsonPropertyName("showSpeed")]
    public bool ShowSpeed { get; set; }

    [JsonPropertyName("showTimestamp")]
    public bool ShowTimestamp { get; set; }
}

public class DeviceMetaDataDto
{
    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("deviceTypePk")]
    public long DeviceTypePk { get; set; }

    [JsonPropertyName("deviceVersionPk")]
    public long DeviceVersionPk { get; set; }
}

public class FileFormat
{
    [JsonPropertyName("formatId")]
    public long FormatId { get; set; }

    [JsonPropertyName("formatKey")]
    public string? FormatKey { get; set; }
}

public class Sensor
{
    [JsonPropertyName("sku")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sku { get; set; }

    [JsonPropertyName("softwareVersion")]
    public double SoftwareVersion { get; set; }

    [JsonPropertyName("localDeviceType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LocalDeviceType { get; set; }
}

public class UserInfoDto
{
    [JsonPropertyName("userProfilePk")]
    public long UserProfilePk { get; set; }

    [JsonPropertyName("displayname")]
    public Guid Displayname { get; set; }

    [JsonPropertyName("fullname")]
    public string? Fullname { get; set; }

    [JsonPropertyName("profileImageUrlLarge")]
    public object? ProfileImageUrlLarge { get; set; }

    [JsonPropertyName("profileImageUrlMedium")]
    public Uri? ProfileImageUrlMedium { get; set; }

    [JsonPropertyName("profileImageUrlSmall")]
    public Uri? ProfileImageUrlSmall { get; set; }

    [JsonPropertyName("userPro")]
    public bool UserPro { get; set; }
}

public class SummaryDto
{
    [JsonPropertyName("startTimeLocal")]
    public DateTimeOffset StartTimeLocal { get; set; }

    [JsonPropertyName("startTimeGMT")]
    public DateTimeOffset StartTimeGmt { get; set; }

    [JsonPropertyName("startLatitude")]
    public double StartLatitude { get; set; }

    [JsonPropertyName("startLongitude")]
    public double StartLongitude { get; set; }

    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    [JsonPropertyName("movingDuration")]
    public double MovingDuration { get; set; }

    [JsonPropertyName("elapsedDuration")]
    public double ElapsedDuration { get; set; }

    [JsonPropertyName("elevationGain")]
    public double ElevationGain { get; set; }

    [JsonPropertyName("elevationLoss")]
    public double ElevationLoss { get; set; }

    [JsonPropertyName("maxElevation")]
    public double MaxElevation { get; set; }

    [JsonPropertyName("minElevation")]
    public double MinElevation { get; set; }

    [JsonPropertyName("averageSpeed")]
    public double AverageSpeed { get; set; }

    [JsonPropertyName("averageMovingSpeed")]
    public double AverageMovingSpeed { get; set; }

    [JsonPropertyName("maxSpeed")]
    public double MaxSpeed { get; set; }

    [JsonPropertyName("calories")]
    public double Calories { get; set; }

    [JsonPropertyName("averageHR")]
    public double AverageHr { get; set; }

    [JsonPropertyName("maxHR")]
    public double MaxHr { get; set; }

    [JsonPropertyName("averageRunCadence")]
    public double AverageRunCadence { get; set; }

    [JsonPropertyName("maxRunCadence")]
    public double MaxRunCadence { get; set; }

    [JsonPropertyName("trainingEffect")]
    public double TrainingEffect { get; set; }

    [JsonPropertyName("anaerobicTrainingEffect")]
    public double AnaerobicTrainingEffect { get; set; }

    [JsonPropertyName("aerobicTrainingEffectMessage")]
    public string? AerobicTrainingEffectMessage { get; set; }

    [JsonPropertyName("anaerobicTrainingEffectMessage")]
    public string? AnaerobicTrainingEffectMessage { get; set; }

    [JsonPropertyName("endLatitude")]
    public double EndLatitude { get; set; }

    [JsonPropertyName("endLongitude")]
    public double EndLongitude { get; set; }

    [JsonPropertyName("maxVerticalSpeed")]
    public double MaxVerticalSpeed { get; set; }

    [JsonPropertyName("waterEstimated")]
    public double WaterEstimated { get; set; }

    [JsonPropertyName("minActivityLapDuration")]
    public double MinActivityLapDuration { get; set; }
}

public class TimeZoneUnitDto
{
    [JsonPropertyName("unitId")]
    public long UnitId { get; set; }

    [JsonPropertyName("unitKey")]
    public string? UnitKey { get; set; }

    [JsonPropertyName("factor")]
    public double Factor { get; set; }

    [JsonPropertyName("timeZone")]
    public string? TimeZone { get; set; }
}
