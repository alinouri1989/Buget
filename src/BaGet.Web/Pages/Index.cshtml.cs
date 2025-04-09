using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core;
using BaGet.Protocol.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BaGet.Web
{
    public class IndexModel : PageModel
    {
        private readonly ISearchService _search;
        private readonly IPackageDeletionService _packageDeletionService;

        public IndexModel(ISearchService search, IPackageDeletionService packageDeletionService, IOptionsSnapshot<BaGetOptions> options)
        {
            _search = search ?? throw new ArgumentNullException(nameof(search));
            _packageDeletionService = packageDeletionService ?? throw new ArgumentNullException(nameof(packageDeletionService));
            IndexModelHelpers.ResultsPerPage = options.Value.PageNumber.HasValue ? options.Value.PageNumber.Value : 100;
        }

        [BindProperty(Name = "q", SupportsGet = true)]
        public string Query { get; set; }

        [BindProperty(Name = "p", SupportsGet = true)]
        [Range(1, int.MaxValue)]
        public int PageIndex { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string PackageType { get; set; } = "any";

        [BindProperty(SupportsGet = true)]
        public string Framework { get; set; } = "any";

        [BindProperty(SupportsGet = true)]
        public bool Prerelease { get; set; } = true;

        public IReadOnlyList<SearchResult> Packages { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest();

            var packageType = PackageType == "any" ? null : PackageType;
            var framework = Framework == "any" ? null : Framework;

            var search = await _search.SearchAsync(
                new SearchRequest
                {
                    Skip = (PageIndex - 1) * IndexModelHelpers.ResultsPerPage,
                    Take = IndexModelHelpers.ResultsPerPage,
                    IncludePrerelease = Prerelease,
                    IncludeSemVer2 = true,
                    PackageType = packageType,
                    Framework = framework,
                    Query = Query,
                },
                cancellationToken);
            LogFileExtensions.WriteToFile(search.Data);
            Packages = search.Data;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync([FromForm] List<string> selectedPackages, [FromForm] List<string> versions, CancellationToken cancellationToken)
        {
            if (selectedPackages != null && selectedPackages.Any())
            {
                foreach (var packageId in selectedPackages)
                {
                    var packageVersions = await _search.GetAllVersionsAsync(packageId, cancellationToken);

                    foreach (var version in packageVersions)
                    {
                        await _packageDeletionService.TryDeletePackageAsync(packageId, version, cancellationToken);
                    }
                }
            }

            return RedirectToPage(new { q = Query, p = PageIndex, packageType = PackageType, framework = Framework, prerelease = Prerelease });
        }
    }
}
