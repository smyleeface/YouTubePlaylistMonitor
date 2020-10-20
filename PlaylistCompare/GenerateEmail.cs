using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Smylee.PlaylistMonitor.Library.Models;

namespace Smylee.PlaylistMonitor.PlaylistCompare {

    public class GenerateEmail {
        
        private XmlDocument _emailXml;

        public GenerateEmail(string dateNowString, string subjectTitle) {
            _emailXml = new XmlDocument();
            _emailXml.Load("emailTemplate.html");
            _emailXml.DocumentElement.SelectSingleNode("//div[@class=\"container\"]/h1").InnerText = string.Format(subjectTitle, dateNowString);
        }

        public string Html => _emailXml.DocumentElement.InnerXml;

        public void AddCard(string playlistTitle, string playlistAuthor, List<PlaylistItemsSnippetDb> deletedItems, List<PlaylistItemsSnippetDb> addedItems) {
            
            // generate card parts
            var headerContent = CardHeader(playlistTitle, playlistAuthor);
            var bodyContent = CardBody(deletedItems, addedItems);
            var footerContent = CardFooter(deletedItems, addedItems);
            
            // create an xml row for the playlist
            var playlistRow = _emailXml.CreateElement("div");
            playlistRow.SetAttribute("class", "row");
            playlistRow.InnerXml = $"<div class=\"col\"><div class=\"card text-left\"><div class=\"card-header\">{headerContent}</div><div class=\"card-body\"><div class=\"card-text\"><div class=\"playlist-changes\">{bodyContent}</div></div></div><div class=\"card-footer text-muted text-monospace\">{footerContent}</div></div></div>";
            
            // Add to the emailTemplate
            var emailBody = _emailXml.DocumentElement.SelectSingleNode("//div[@class=\"container\"]");
            emailBody.AppendChild(playlistRow);
        }

        private string CardHeader(string playlistTitle, string playlistAuthor) {
            var output = $"<h2>{playlistTitle}</h2>";
            output += $"<h6 class=\"card-subtitle mb-2 text-muted\">by {playlistAuthor}</h6>";
            return output;
        }
        
        private string CardBody(List<PlaylistItemsSnippetDb> deletedItems, List<PlaylistItemsSnippetDb> addedItems) {
            var playlistItems = "";
            var index = 0;
            var deleteStopItemCount = deletedItems.Count;
            var deleteStopCounter = 0;
            var combinedItems = deletedItems.Concat(addedItems).ToList();
            foreach (var itemGroup in combinedItems.GroupBy(x => index++ / 2).ToList()) {
                var unorderedList = _emailXml.CreateElement("ul");
                unorderedList.SetAttribute("class", "list-group");
                foreach (var item in itemGroup) {
                    deleteStopCounter++;
                    XmlElement liList;
                    liList = CardBodyListItem(item, deleteStopCounter > deleteStopItemCount ? "success" : "danger");
                    unorderedList.InnerXml += liList.OuterXml;
                }
                playlistItems += unorderedList.OuterXml;
            }
            return playlistItems;
        }

        private XmlElement CardBodyListItem(PlaylistItemsSnippetDb item, string style) {
            var liList = _emailXml.CreateElement("li");
            liList.SetAttribute("class", "list-group-item col-md-6 playlist-item");
            var output = "";
            output += $"<strong><a href=\"https://www.youtube.com/watch?v={item.Id}\" class=\"alert-link\">{item.Title}</a></strong><br />";
            output += $"by <a href=\"https://www.youtube.com/channel/{item.ChannelId}\" class=\"alert-link\">{item.ChannelTitle}</a><br />"; 
            if (!string.IsNullOrEmpty(item.Description)) {
                output += $"<details><summary>Description</summary>{item.Description}</details>";
            }
            liList.InnerXml = $"<div class=\"alert alert-{style}\" role=\"alert\">{output}</div>";
            return liList;
        }

        private string CardFooter(List<PlaylistItemsSnippetDb> deletedItems, List<PlaylistItemsSnippetDb> addedItems) {
            return $"-{deletedItems.Count} | +{addedItems.Count}";
        }

    }
    
}

