using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Protocol.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace BaGet.Core
{
    public class DatabaseSearchService : ISearchService
    {
        private readonly IContext _context;
        private readonly IFrameworkCompatibilityService _frameworks;
        private readonly ISearchResponseBuilder _searchBuilder;
        private readonly IPackageService _packageService;
        private readonly BaGetOptions _options;

        public DatabaseSearchService(
            IContext context,
            IFrameworkCompatibilityService frameworks,
            ISearchResponseBuilder searchBuilder,
            IOptionsSnapshot<BaGetOptions> options,
            IPackageService packageService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _frameworks = frameworks ?? throw new ArgumentNullException(nameof(frameworks));
            _searchBuilder = searchBuilder ?? throw new ArgumentNullException(nameof(searchBuilder));
            _packageService = packageService;
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
        {
            var frameworks = GetCompatibleFrameworksOrNull(request.Framework);
            LogFileExtensions.WriteToFile(frameworks);

            IQueryable<Package> search = _context.Packages;
            LogFileExtensions.WriteToFile(search);

            search = ApplySearchQuery(search, request.Query);

            LogFileExtensions.WriteToFile(search);

            search = ApplySearchFilters(
                search,
                request.IncludePrerelease,
                request.IncludeSemVer2,
                request.PackageType,
                frameworks);

            LogFileExtensions.WriteToFile(search);

            var packageIds = search
                .Select(p => p.Id)
                .Distinct()
                .OrderBy(id => id)
                .Skip(request.Skip)
                .Take(request.Take);
            LogFileExtensions.WriteToFile(search);

            // This query MUST fetch all versions for each package that matches the search,
            // otherwise the results for a package's latest version may be incorrect.
            // If possible, we'll find all these packages in a single query by matching
            // the package IDs in a subquery. Otherwise, run two queries:
            //   1. Find the package IDs that match the search
            //   2. Find all package versions for these package IDs
            if (!packageIds.Any() && request.Query != null && _options.Mirror.Enabled)
            {
                var packages = await _packageService.SearchMirrorPackagesAsync(request.Query,
                                                                         new SearchFilter(request.IncludePrerelease),
                                                                         request.Skip,
                                                                         request.Take,
                                                                         cancellationToken);

                var groupMirrorResults = packages.GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                                                 .Select(group => new PackageRegistration(group.Key, group.ToList()))
                                                 .ToList();

                LogFileExtensions.WriteToFile(groupMirrorResults);

                return _searchBuilder.BuildSearch(groupMirrorResults);
            }



            if (_context.SupportsLimitInSubqueries)
            {
                search = _context.Packages.Where(p => packageIds.Contains(p.Id));
                LogFileExtensions.WriteToFile(search);

            }
            else
            {
                var packageIdResults = await packageIds.ToListAsync(cancellationToken);
                LogFileExtensions.WriteToFile(search);

                search = _context.Packages.Where(p => packageIdResults.Contains(p.Id));
                LogFileExtensions.WriteToFile(search);

            }

            search = ApplySearchFilters(
                search,
                request.IncludePrerelease,
                request.IncludeSemVer2,
                request.PackageType,
                frameworks);
            LogFileExtensions.WriteToFile(search);

            var results = await search.ToListAsync(cancellationToken);
            var groupedResults = results
                .GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => new PackageRegistration(group.Key, group.ToList()))
                .ToList();

            LogFileExtensions.WriteToFile(groupedResults);

            return _searchBuilder.BuildSearch(groupedResults);
        }

        public async Task<AutocompleteResponse> AutocompleteAsync(
            AutocompleteRequest request,
            CancellationToken cancellationToken)
        {
            IQueryable<Package> search = _context.Packages;

            search = ApplySearchQuery(search, request.Query);
            search = ApplySearchFilters(
                search,
                request.IncludePrerelease,
                request.IncludeSemVer2,
                request.PackageType,
                frameworks: null);

            var packageIds = await search
                .OrderByDescending(p => p.Downloads)
                .Select(p => p.Id)
                .Distinct()
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync(cancellationToken);

            return _searchBuilder.BuildAutocomplete(packageIds);
        }

        public async Task<AutocompleteResponse> ListPackageVersionsAsync(
            VersionsRequest request,
            CancellationToken cancellationToken)
        {
            var packageId = request.PackageId.ToLower();
            var search = _context
                .Packages
                .Where(p => p.Id.ToLower().Equals(packageId));

            search = ApplySearchFilters(
                search,
                request.IncludePrerelease,
                request.IncludeSemVer2,
                packageType: null,
                frameworks: null);

            var packageVersions = await search
                .Select(p => p.NormalizedVersionString)
                .ToListAsync(cancellationToken);

            return _searchBuilder.BuildAutocomplete(packageVersions);
        }

        public async Task<DependentsResponse> FindDependentsAsync(string packageId, CancellationToken cancellationToken)
        {
            var dependents = await _context
                .Packages
                .Where(p => p.Listed)
                .OrderByDescending(p => p.Downloads)
                .Where(p => p.Dependencies.Any(d => d.Id == packageId))
                .Take(20)
                .Select(r => new PackageDependent
                {
                    Id = r.Id,
                    Description = r.Description,
                    TotalDownloads = r.Downloads
                })
                .Distinct()
                .ToListAsync(cancellationToken);

            return _searchBuilder.BuildDependents(dependents);
        }

        private IQueryable<Package> ApplySearchQuery(IQueryable<Package> query, string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return query;
            }

            search = search.ToLowerInvariant();

            return query.Where(p => p.Id.ToLower().Contains(search));
        }

        private IQueryable<Package> ApplySearchFilters(
            IQueryable<Package> query,
            bool includePrerelease,
            bool includeSemVer2,
            string packageType,
            IReadOnlyList<string> frameworks)
        {
            if (!includePrerelease)
            {
                query = query.Where(p => !p.IsPrerelease);
            }

            if (!includeSemVer2)
            {
                query = query.Where(p => p.SemVerLevel != SemVerLevel.SemVer2);
            }

            if (!string.IsNullOrEmpty(packageType))
            {
                query = query.Where(p => p.PackageTypes.Any(t => t.Name == packageType));
            }

            if (frameworks != null)
            {
                query = query.Where(p => p.TargetFrameworks.Any(f => frameworks.Contains(f.Moniker)));
            }

            return query.Where(p => p.Listed);
        }

        private IReadOnlyList<string> GetCompatibleFrameworksOrNull(string framework)
        {
            if (framework == null) return null;

            return _frameworks.FindAllCompatibleFrameworks(framework);
        }

        public async Task<IReadOnlyList<NuGetVersion>> GetAllVersionsAsync(string packageId, CancellationToken cancellationToken)
        {
            var versions = await _context.Packages
                .Where(p => p.Id == packageId)
                .Select(p => p.Version)
                .ToListAsync(cancellationToken);

            return versions;
        }
    }
}
