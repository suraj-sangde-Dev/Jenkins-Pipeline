using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using TeleICU.API.DTOs.CallDtos;
using TeleICU.API.Services;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Hubs;

/// <summary>
/// SignalR Hub for managing real-time video/audio calls between nurses and specialists
/// Implements the inVC P2P SDK signaling requirements
/// </summary>
[Authorize]
public class CallHub : Hub
{
    // Store active calls in memory for quick access (CallId -> EncounterData)
    private static readonly ConcurrentDictionary<string, EncounterData> _activeCalls = new();

    // Reconnection window in minutes
    private const int RECONNECTION_WINDOW_MINUTES = 30;

    private readonly ILogger<CallHub> _logger;
    private readonly EncounterStorageService _encounterStorage;
    private readonly ICallService _callService;
    public CallHub(ILogger<CallHub> logger, EncounterStorageService encounterStorage, ICallService callService)
    {
        _logger = logger;
        _encounterStorage = encounterStorage;
        _callService = callService;
    }

    /// <summary>
    /// Called when a user connects to the hub
    /// Maps userId to connectionId for direct messaging
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        await _encounterStorage.SaveUserConnectionAsync(userId, Context.ConnectionId);

        _logger.LogInformation("User {UserId} connected with ConnectionId {ConnectionId}", userId, Context.ConnectionId);

        // Notify user they are connected (maps to 'connected' callback in SDK)
        await Clients.Caller.SendAsync("Connected", new { userId, connectionId = Context.ConnectionId, timestamp = DateTime.UtcNow });

        await base.OnConnectedAsync();
    }

    public async Task SendNotification(string toUserId, string message)
    {
        var fromUserId = Context.UserIdentifier ?? Context.ConnectionId;
        await Clients.User(toUserId).SendAsync("ReceiveNotification", new
        {
            From = fromUserId,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Called when a user disconnects
    /// Mark calls as disconnected instead of ending them to allow reconnection
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        await _encounterStorage.RemoveUserConnectionAsync(userId);

        // Mark active calls as disconnected instead of ending them
        var userCalls = _activeCalls.Where(c =>
            (c.Value.CallerId == userId || c.Value.CalleeId == userId) &&
            c.Value.Status == CallStatus.Connected).ToList();

        foreach (var call in userCalls)
        {
            var encounterData = call.Value;
            encounterData.Status = CallStatus.Disconnected;
            encounterData.DisconnectedTime = DateTime.UtcNow;

            // Update in Redis
            await _encounterStorage.UpdateEncounterAsync(encounterData.EncounterId, encounterData);

            // Notify the other participant about disconnection
            var otherUserId = encounterData.CallerId == userId ? encounterData.CalleeId : encounterData.CallerId;
            var otherConnectionId = await _encounterStorage.GetUserConnectionAsync(otherUserId);

            if (!string.IsNullOrEmpty(otherConnectionId))
            {
                await Clients.Client(otherConnectionId).SendAsync("ParticipantDisconnected", new
                {
                    callId = call.Key,
                    encounterId = encounterData.EncounterId,
                    disconnectedUserId = userId,
                    reconnectionWindowMinutes = RECONNECTION_WINDOW_MINUTES,
                    timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation("User {UserId} disconnected from call {CallId}, encounter {EncounterId}. Reconnection available for {Minutes} minutes.",
                userId, call.Key, encounterData.EncounterId, RECONNECTION_WINDOW_MINUTES);
        }

        _logger.LogInformation("User {UserId} disconnected", userId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Initiate a call to another user (maps to startCall method in SDK)
    /// </summary>
    public async Task StartCall(CallRequestDto request)
    {
        try
        {
            var callId = await _callService.InitiateCallAsync(request);
            var encounterId = Guid.NewGuid().ToString(); // Generate unique encounter ID

            var encounterData = new EncounterData
            {
                CallId = callId,
                EncounterId = encounterId,
                CallerId = request.CallerId,
                CallerName = request.CallerName,
                CalleeId = request.CalleeId,
                CalleeName = request.CalleeName,
                CaseId = request.CaseId,
                PatientId = request.PatientId,
                CallType = request.CallType,
                StartTime = DateTime.UtcNow,
                Status = CallStatus.Initiated
            };

            _activeCalls[callId] = encounterData;

            // Save to Redis with 30-minute TTL
            await _encounterStorage.SaveEncounterAsync(encounterId, encounterData);

            // Check if callee is online
            var calleeConnectionId = await _encounterStorage.GetUserConnectionAsync(request.CalleeId);
            if (!string.IsNullOrEmpty(calleeConnectionId))
            {
                // Send incoming call notification to callee (maps to 'incoming' callback in SDK)
                await Clients.Client(calleeConnectionId).SendAsync("IncomingCall", new
                {
                    callId,
                    encounterId,
                    from = request.CallerId,
                    fromName = request.CallerName,
                    callType = request.CallType.ToString(),
                    caseId = request.CaseId,
                    patientId = request.PatientId,
                    timestamp = DateTime.UtcNow
                });

                // Notify caller that call is ringing
                await Clients.Caller.SendAsync("CallRinging", new { callId, encounterId, to = request.CalleeId });

                encounterData.Status = CallStatus.Ringing;
                await _encounterStorage.UpdateEncounterAsync(encounterId, encounterData);
                _logger.LogInformation("Call {CallId} initiated from {Caller} to {Callee}", callId, request.CallerId, request.CalleeId);
            }
            else
            {
                // Callee is offline
                await Clients.Caller.SendAsync("CallFailed", new { callId, reason = "User is offline" });
                _activeCalls.TryRemove(callId, out _);
                _logger.LogWarning("Call failed: User {CalleeId} is offline", request.CalleeId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting call");
            await Clients.Caller.SendAsync("CallFailed", new { reason = "Internal error" });
        }
    }

    /// <summary>
    /// Accept an incoming call (maps to acceptCall method in SDK)
    /// </summary>
    public async Task AcceptCall(string callId)
    {
        try
        {
            if (_activeCalls.TryGetValue(callId, out var encounterData))
            {
                encounterData.Status = CallStatus.Connected;
                encounterData.ConnectedTime = DateTime.UtcNow;

                // Update in Redis
                await _encounterStorage.UpdateEncounterAsync(encounterData.EncounterId, encounterData);

                // Notify caller that call was accepted (maps to 'callaccepted' callback in SDK)
                var callerConnectionId = await _encounterStorage.GetUserConnectionAsync(encounterData.CallerId);
                if (!string.IsNullOrEmpty(callerConnectionId))
                {
                    await Clients.Client(callerConnectionId).SendAsync("CallAccepted", new
                    {
                        callId,
                        encounterId = encounterData.EncounterId,
                        acceptedBy = encounterData.CalleeId,
                        timestamp = DateTime.UtcNow
                    });
                }

                // Notify callee
                await Clients.Caller.SendAsync("CallConnected", new { callId, encounterId = encounterData.EncounterId, timestamp = DateTime.UtcNow });

                _logger.LogInformation("Call {CallId} accepted", callId);
            }
            else
            {
                await Clients.Caller.SendAsync("CallError", new { callId, reason = "Call not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting call {CallId}", callId);
            await Clients.Caller.SendAsync("CallError", new { callId, reason = "Internal error" });
        }
    }

    /// <summary>
    /// Reject an incoming call (maps to call denial in SDK)
    /// </summary>
    public async Task RejectCall(string callId)
    {
        try
        {
            if (_activeCalls.TryGetValue(callId, out var encounterData))
            {
                encounterData.Status = CallStatus.Rejected;
                encounterData.EndTime = DateTime.UtcNow;

                // Notify caller that call was rejected (maps to 'calldenied' callback in SDK)
                var callerConnectionId = await _encounterStorage.GetUserConnectionAsync(encounterData.CallerId);
                if (!string.IsNullOrEmpty(callerConnectionId))
                {
                    await Clients.Client(callerConnectionId).SendAsync("CallDenied", new
                    {
                        callId,
                        rejectedBy = encounterData.CalleeId,
                        timestamp = DateTime.UtcNow
                    });
                }

                _activeCalls.TryRemove(callId, out _);
                await _encounterStorage.DeleteEncounterAsync(encounterData.EncounterId);
                _logger.LogInformation("Call {CallId} rejected", callId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting call {CallId}", callId);
        }
    }

    /// <summary>
    /// End an active call (maps to endMeeting method in SDK)
    /// </summary>
    public async Task EndCall(string callId)
    {
        try
        {
            if (_activeCalls.TryGetValue(callId, out var encounterData))
            {
                encounterData.Status = CallStatus.Ended;
                encounterData.EndTime = DateTime.UtcNow.AddHours(5.5);

                // Notify both participants
                var participants = new[] { encounterData.CallerId, encounterData.CalleeId };
                foreach (var userId in participants)
                {
                    var connectionId = await _encounterStorage.GetUserConnectionAsync(userId);
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        await Clients.Client(connectionId).SendAsync("CallEnded", new
                        {
                            callId,
                            endedBy = Context.UserIdentifier,
                            duration = (encounterData.EndTime - encounterData.ConnectedTime)?.TotalSeconds ?? 0,
                            timestamp = DateTime.UtcNow.AddHours(5.5)
                        });
                    }
                }
                await _callService.UpdateCallStatusAsync(callId, CallStatus.Ended);
                await _callService.UpdateCallEndTimeAsync(callId, encounterData.EndTime.Value);

                _activeCalls.TryRemove(callId, out _);

                // Remove from Redis
                await _encounterStorage.DeleteEncounterAsync(encounterData.EncounterId);

                _logger.LogInformation("Call {CallId} ended", callId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending call {CallId}", callId);
        }
    }

    /// <summary>
    /// Send WebRTC signaling data (ICE candidates, SDP offers/answers)
    /// Required for P2P connection establishment
    /// </summary>
    public async Task SendSignal(CallSignalDto signal)
    {
        try
        {
            var connectionId = await _encounterStorage.GetUserConnectionAsync(signal.ToUserId);
            if (!string.IsNullOrEmpty(connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveSignal", signal);
                _logger.LogDebug("Signal sent from {From} to {To}, Type: {Type}", signal.FromUserId, signal.ToUserId, signal.Type);
            }
            else
            {
                await Clients.Caller.SendAsync("SignalError", new { reason = "Recipient not connected" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending signal");
            await Clients.Caller.SendAsync("SignalError", new { reason = "Internal error" });
        }
    }

    /// <summary>
    /// Notify media control changes (maps to mute callback in SDK)
    /// </summary>
    public async Task MediaControl(MediaControlDto control)
    {
        try
        {
            if (_activeCalls.TryGetValue(control.CallId, out var callInfo))
            {
                var otherUserId = control.UserId == callInfo.CallerId ? callInfo.CalleeId : callInfo.CallerId;

                var connectionId = await _encounterStorage.GetUserConnectionAsync(otherUserId);
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await Clients.Client(connectionId).SendAsync("MediaControlChanged", new
                    {
                        callId = control.CallId,
                        userId = control.UserId,
                        type = control.Type.ToString().ToLower(),
                        enabled = control.Enabled
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in media control");
        }
    }

    /// <summary>
    /// Send translation captions (maps to pushingCaptions callback in SDK)
    /// </summary>
    public async Task SendTranslation(TranslationDto translation)
    {
        try
        {
            if (_activeCalls.TryGetValue(translation.CallId, out var callInfo))
            {
                var otherUserId = translation.UserId == callInfo.CallerId ? callInfo.CalleeId : callInfo.CallerId;

                var connectionId = await _encounterStorage.GetUserConnectionAsync(otherUserId);
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveTranslation", translation);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending translation");
        }
    }

    /// <summary>
    /// Get list of online users
    /// </summary>
    public async Task<List<string>> GetOnlineUsers()
    {
        return await _encounterStorage.GetOnlineUsersAsync();
    }

    /// <summary>
    /// Check if a specific user is online
    /// </summary>
    public async Task<bool> IsUserOnline(string userId)
    {
        var connectionId = await _encounterStorage.GetUserConnectionAsync(userId);
        return !string.IsNullOrEmpty(connectionId);
    }

    /// <summary>
    /// Reconnect to an existing encounter within the 30-minute window
    /// </summary>
    public async Task ReconnectCall(ReconnectCallDto request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.EncounterId))
            {
                await Clients.Caller.SendAsync("ReconnectFailed", new { reason = "EncounterId is required" });
                return;
            }

            // Find the encounter from Redis
            var encounterData = await _encounterStorage.GetEncounterAsync(request.EncounterId);
            if (encounterData == null)
            {
                await Clients.Caller.SendAsync("ReconnectFailed", new { reason = "Encounter not found or has expired" });
                _logger.LogWarning("Reconnect failed: Encounter {EncounterId} not found or expired", request.EncounterId);
                return;
            }

            // Verify user is part of this encounter
            if (request.UserId != encounterData.CallerId && request.UserId != encounterData.CalleeId)
            {
                await Clients.Caller.SendAsync("ReconnectFailed", new { reason = "User not authorized for this encounter" });
                _logger.LogWarning("Reconnect failed: User {UserId} not authorized for encounter {EncounterId}",
                    request.UserId, request.EncounterId);
                return;
            }

            // Update call status back to connected
            encounterData.Status = CallStatus.Connected;
            encounterData.DisconnectedTime = null;

            // Update in Redis and in-memory cache
            await _encounterStorage.UpdateEncounterAsync(encounterData.EncounterId, encounterData);
            _activeCalls[encounterData.CallId] = encounterData;

            // Notify the reconnecting user
            await Clients.Caller.SendAsync("ReconnectSuccessful", new
            {
                callId = encounterData.CallId,
                encounterId = encounterData.EncounterId,
                callerId = encounterData.CallerId,
                callerName = encounterData.CallerName,
                calleeId = encounterData.CalleeId,
                calleeName = encounterData.CalleeName,
                caseId = encounterData.CaseId,
                patientId = encounterData.PatientId,
                callType = encounterData.CallType.ToString(),
                timestamp = DateTime.UtcNow
            });

            // Notify the other participant if they're online
            var otherUserId = request.UserId == encounterData.CallerId ? encounterData.CalleeId : encounterData.CallerId;
            var otherConnectionId = await _encounterStorage.GetUserConnectionAsync(otherUserId);
            if (!string.IsNullOrEmpty(otherConnectionId))
            {
                await Clients.Client(otherConnectionId).SendAsync("ParticipantReconnected", new
                {
                    callId = encounterData.CallId,
                    encounterId = encounterData.EncounterId,
                    reconnectedUserId = request.UserId,
                    reconnectedUserName = request.UserName,
                    timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation("User {UserId} successfully reconnected to encounter {EncounterId}",
                request.UserId, request.EncounterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconnecting to encounter {EncounterId}", request.EncounterId);
            await Clients.Caller.SendAsync("ReconnectFailed", new { reason = "Internal error" });
        }
    }

    /// <summary>
    /// Get encounter status - useful for checking if reconnection is possible
    /// </summary>
    public async Task<object?> GetEncounterStatus(string encounterId)
    {
        try
        {
            var encounterData = await _encounterStorage.GetEncounterAsync(encounterId);
            if (encounterData != null)
            {
                var minutesRemaining = await _encounterStorage.GetRemainingMinutesAsync(encounterId);
                var canReconnect = minutesRemaining > 0;

                return new
                {
                    encounterId,
                    callId = encounterData.CallId,
                    status = encounterData.Status.ToString(),
                    canReconnect,
                    minutesRemaining,
                    callerId = encounterData.CallerId,
                    calleeId = encounterData.CalleeId,
                    disconnectedTime = encounterData.DisconnectedTime
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting encounter status for {EncounterId}", encounterId);
            return null;
        }
    }

    /// <summary>
    /// Broadcast prescription sync to both participants in an encounter
    /// </summary>
    public async Task<bool> BroadcastPrescriptionSync(string encounterId, object prescriptionData)
    {
        try
        {
            if (string.IsNullOrEmpty(encounterId))
            {
                _logger.LogWarning("BroadcastPrescriptionSync called with empty encounterId");
                return false;
            }

            var encounterData = await _encounterStorage.GetEncounterAsync(encounterId);
            if (encounterData == null)
            {
                _logger.LogWarning("Encounter {EncounterId} not found for prescription sync", encounterId);
                return false;
            }

            // Get connection IDs for both participants
            var callerConnectionId = await _encounterStorage.GetUserConnectionAsync(encounterData.CallerId);
            var calleeConnectionId = await _encounterStorage.GetUserConnectionAsync(encounterData.CalleeId);

            var notificationsSent = 0;

            // Send to caller if online
            if (!string.IsNullOrEmpty(callerConnectionId))
            {
                await Clients.Client(callerConnectionId).SendAsync("PrescriptionSynced", prescriptionData);
                notificationsSent++;
            }

            // Send to callee if online
            if (!string.IsNullOrEmpty(calleeConnectionId))
            {
                await Clients.Client(calleeConnectionId).SendAsync("PrescriptionSynced", prescriptionData);
                notificationsSent++;
            }

            _logger.LogInformation("Prescription sync broadcasted to {Count} participants in encounter {EncounterId}",
                notificationsSent, encounterId);

            return notificationsSent > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting prescription sync for encounter {EncounterId}", encounterId);
            return false;
        }
    }

}
