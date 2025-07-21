using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using TrelloClone.Shared.DTOs;

namespace TrelloClone.Client.Services
{
    public interface IInvitationService
    {
        Task<List<BoardInvitationDto>> GetPendingInvitations();
        Task AcceptInvitation(Guid invitationId);
        Task DeclineInvitation(Guid invitationId);
        Task SendInvitation(Guid boardId, string invitedEmail, PermissionLevel permission = PermissionLevel.Editor);
    }

    public class InvitationService : IInvitationService
    {
        private readonly HttpClient _http;
        private readonly AuthenticationStateProvider _authStateProvider;

        public InvitationService(HttpClient http, AuthenticationStateProvider authStateProvider)
        {
            _http = http;
            _authStateProvider = authStateProvider;
        }

        public async Task<List<BoardInvitationDto>> GetPendingInvitations()
        {
            var userId = await GetCurrentUserId();
            if (userId == Guid.Empty) return new List<BoardInvitationDto>();

            return await _http.GetFromJsonAsync<List<BoardInvitationDto>>($"api/invitations?userId={userId}")
                   ?? new List<BoardInvitationDto>();
        }

        public async Task AcceptInvitation(Guid invitationId)
        {
            var userId = await GetCurrentUserId();
            if (userId == Guid.Empty) return;

            await _http.PatchAsync($"api/invitations/{invitationId}/accept?userId={userId}", null);
        }

        public async Task DeclineInvitation(Guid invitationId)
        {
            var userId = await GetCurrentUserId();
            if (userId == Guid.Empty) return;

            await _http.PatchAsync($"api/invitations/{invitationId}/decline?userId={userId}", null);
        }

        private async Task<Guid> GetCurrentUserId()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userId, out var id) ? id : Guid.Empty;
        }

        public async Task SendInvitation(Guid boardId, string invitedUsername, PermissionLevel permission = PermissionLevel.Editor)
        {
            var userId = await GetCurrentUserId();
            if (userId == Guid.Empty) return;

            var request = new
            {
                BoardId = boardId,
                Username = invitedUsername,
                PermissionLevel = permission
            };

            await _http.PostAsJsonAsync("api/invitations", request);
        }
    }
}