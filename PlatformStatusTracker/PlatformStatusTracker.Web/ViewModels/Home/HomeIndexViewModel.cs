﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using PlatformStatusTracker.Core.Enum;
using PlatformStatusTracker.Core.Model;
using PlatformStatusTracker.Core.Repository;

namespace PlatformStatusTracker.Web.ViewModels.Home
{
    public class HomeIndexViewModel
    {
        public Dictionary<DateTime, ChangeSet> IeChangeSetsByDate { get; private set; }
        public Dictionary<DateTime, ChangeSet> ChromeChangeSetsByDate { get; private set; }
        public DateTime[] Dates { get; private set; }
        public DateTime LastUpdatedAt { get; private set; }

        public static async Task<HomeIndexViewModel> CreateAsync(IStatusDataRepository statusDataRepository)
        {
            var ieChangeSetsTask = GetChangeSetsByBrowser(statusDataRepository, StatusDataType.InternetExplorer);
            var chromeChangeSetsTask = GetChangeSetsByBrowser(statusDataRepository, StatusDataType.Chromium);
            var lastUpdatedTask = statusDataRepository.GetLastUpdated();
            await Task.WhenAll(ieChangeSetsTask, chromeChangeSetsTask, lastUpdatedTask);

            var ieChangeSets = ieChangeSetsTask.Result;
            var chromeChangeSets = chromeChangeSetsTask.Result;
            var lastUpdated = lastUpdatedTask.Result;

            return new HomeIndexViewModel()
                   {
                       IeChangeSetsByDate = ieChangeSets.ToDictionary(k => k.Date, v => v),
                       ChromeChangeSetsByDate = chromeChangeSets.ToDictionary(k => k.Date, v => v),
                       Dates = Enumerable.Concat(ieChangeSets, chromeChangeSets).Where(x => x.Changes.Any()).Select(x => x.Date).Distinct().ToArray(),
                       LastUpdatedAt = lastUpdated,
                   };
        }

        private static async Task<ChangeSet[]> GetChangeSetsByBrowser(IStatusDataRepository statusDataRepository, StatusDataType type)
        {
            var statusDataSet = (await statusDataRepository.GetPlatformStatusesRangeAsync(type, DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow, take: 30))
                                                           .OrderByDescending(x => x.Date)
                                                           .ToArray();

            if (statusDataSet.Length < 2)
            {
                return new ChangeSet[0];
            }

            return ChangeSet.GetChangeSetsFromPlatformStatuses(statusDataSet);
        }
    }
}