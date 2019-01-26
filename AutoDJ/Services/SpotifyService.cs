using AutoDJ.Models.Requests;
using AutoDJ.Models.Responses;
using AutoDJ.Models.Spotify;
using AutoDJ.Options;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDJ.Services
{
    public class SpotifyService : ISpotifyService
    {
        private readonly IHttpClient _httpClient;
        private readonly string _playbackPlaylistUrl;
        private readonly string _spotifyBaseUrl;
        private readonly string _authorizationCode;
        private readonly string _refreshToken;
        private readonly SemaphoreSlim _tokenLock;

        private string _accessToken;
        private DateTime _accessTokenExpiry;

        private const string AUTH_URI = "https://accounts.spotify.com/api/token";
        private const string CURRENT_TRACK_PATH = "/v1/me/player/currently-playing";
        private const string GET_PLAYLIST_PATH = "/v1/playlists/{0}";
        private const string SET_PLAYLIST_PATH = "/v1/playlists/{0}/tracks";

        public SpotifyService(IHttpClient httpClient, IOptions<SpotifyOptions> options)
        {
            _httpClient = httpClient;
            _playbackPlaylistUrl = options.Value.PlaybackPlaylistId;
            _spotifyBaseUrl = options.Value.SpotifyBaseUrl;
            _authorizationCode = options.Value.AuthorizationCode;
            _refreshToken = options.Value.RefreshToken;

            _tokenLock = new SemaphoreSlim(1, 1);
        }

        public async Task<Track> GetCurrentTrack()
        {
            await RefreshTokenIfRequired();

            var response = await SendRequest<CurrentlyPlayingResponse>(CURRENT_TRACK_PATH, HttpMethod.Get, null);
            return response.Item;
        }

        public async Task<ICollection<Track>> GetPlaylistContent(string playlistId)
        {
            await RefreshTokenIfRequired();

            return await GetPlaylistContentInternal(string.Format(GET_PLAYLIST_PATH, playlistId));
        }

        public async Task SetPlaylistContent(ICollection<string> trackIds)
        {
            await RefreshTokenIfRequired();

            var taskUris = trackIds.Select(t => $"spotify:track:{t}").ToList();
            var payload = new SetPlaylistContentRequest
            {
                URIs = taskUris
            };

            await SendRequest(string.Format(SET_PLAYLIST_PATH, _playbackPlaylistUrl), HttpMethod.Put, payload);
        }

        private async Task<ICollection<Track>> GetPlaylistContentInternal(string url)
        {
            var tracks = new List<Track>();

            var response = await SendRequest<Playlist>(url, HttpMethod.Get, null);
            tracks.AddRange(response.Tracks.Items.Select(t => t.Track));

            if (response.Tracks.Next != null)
            {
                tracks.AddRange(await GetPlaylistContentInternal(response.Tracks.Next));
            }

            return tracks;
        }

        private async Task<T> SendRequest<T>(string path, HttpMethod method, object payload)
        {
            var content = await SendRequest(path, method, payload);
            return JsonConvert.DeserializeObject<T>(content);
        }

        private async Task<string> SendRequest(string path, HttpMethod method, object content)
        {
            using (var request = new HttpRequestMessage())
            {
                var uri = new Uri(_spotifyBaseUrl);
                uri = new Uri(uri, path);

                request.RequestUri = uri;
                request.Method = method;
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                if (content != null)
                {
                    request.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
                }

                var response = await _httpClient.Send(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"An error occured processing the request: {responseContent}. Status code: {response.StatusCode}");
                }

                return responseContent;
            }
        }

        private async Task RefreshTokenIfRequired()
        {
            if (_accessToken == null || _accessTokenExpiry < DateTime.UtcNow)
            {
                await _tokenLock.WaitAsync();

                try
                {
                    if (_accessToken == null || _accessTokenExpiry < DateTime.UtcNow)
                    {
                        _accessToken = await RefreshToken();
                        _accessTokenExpiry = DateTime.UtcNow.AddMinutes(50);
                    }
                }
                finally
                {
                    _tokenLock.Release();
                }
            }
        }

        private async Task<string> RefreshToken()
        {
            using (var request = new HttpRequestMessage())
            {
                var uri = new Uri(AUTH_URI);

                var content = new Dictionary<string, string>
                {
                    {"grant_type", "refresh_token"},
                    {"refresh_token", _refreshToken}
                };

                request.RequestUri = uri;
                request.Method = HttpMethod.Post;
                request.Content = new FormUrlEncodedContent(content.ToList());
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _authorizationCode);

                var response = await _httpClient.Send(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Unable to refresh token: {responseContent}. Status code: {response.StatusCode}");
                }

                return JsonConvert.DeserializeObject<dynamic>(responseContent).access_token;
            }
        }
    }
}
