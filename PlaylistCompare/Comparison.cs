using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Smylee.PlaylistMonitor.Library.Models;

namespace Smylee.PlaylistMonitor.PlaylistCompare {

    public static class Comparison {

        public static string Report(string playlistTitle, List<PlaylistItemsSnippetDb> cachedPlaylistItems, List<PlaylistItemsSnippetDb> currentPlaylistItems) {
            var deletedReport = DeletedReport(playlistTitle, cachedPlaylistItems, currentPlaylistItems);
            var addedReport = AddedReport(playlistTitle, cachedPlaylistItems, currentPlaylistItems);
            return deletedReport + " " + addedReport;
        }
        
        public static string DeletedReport(string playlistTitle, List<PlaylistItemsSnippetDb> cachedPlaylistItems, List<PlaylistItemsSnippetDb> currentPlaylistItems) {
            if (cachedPlaylistItems == null) return "Deleted video status will be in next report.";
            var items = ComparePlaylistsForDelete(cachedPlaylistItems, currentPlaylistItems);
            Console.WriteLine("deletedItems " + JsonConvert.SerializeObject(items));
            return items.Count <= 0 ? "No videos deleted." : GenerateEmailBody($"{playlistTitle} missing videos", items);
        }
        
        public static List<PlaylistItemsSnippetDb> ComparePlaylistsForDelete(List<PlaylistItemsSnippetDb> existingPlaylistList, List<PlaylistItemsSnippetDb> currentPlaylistItems) {
        
            // find all items that are in the existing list that are not in the current (deleted items)
            var currentItemIds = currentPlaylistItems.Select(x => x.Id).ToList();
            return existingPlaylistList.Where(item => !currentItemIds.Contains(item.Id)).ToList();
        } 
        
        public static string AddedReport(string playlistTitle, List<PlaylistItemsSnippetDb> existingPlaylistList, List<PlaylistItemsSnippetDb> currentPlaylistItemList) {
            if (existingPlaylistList == null) return "Added video status will be in next report.";
            var items = ComparePlaylistsForAdd(existingPlaylistList, currentPlaylistItemList);
            Console.WriteLine("addedItems " + JsonConvert.SerializeObject(items));
            return items.Count <= 0 ? "No new videos added." : GenerateEmailBody($"{playlistTitle} new videos", items);
        }

        public static List<PlaylistItemsSnippetDb> ComparePlaylistsForAdd(List<PlaylistItemsSnippetDb> existingPlaylistList, List<PlaylistItemsSnippetDb> currentPlaylistItemList) {

            // find all items that are in the current list that are not in the existing (added items)
            var existingItemIds = existingPlaylistList.Select(x => x.Id).ToList();
            return currentPlaylistItemList.Where(item => !existingItemIds.Contains(item.Id)).ToList();
        }

        public static string GenerateEmailBody(string header, List<PlaylistItemsSnippetDb> deletedItems) {
            var output = $"<h3>{header}</h3><br /><br />";
            foreach (var item in deletedItems) {
                output += $"<strong><a href=\"https://www.youtube.com/watch?v={item.Id}\">{item.Title}</a></strong><br />";
                output += $"by <a href=\"https://www.youtube.com/channel/{item.ChannelId}\">{item.ChannelTitle}</a></strong><br />"; 
                if (!string.IsNullOrEmpty(item.Description)) {
                    output += $"{item.Description}<br />";
                }
                output += "<br />";
            }
            return output;
        }
    }
}
