using Dapper;
using TeleICU.API.DTOs.CallDtos;
using TeleICU.API.Helpers;
using TeleICU.API.Repository.Interface;

namespace TeleICU.API.Repository;

public class CallRepository : ICallRepository
{
    private readonly DapperContext _context;

    public CallRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<string> CreateCallLogAsync(CallRequestDto request)
    {
        var callId = Guid.NewGuid().ToString();
        var query = @"
            INSERT INTO CallLogs (
                CallId, CallerId, CallerName, CalleeId, CalleeName, 
                CaseId, PatientId, CallType, Status, StartTime, CreatedDate
            ) VALUES (
                @CallId, @CallerId, @CallerName, @CalleeId, @CalleeName,
                @CaseId, @PatientId, @CallType, @Status, @StartTime, @CreatedDate
            )";

        using var connection = _context.CreateConnection();
        
        // Parse IDs with error handling
        if (!int.TryParse(request.CallerId, out int callerId))
            throw new ArgumentException($"Invalid CallerId: {request.CallerId}");
        
        if (!int.TryParse(request.CalleeId, out int calleeId))
            throw new ArgumentException($"Invalid CalleeId: {request.CalleeId}");
        
        int? caseId = null;
        if (!string.IsNullOrEmpty(request.CaseId) && int.TryParse(request.CaseId, out int parsedCaseId))
            caseId = parsedCaseId;
        
        int? patientId = null;
        if (!string.IsNullOrEmpty(request.PatientId) && int.TryParse(request.PatientId, out int parsedPatientId))
            patientId = parsedPatientId;
        
        await connection.ExecuteAsync(query, new
        {
            CallId = callId,
            CallerId = callerId,
            request.CallerName,
            CalleeId = calleeId,
            request.CalleeName,
            CaseId = caseId,
            PatientId = patientId,
            CallType = (int)request.CallType,
            Status = (int)CallStatus.Initiated,
            StartTime = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        });

        return callId;
    }

    public async Task UpdateCallStatusAsync(string callId, CallStatus status)
    {
        var query = @"
            UPDATE CallLogs 
            SET Status = @Status, UpdatedDate = @UpdatedDate
            WHERE CallId = @CallId";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(query, new
        {
            CallId = callId,
            Status = (int)status,
            UpdatedDate = DateTime.UtcNow.AddHours(5.5)
        });
    }

    public async Task UpdateCallEndTimeAsync(string callId, DateTime endTime)
    {
        var query = @"
            UPDATE CallLogs 
            SET EndTime = @EndTime, UpdatedDate = @UpdatedDate
            WHERE CallId = @CallId";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(query, new
        {
            CallId = callId,
            EndTime = endTime,
            UpdatedDate = DateTime.UtcNow.AddHours(5.5)
        });
    }

    public async Task<object?> GetCallHistoryAsync(string userId, int limit = 50)
    {
        var query = @"
            SELECT 
                CallId,
                CallerId,
                CallerName,
                CalleeId,
                CalleeName,
                CaseId,
                PatientId,
                CallType,
                Status,
                StartTime,
                EndTime,
                RecordingUrl
            FROM CallLogs
            WHERE CallerId = @UserId OR CalleeId = @UserId
            ORDER BY StartTime DESC
            LIMIT @Limit";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync(query, new { UserId = int.Parse(userId), Limit = limit });
        return result;
    }

    public async Task<object?> GetLatestCallAsync(int caseId)
    {
        var query = @"
            SELECT CallId
            FROM CallLogs
            WHERE CaseId = @caseId
            ORDER BY StartTime DESC
            LIMIT 1;";

        using var connection = _context.CreateConnection();
        var history = await connection.QueryFirstOrDefaultAsync<object>(query, new { CaseId = caseId });
        return history;
    }

    public async Task<object?> GetActiveCallsAsync(string userId)
    {
        var query = @"
            SELECT 
                CallId,
                CallerId,
                CallerName,
                CalleeId,
                CalleeName,
                CaseId,
                PatientId,
                CallType,
                Status,
                StartTime
            FROM CallLogs
            WHERE (CallerId = @UserId OR CalleeId = @UserId)
            AND Status IN (@Initiated, @Ringing, @Connected)
            ORDER BY StartTime DESC";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryAsync(query, new
        {
            UserId = int.Parse(userId),
            Initiated = (int)CallStatus.Initiated,
            Ringing = (int)CallStatus.Ringing,
            Connected = (int)CallStatus.Connected
        });
        return result;
    }

    public async Task SaveCallRecordingAsync(string callId, string recordingUrl)
    {
        var query = @"
            UPDATE CallLogs 
            SET RecordingUrl = @RecordingUrl, UpdatedDate = @UpdatedDate
            WHERE CallId = @CallId";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(query, new
        {
            CallId = callId,
            RecordingUrl = recordingUrl,
            UpdatedDate = DateTime.UtcNow
        });
    }

    public async Task<ConsultationSummaryResponse> GetConsultationSummaryAsync(ConsultationSummaryRequest request, int userId, string role)
    {
        using var connection = _context.CreateConnection();

        // Build base query with joins
        var baseQuery = @"
            FROM CallLogs cl
            LEFT JOIN Cases c ON cl.CaseId = c.CaseId
            LEFT JOIN Patients p ON cl.PatientId = p.PatientId OR (c.PatientId = p.PatientId)
            LEFT JOIN Spokes s ON p.SpokeId = s.SpokeId
            LEFT JOIN Users specialist ON (cl.CalleeId = specialist.UserId AND specialist.RoleId = 5) 
                                       OR (cl.CallerId = specialist.UserId AND specialist.RoleId = 5)
            LEFT JOIN SpecialistsMapping sm ON specialist.UserId = sm.UserId
            LEFT JOIN CentersOfExcellence coe ON sm.CoeId = coe.CoeId OR s.CoeId = coe.CoeId
            LEFT JOIN CasePreAdmitMedications cpm ON cl.CallId = cpm.CallId
            LEFT JOIN CaseHealthRecords chr ON c.CaseId = chr.CaseId AND chr.RecordType = 'Prescription'
            WHERE 1=1";

        var parameters = new DynamicParameters();

        // Role-based filtering
        if (role == "nurse" || role == "doctor")
        {
            // Get user's spoke
            var spokeId = await connection.ExecuteScalarAsync<int?>(
                "SELECT SpokeId FROM Users WHERE UserId = @UserId",
                new { UserId = userId });
            
            if (spokeId.HasValue)
            {
                baseQuery += " AND p.SpokeId = @SpokeId";
                parameters.Add("@SpokeId", spokeId.Value);
            }
            else
            {
                // If no spoke, return empty result
                return new ConsultationSummaryResponse
                {
                    Data = new List<ConsultationSummaryDto>(),
                    TotalRecords = 0,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = 0
                };
            }
        }
        else if (role == "specialist")
        {
            // Specialists see consultations where they are the caller or callee
            baseQuery += " AND (cl.CallerId = @SpecialistUserId OR cl.CalleeId = @SpecialistUserId)";
            parameters.Add("@SpecialistUserId", userId);
        }
        else if (role == "coe_admin")
        {
            // COE admin sees consultations for their COE
            var coeId = await connection.ExecuteScalarAsync<int?>(
                "SELECT CoeId FROM CoeAdminsMapping WHERE UserId = @UserId",
                new { UserId = userId });
            
            if (coeId.HasValue)
            {
                baseQuery += " AND (coe.CoeId = @CoeId OR s.CoeId = @CoeId)";
                parameters.Add("@CoeId", coeId.Value);
            }
            else
            {
                return new ConsultationSummaryResponse
                {
                    Data = new List<ConsultationSummaryDto>(),
                    TotalRecords = 0,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = 0
                };
            }
        }
        else if (role == "state_admin")
        {
            // State admin sees consultations for their state
            var stateId = await connection.ExecuteScalarAsync<int?>(
                "SELECT StateId FROM StateAdminsMapping WHERE UserId = @UserId",
                new { UserId = userId });
            
            if (stateId.HasValue)
            {
                baseQuery += " AND (s.StateId = @StateId OR coe.StateId = @StateId)";
                parameters.Add("@StateId", stateId.Value);
            }
            else
            {
                return new ConsultationSummaryResponse
                {
                    Data = new List<ConsultationSummaryDto>(),
                    TotalRecords = 0,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = 0
                };
            }
        }

        // Apply filters from request
        if (request.PatientId.HasValue)
        {
            baseQuery += " AND p.PatientId = @PatientId";
            parameters.Add("@PatientId", request.PatientId.Value);
        }

        if (request.Date.HasValue)
        {
            baseQuery += " AND DATE(cl.StartTime) = DATE(@Date)";
            parameters.Add("@Date", request.Date.Value);
        }

        if (request.CoEId.HasValue)
        {
            baseQuery += " AND coe.CoeId = @RequestCoeId";
            parameters.Add("@RequestCoeId", request.CoEId.Value);
        }

        if (request.SpecialistId.HasValue)
        {
            baseQuery += " AND specialist.UserId = @SpecialistId";
            parameters.Add("@SpecialistId", request.SpecialistId.Value);
        }

        // Only show consultations with CaseId (actual consultations, not just calls)
        baseQuery += " AND cl.CaseId IS NOT NULL";

        // Get total count with proper grouping
        var countQuery = "SELECT COUNT(DISTINCT cl.CallId) " + baseQuery;
        var totalRecords = await connection.ExecuteScalarAsync<int>(countQuery, parameters);

        // Calculate pagination
        var totalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize);
        var offset = (request.Page - 1) * request.PageSize;

        // Build data query with proper grouping for ConsultationId
        // Always use ConsultationId from medications, fallback to CallId only if no ConsultationId exists
        var dataQuery = @"
            SELECT 
                CASE 
                    WHEN MAX(cpm.ConsultationId) IS NOT NULL AND MAX(cpm.ConsultationId) != '' THEN MAX(cpm.ConsultationId)
                    ELSE cl.CallId
                END AS ConsultationId,
                MAX(p.PatientId) AS PatientId,
                MAX(CONCAT(p.FirstName, ' ', COALESCE(p.MiddleName, ''), ' ', COALESCE(p.LastName, ''))) AS Patient,
                MAX(CONCAT(COALESCE(specialist.Title, ''), ' ', COALESCE(specialist.FirstName, ''), ' ', COALESCE(specialist.LastName, ''))) AS Specialist,
                MAX(COALESCE(coe.CoeName, '')) AS CoE,
                MAX(cl.StartTime) AS StartDateTime,
                MAX(cl.EndTime) AS EndDateTime,
                MAX(cl.Status) AS StatusCode,
                CASE 
                    WHEN MAX(chr.CaseHealthRecordId) IS NOT NULL THEN 1 
                    WHEN MAX(cpm.CasePreAdmitMedicationId) IS NOT NULL THEN 1 
                    ELSE 0 
                END AS HasPrescription
            " + baseQuery + @"
            GROUP BY cl.CallId
            ORDER BY MAX(cl.StartTime) DESC
            LIMIT @Offset, @PageSize";

        parameters.Add("@Offset", offset);
        parameters.Add("@PageSize", request.PageSize);

        var results = await connection.QueryAsync<dynamic>(dataQuery, parameters);

        // Convert to DTOs
        var consultations = results.Select((r, index) =>
        {
            var duration = TimeSpan.Zero;
            if (r.EndDateTime != null && r.StartDateTime != null)
            {
                duration = ((DateTime)r.EndDateTime) - ((DateTime)r.StartDateTime);
            }

            var statusText = r.StatusCode switch
            {
                1 => "Initiated",
                2 => "Ringing",
                3 => "Connected",
                4 => "Completed",
                5 => "Rejected",
                6 => "Missed",
                7 => "Failed",
                8 => "Disconnected",
                _ => "Unknown"
            };

            return new ConsultationSummaryDto
            {
                SrNo = offset + index + 1,
                ConsultationId = r.ConsultationId,
                PatientId = r.PatientId,
                Patient = r.Patient?.Trim() ?? "",
                Specialist = r.Specialist?.Trim() ?? "",
                CoE = r.CoE ?? "",
                StartDateTime = r.StartDateTime,
                EndDateTime = r.EndDateTime,
                Duration = duration.TotalSeconds > 0 
                    ? $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}"
                    : "00:00:00",
                Status = statusText,
                HasPrescription = r.HasPrescription == 1
            };
        }).ToList();

        return new ConsultationSummaryResponse
        {
            Data = consultations,
            TotalRecords = totalRecords,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = totalPages
        };
    }

    public async Task<CallPrescriptionDataDto?> GetCallDataForPrescriptionAsync(string callId)
    {
        var query = @"
            SELECT 
                cl.CallId,
                COALESCE(MAX(cpm.ConsultationId), cl.CallId) AS ConsultationId,
                CONCAT(COALESCE(specialist.Title, ''), ' ', COALESCE(specialist.FirstName, ''), ' ', COALESCE(specialist.LastName, '')) AS SpecialistName,
                COALESCE(specialist.Title, '') AS SpecialistTitle,
                CONCAT(COALESCE(sender.Title, ''), ' ', COALESCE(sender.FirstName, ''), ' ', COALESCE(sender.LastName, '')) AS SenderName,
                CONCAT(
                    COALESCE(sender.AddressLine1, ''),
                    CASE WHEN sender.AddressLine2 IS NOT NULL THEN CONCAT(', ', sender.AddressLine2) ELSE '' END,
                    CASE WHEN ci.CityName IS NOT NULL THEN CONCAT(', ', ci.CityName) ELSE '' END,
                    CASE WHEN d.DistrictName IS NOT NULL THEN CONCAT(', ', d.DistrictName) ELSE '' END,
                    CASE WHEN st.StateName IS NOT NULL THEN CONCAT(', ', st.StateName) ELSE '' END,
                    CASE WHEN sender.PIN IS NOT NULL THEN CONCAT(', ', sender.PIN) ELSE '' END
                ) AS SenderAddress,
                cl.StartTime AS ConsultationDateTime
            FROM CallLogs cl
            LEFT JOIN Cases c ON cl.CaseId = c.CaseId
            LEFT JOIN Patients p ON cl.PatientId = p.PatientId OR (c.PatientId = p.PatientId)
            LEFT JOIN Spokes s ON p.SpokeId = s.SpokeId
            LEFT JOIN Users specialist ON (cl.CalleeId = specialist.UserId AND specialist.RoleId = 5) 
                                       OR (cl.CallerId = specialist.UserId AND specialist.RoleId = 5)
            LEFT JOIN Users sender ON (cl.CallerId = sender.UserId AND sender.RoleId != 5) 
                                    OR (cl.CalleeId = sender.UserId AND sender.RoleId != 5)
            LEFT JOIN Cities ci ON ci.CityCode = sender.City
            LEFT JOIN Districts d ON d.DistrictCode = sender.District
            LEFT JOIN States st ON st.StateCode = sender.State
            LEFT JOIN CasePreAdmitMedications cpm ON cl.CallId = cpm.CallId
            WHERE cl.CallId = @CallId
            GROUP BY cl.CallId, specialist.Title, specialist.FirstName, specialist.LastName,
                     sender.Title, sender.FirstName, sender.LastName, sender.AddressLine1, 
                     sender.AddressLine2, ci.CityName, d.DistrictName, st.StateName, sender.PIN, cl.StartTime";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryFirstOrDefaultAsync<CallPrescriptionDataDto>(query, new { CallId = callId });
        return result;
    }

    public async Task<ConsultationInfoDto?> GetCallIdAndCaseIdFromConsultationIdAsync(string consultationId)
    {
        // First try to find from CasePreAdmitMedications (ConsultationId format: CONS/...)
        var query1 = @"
            SELECT DISTINCT cpm.CallId, cpm.CaseId, cpm.ConsultationId
            FROM CasePreAdmitMedications cpm
            WHERE cpm.ConsultationId = @ConsultationId
            LIMIT 1";

        using var connection = _context.CreateConnection();
        var result = await connection.QueryFirstOrDefaultAsync<ConsultationInfoDto>(query1, new { ConsultationId = consultationId });
        
        // If not found, try CallId (fallback for old records without ConsultationId)
        if (result == null)
        {
            var query2 = @"
                SELECT cl.CallId, cl.CaseId, @ConsultationId AS ConsultationId
                FROM CallLogs cl
                WHERE cl.CallId = @ConsultationId
                LIMIT 1";
            
            result = await connection.QueryFirstOrDefaultAsync<ConsultationInfoDto>(query2, new { ConsultationId = consultationId });
        }

        return result;
    }

    public async Task<string?> GetConsultationIdFromCallIdAsync(string callId)
    {
        using var connection = _context.CreateConnection();
        
        // Get ConsultationId from CasePreAdmitMedications where CallId matches
        var consultationId = await connection.ExecuteScalarAsync<string>(
            @"SELECT DISTINCT ConsultationId 
              FROM CasePreAdmitMedications 
              WHERE CallId = @CallId AND ConsultationId IS NOT NULL AND ConsultationId != ''
              LIMIT 1",
            new { CallId = callId });

        // If not found, return null (will use CallId as fallback)
        return consultationId;
    }
}
