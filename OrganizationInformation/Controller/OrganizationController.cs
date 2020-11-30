using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OrganizationInformation.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;
using Microsoft.FeatureManagement;
using Microsoft.Extensions.Configuration;

namespace OrganizationInformation_MemoryCache.Controller
{
    [ApiController]
    [Route("repositories")]
    
    public class RepositoryController : ControllerBase
    {
        private IFeatureManager _featureManager;
        public RepositoryController(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }
        private static readonly HttpClient client = new HttpClient();

        [HttpGet]
        [Route("{company}")]
        public async Task<List<Repository>> ProcessRepositories(string company, [FromServices] IMemoryCache _cache, [FromServices] IConfiguration _config)
        {
            float cacheExpiration = float.Parse(_config.GetSection("Settings").GetSection("CacheExpirationTimeSeconds").Value);
            if (await _featureManager.IsEnabledAsync("FeatureCache"))
            {
                var cacheEntry = await _cache.GetOrCreateAsync(company, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                    entry.SetPriority(CacheItemPriority.High);
                    return await ProcessarRepositories(company);
                });
                return cacheEntry;
            }
            else return await ProcessarRepositories(company);
        }
        private async Task<List<Repository>> ProcessarRepositories(string company)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            var streamTask = client.GetStreamAsync($"https://api.github.com/orgs/{company}/repos");
            var repositories = await JsonSerializer.DeserializeAsync<List<Repository>>(await streamTask);
            
            return repositories;
        }
     }
}