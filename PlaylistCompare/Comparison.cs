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

        public static List<PlaylistItemsSnippetDb> ComparePlaylistsForDelete(List<PlaylistItemsSnippetDb> existingPlaylistList, List<PlaylistItemsSnippetDb> currentPlaylistItems) {
        
            // find all items that are in the existing list that are not in the current (deleted items)
            var currentItemIds = currentPlaylistItems.Select(x => x.Id).ToList();
            return existingPlaylistList.Where(item => !currentItemIds.Contains(item.Id)).ToList();
        }

        public static List<PlaylistItemsSnippetDb> DeletedItems(List<PlaylistItemsSnippetDb> cachedPlaylistItems, List<PlaylistItemsSnippetDb> currentPlaylistItems) {
            if (cachedPlaylistItems == null) return new List<PlaylistItemsSnippetDb>();
            var items = ComparePlaylistsForDelete(cachedPlaylistItems, currentPlaylistItems);
            Console.WriteLine("deletedItems " + JsonConvert.SerializeObject(items));
            return items;
        }
        
        public static List<PlaylistItemsSnippetDb> ComparePlaylistsForAdd(List<PlaylistItemsSnippetDb> existingPlaylistList, List<PlaylistItemsSnippetDb> currentPlaylistItemList) {

            // find all items that are in the current list that are not in the existing (added items)
            var existingItemIds = existingPlaylistList.Select(x => x.Id).ToList();
            return currentPlaylistItemList.Where(item => !existingItemIds.Contains(item.Id)).ToList();
        }

        public static List<PlaylistItemsSnippetDb> AddedItems(List<PlaylistItemsSnippetDb> existingPlaylistList, List<PlaylistItemsSnippetDb> currentPlaylistItemList) {
            if (existingPlaylistList == null) return new List<PlaylistItemsSnippetDb>();
            var items = ComparePlaylistsForAdd(existingPlaylistList, currentPlaylistItemList);
            Console.WriteLine("addedItems " + JsonConvert.SerializeObject(items));
            return items;
        }

    }
}
