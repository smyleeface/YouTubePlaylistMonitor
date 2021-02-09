using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Smylee.PlaylistMonitor.Library.Models;

namespace Smylee.PlaylistMonitor.PlaylistCompare {

    public static class Comparison {

        public static bool IsSame(this List<PlaylistItemsSnippetDb> listPlaylistItems, List<PlaylistItemsSnippetDb> compareList) {
            foreach (var playlistItem in listPlaylistItems) {
                var foundId = compareList.Exists(x => x.Id == playlistItem.Id);
                var foundChannelId = compareList.Exists(x => x.ChannelId == playlistItem.ChannelId);
                if (!foundId || !foundChannelId) {
                    return false;
                }
            }
            return true;
        }

        public static List<PlaylistItemsSnippetDb> DeletedItems(List<PlaylistItemsSnippetDb> cachedPlaylistItems, List<PlaylistItemsSnippetDb> currentPlaylistItems) {
            if (cachedPlaylistItems == null) return new List<PlaylistItemsSnippetDb>();
            var currentItemIds = currentPlaylistItems.Select(x => x.Id).ToList();
            var items = cachedPlaylistItems.Where(item => !currentItemIds.Contains(item.Id)).ToList();
            Console.WriteLine("deletedItems " + JsonConvert.SerializeObject(items));
            return items;
        }

        public static List<PlaylistItemsSnippetDb> AddedItems(List<PlaylistItemsSnippetDb> existingPlaylistList, List<PlaylistItemsSnippetDb> currentPlaylistItemList) {
            if (existingPlaylistList == null) return new List<PlaylistItemsSnippetDb>();
            var existingItemIds = existingPlaylistList.Select(x => x.Id).ToList();
            var items  = currentPlaylistItemList.Where(item => !existingItemIds.Contains(item.Id)).ToList();
            Console.WriteLine("addedItems " + JsonConvert.SerializeObject(items));
            return items;
        }

    }
}
