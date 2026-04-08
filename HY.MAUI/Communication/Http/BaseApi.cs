using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace HY.MAUI.Communication.Http
{
    public abstract class BaseApi
    {
        protected readonly HttpClient Http;

        protected BaseApi(HttpClient http)
        {
            Http = http;
        }

        protected async Task<Response?> GetAsync(string url, CancellationToken ct = default)
        {
            try
            {
                var resp = await Http.GetAsync(url, ct);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadFromJsonAsync<Response>(cancellationToken: ct);
            }
            catch (Exception e)
            {
                return new Response(false, e.Message);
            }
        }

        protected async Task<Response?> DeleteAsync(string url, CancellationToken ct = default)
        {
            try
            {
                var resp = await Http.DeleteAsync(url, ct);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadFromJsonAsync<Response>(cancellationToken: ct);
            }
            catch (Exception e)
            {
                return new Response(false, e.Message);
            }
        }

        protected async Task<Response?> PostAsync(string url, HttpContent? content, CancellationToken ct = default)
        {
            try
            {
                var resp = await Http.PostAsync(url, content, ct);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadFromJsonAsync<Response>(cancellationToken: ct);
            }
            catch (Exception e)
            {
                return new Response(false, e.Message);
            }
        }

        protected async Task<Response?> PostAsJsonAsync<TReq>(string url, TReq data, CancellationToken ct = default)
        {
            try
            {
                var resp = await Http.PostAsJsonAsync(url, data, ct);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadFromJsonAsync<Response>(cancellationToken: ct);
            }
            catch (Exception e)
            {
                return new Response(false, e.Message);
            }
        }

        protected async Task<Response?> PatchAsJsonAsync<TReq>(string url, TReq data, CancellationToken ct = default)
        {
            try
            {
                var resp = await Http.PatchAsJsonAsync(url, data, ct);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadFromJsonAsync<Response>(cancellationToken: ct);
            }
            catch (Exception e)
            {
                return new Response(false, e.Message);
            }
        }
    }
}
