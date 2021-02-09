using System.Collections.Generic;
using System.Linq;
using Smylee.PlaylistMonitor.Library.Models;

namespace Smylee.PlaylistMonitor.PlaylistCompare {

    public class GenerateEmail {

        private string _emailBody;
        private readonly string _subjectTitle;

        public GenerateEmail(string subjectTitle) {
            _subjectTitle = subjectTitle;
            _emailBody = "";
        }

        public string Html => $"<div style=\"font-family: Arial;\"><h1>{_subjectTitle}</h1>{_emailBody}</div>";

        public void AddCard(string playlistTitle, string playlistAuthor, List<PlaylistItemsSnippetDb> deletedItems, List<PlaylistItemsSnippetDb> addedItems) {
            var headerContent = CardHeader(playlistTitle, playlistAuthor);
            var bodyContent = CardBody(deletedItems, addedItems);
            var footerContent = CardFooter(deletedItems, addedItems);
            _emailBody += $"<div style=\"border: 1px solid grey;\">{headerContent}{bodyContent}{footerContent}</div>";
        }

        private string CardHeader(string playlistTitle, string playlistAuthor) {
            return $"<div style=\"background-color: whitesmoke; padding: 10px 15px;\"><h2 style=\"margin: 0;\">{playlistTitle}</h2><div class=\"byline\">by {playlistAuthor}</div></div>";
        }

        private string CardBody(List<PlaylistItemsSnippetDb> deletedItems, List<PlaylistItemsSnippetDb> addedItems) {
            var playlistItems = "";
            var index = 0;
            var deleteStopItemCount = deletedItems.Count;
            var deleteStopCounter = 0;
            var combinedItems = deletedItems.Concat(addedItems).ToList();
            foreach (var itemGroup in combinedItems.GroupBy(x => index++ / 2).ToList()) {
                var unorderedList = "";
                foreach (var item in itemGroup) {
                    deleteStopCounter++;
                    if (deleteStopCounter > deleteStopItemCount) {
                        unorderedList += CardBodyListItemAdded(item);
                    } else {
                        unorderedList += CardBodyListItemDeleted(item);
                    }
                }
                playlistItems += $"<ul style=\"margin: 0; padding: 0;\">{unorderedList}</ul>";
            }
            return $"<div class=\"playlist-body\">{playlistItems}</div>";
        }

        private string CardBodyListItemDeleted(PlaylistItemsSnippetDb item) {
            var output = "";
            output += $"<strong><a href=\"https://www.youtube.com/watch?v={item.Id}\" style=\"color: #6d1223; text-decoration: none;\">{item.Title}</a></strong><br />";
            output += $"by <a href=\"https://www.youtube.com/channel/{item.ChannelId}\" style=\"color: #6d1223; text-decoration: none;\">{item.ChannelTitle}</a><br />";
            if (!string.IsNullOrEmpty(item.Description)) {
                output += $"<div><strong>Description</strong><br><div>{item.Description.Replace("\n", "</div><div>")}</div></div>";
            }
            return $"<li style=\"width: 97%;list-style: none;margin-left: 15px;margin-top: 15px;\"><div style=\"padding: 15px; background-color: #ffdddd; border: 1px solid #e09a9a;\">{output}</div></li>";
        }

        private string CardBodyListItemAdded(PlaylistItemsSnippetDb item) {
            var output = "";
            output += $"<strong><a href=\"https://www.youtube.com/watch?v={item.Id}\" style=\"color: green; text-decoration: none;\">{item.Title}</a></strong><br />";
            output += $"by <a href=\"https://www.youtube.com/channel/{item.ChannelId}\" style=\"color: green; text-decoration: none;\">{item.ChannelTitle}</a><br />";
            if (!string.IsNullOrEmpty(item.Description)) {
                output += $"<div><strong>Description</strong><br><div>{item.Description.Replace("\n", "</div><div>")}</div></div>";
            }
            return $"<li style=\"width: 97%;list-style: none;margin-left: 15px;margin-top: 15px;\"><div style=\"padding: 15px; background-color: #ddffe0; border: 1px solid green;\">{output}</div></li>";
        }

        private string CardFooter(List<PlaylistItemsSnippetDb> deletedItems, List<PlaylistItemsSnippetDb> addedItems) {
            return $"<div style=\"background-color: whitesmoke; padding: 15px; margin-top: 15px;\">-{deletedItems.Count} | +{addedItems.Count}</div>";
        }
    }
}
