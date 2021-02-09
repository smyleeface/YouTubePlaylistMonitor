using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Smylee.PlaylistMonitor.Library.Models;
using Xunit;

namespace Smylee.PlaylistMonitor.PlaylistCompare.Tests {

    public class ComparisonTest {
        readonly List<PlaylistItemsSnippetDb> _oldPlaylistItems = new List<PlaylistItemsSnippetDb> {
            new PlaylistItemsSnippetDb {
                ChannelId = "channel-id-1",
                Id = "playlist-id-1"
            },
            new PlaylistItemsSnippetDb {
                ChannelId = "channel-id-2",
                Id = "playlist-id-1"
            },
            new PlaylistItemsSnippetDb {
                ChannelId = "channel-id-3",
                Id = "playlist-id-2"
            }
        };
        readonly List<PlaylistItemsSnippetDb> _newPlaylistItems1 = new List<PlaylistItemsSnippetDb> {
            new PlaylistItemsSnippetDb {
                ChannelId = "channel-id-3",
                Id = "playlist-id-2"
            },
            new PlaylistItemsSnippetDb {
                ChannelId = "channel-id-1",
                Id = "playlist-id-1"
            },
            new PlaylistItemsSnippetDb {
                ChannelId = "channel-id-2",
                Id = "playlist-id-1"
            }
        };
        readonly List<PlaylistItemsSnippetDb> _newPlaylistItems2 = new List<PlaylistItemsSnippetDb> {
            new PlaylistItemsSnippetDb {
                ChannelId = "channel-id-3",
                Id = "playlist-id-2"
            },
            new PlaylistItemsSnippetDb {
                ChannelId = "channel-id-4",
                Id = "playlist-id-3"
            },
            new PlaylistItemsSnippetDb {
                ChannelId = "channel-id-5",
                Id = "playlist-id-3"
            }
        };

        [Fact]
        public async Task IsSameTest() {
            
            // Arrange
            // Act
            var comparisonResult1 = _oldPlaylistItems.IsSame(_newPlaylistItems1);
            var comparisonResult2 = _oldPlaylistItems.IsSame(_newPlaylistItems2);
            
            // Assert
            Assert.True(comparisonResult1);
            Assert.False(comparisonResult2);
            
        }

        [Fact]
        public async Task AddItemsTest() {
            
            // Arrange
            // Act
            var comparisonResult1 = Comparison.AddedItems(_oldPlaylistItems, _newPlaylistItems1);
            var comparisonResult2 = Comparison.AddedItems(null, _newPlaylistItems2);
            var comparisonResult3 = Comparison.AddedItems(_oldPlaylistItems, _newPlaylistItems2);

            // Assert
            Assert.Empty(comparisonResult1);
            Assert.Empty(comparisonResult2);
            Assert.True(comparisonResult3.Exists(x => x.ChannelId == "channel-id-4"));
            Assert.True(comparisonResult3.Exists(x => x.ChannelId == "channel-id-5"));
            Assert.True(comparisonResult3.Exists(x => x.Id == "playlist-id-3"));
            
        }

        [Fact]
        public async Task DeletedItemsTest() {

            // Arrange
            // Act
            var comparisonResult1 = Comparison.DeletedItems(_oldPlaylistItems, _newPlaylistItems1);
            var comparisonResult2 = Comparison.DeletedItems(null, _newPlaylistItems1);
            var comparisonResult3 = Comparison.DeletedItems(_oldPlaylistItems, _newPlaylistItems2);

            // Assert
            Assert.Empty(comparisonResult1);
            Assert.Empty(comparisonResult2);
            Assert.True(comparisonResult3.Exists(x => x.ChannelId == "channel-id-1"));
            Assert.True(comparisonResult3.Exists(x => x.ChannelId == "channel-id-2"));
            Assert.True(comparisonResult3.Exists(x => x.Id == "playlist-id-1"));
        }

        // [Fact]
        // public async Task PlaylistCompareLogicTest() {
        //     
        //     var email = new GenerateEmail(DateTime.Now.Date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture), "Playlist monitor report");
        //     var deletedList = new List<PlaylistItemsSnippetDb> {
        //         new PlaylistItemsSnippetDb {
        //             Title = "foo-playlist3-item5",
        //             Description = "foo-bar-description3"
        //         },
        //         new PlaylistItemsSnippetDb {
        //             Title = "foo-playlist3-item5",
        //             Description = "foo-bar-description3"
        //         },
        //         new PlaylistItemsSnippetDb {
        //             Title = "foo-playlist3-item5",
        //             Description = "foo-bar-description3"
        //         }
        //     };
        //     var addedList = new List<PlaylistItemsSnippetDb> {
        //         new PlaylistItemsSnippetDb {
        //             Title = "foo-playlist3-item5",
        //             Description = "foo-bar-description3"
        //         },
        //         new PlaylistItemsSnippetDb {
        //             Title = "foo-playlist3-item5",
        //             Description = "foo-bar-description3"
        //         },
        //         new PlaylistItemsSnippetDb {
        //             Title = "foo-playlist3-item5",
        //             Description = "foo-bar-description3"
        //         }
        //     };
        //     email.AddCard("Playlist Title", "author", deletedList, addedList);
        //     var testing = email.Html;
        //     // email.PlaylistReport("dplaylist Title", "playlist author", new List<PlaylistItemsSnippetDb>(), new List<PlaylistItemsSnippetDb>());
        //
        //     XmlDocument emailTemplate = new XmlDocument();
            // emailTemplate.Load("emailTemplate.html");
            // emailTemplate.DocumentElement.SelectSingleNode("//div[@class=\"container\"]/h1").InnerText = "Report Title";
            //
            // var emailBody = emailTemplate.DocumentElement.SelectSingleNode("//div[@class=\"container\"]/div[@class=\"row\"]/div[@class=\"col\"]/div[@class=\"card text-left\"]");
            // var cardHeader = emailBody.SelectSingleNode("//div[@class=\"card-header\"]");
            // cardHeader.SelectSingleNode("//h2").InnerText = "Playlist Name";
            // cardHeader.SelectSingleNode("//h6[@class=\"card-subtitle mb-2 text-muted\"]").InnerText = "by Author Name";
            //
            // var cardBody = emailBody.SelectSingleNode("//div[@class=\"card-body\"]/div[@class=\"card-text\"]/div[@class=\"playlist-changes\"]");
            // cardBody.InnerText = "ul/li list";

            //emailTemplate.DocumentElement.InnerXml

            //var playlistHeader = new XmlDocument();
            // var playlistHeaderRoot = playlistHeader.DocumentElement;
            //
            // elem.InnerText="19.95";
            // playlistHeader.DocumentElement.InsertAfter(elem, playlistHeaderRoot.FirstChild);
            //var playlistHeader = "";





            //XmlNode root = emailTemplate.DocumentElement;
            // root.ChildNodes[2].ChildNodes[0].InnerText = "foobar";
            // var emailContent = root.InnerXml;
            //
            // var cardHeader = doc.CreateElement("div");
            //cardHeader.Attributes("class", "card-header");
            //<div class=\"card-header\"><h2>{playlistTitle}</h2><h6 class=\"card-subtitle mb-2 text-muted\">by {playlistAuthor}</h6></div>";



        // }
        // [Fact]
        // public async Task LogicFunctionalTest() {
        //     var config = new LambdaConfig(new LambdaDictionarySource(new List<KeyValuePair<string, string>> {
        //         new KeyValuePair<string, string>("/CachePlaylists", "arn:aws:dynamodb:us-east-1:892750500233:table/Sandbox-Smylee-PlaylistMonitor-CachePlaylists-EZQ47Q5W0F7U"),
        //         new KeyValuePair<string, string>("/CacheVideos", "arn:aws:dynamodb:us-east-1:892750500233:table/Sandbox-Smylee-PlaylistMonitor-CacheVideos-1S8TRT4N4GXNY"),
        //         new KeyValuePair<string, string>("/UserSubscriptions", "arn:aws:dynamodb:us-east-1:892750500233:table/Sandbox-Smylee-PlaylistMonitor-UserSubscriptions-1IZ8B6HR1NHF9"),
        //         new KeyValuePair<string, string>("/FromEmail", "patty.ramert@gmail.com"),
        //         new KeyValuePair<string, string>("/YouTubeApiKey", "REDACTED")
        //     }));
        //     var snsEvent = new SNSEvent {Records = new List<SNSEvent.SNSRecord> {new SNSEvent.SNSRecord {Sns = new SNSEvent.SNSMessage {Message = "{\"Key\": \"patty.ramert@gmail.com\",\"Value\": [{\"channelId\": \"UCDFjGPe4Yu6sVL_KmrcDtpg\",\"playlistName\": \"electro 2016\",\"finalEmail\": null,\"timestamp\": null},{\"channelId\": \"UCDFjGPe4Yu6sVL_KmrcDtpg\",\"playlistName\": \"Vocal HTC\",\"finalEmail\": null,\"timestamp\": null},{\"channelId\": \"UCDFjGPe4Yu6sVL_KmrcDtpg\",\"playlistName\": \"Fyre!\",\"finalEmail\": null,\"timestamp\": null},{\"channelId\": \"UCDFjGPe4Yu6sVL_KmrcDtpg\",\"playlistName\": \"Trance/House\",\"finalEmail\": null,\"timestamp\": null},{\"channelId\": \"UCDFjGPe4Yu6sVL_KmrcDtpg\",\"playlistName\": \"Comedy\",\"finalEmail\": null,\"timestamp\": null},{\"channelId\": \"UCDFjGPe4Yu6sVL_KmrcDtpg\",\"playlistName\": \"amazing\",\"finalEmail\": null,\"timestamp\": null},{\"channelId\": \"UCDFjGPe4Yu6sVL_KmrcDtpg\",\"playlistName\": \"music\",\"finalEmail\": null,\"timestamp\": null}]}",}}}};
        //     var function = new Function();
        //     await function.InitializeAsync(config);
        //     await function.ProcessMessageAsync(snsEvent);
        // }
        // var provider = new Mock<IDependencyProvider>(MockBehavior.Strict);
            //     provider.SetupSequence(x => x.YouTubeApiChannelSnippetAsync(It.Is<string>(y => y.StartsWith("foo-username"))))
            //         .Returns(Task.FromResult(youtubeChannelListResponse1))
            //         .Returns(Task.FromResult(youtubeChannelListResponse2))
            //         .Returns(Task.FromResult(youtubeChannelListResponse3))
            //         .Returns(Task.FromResult(youtubeChannelListResponse4))
            //         .Returns(Task.FromResult(youtubeChannelListResponse5));
            //     provider.SetupSequence(x => x.YouTubeApiPlaylistsListAllAsync(It.Is<string>(y => y.StartsWith("foo-channel-id"))))
            //         .Returns(Task.FromResult(youtubePlaylistResponse1))
            //         .Returns(Task.FromResult(youtubePlaylistResponse2))
            //         .Returns(Task.FromResult(youtubePlaylistResponse3))
            //         .Returns(Task.FromResult(youtubePlaylistResponse4))
            //         .Returns(Task.FromResult(youtubePlaylistResponse5));
            //     provider.SetupSequence(x => x.YouTubeApiPlaylistItemsAllAsync(It.Is<string>(y => y.StartsWith("foo-playlist-id"))))
            //         .Returns(Task.FromResult(youtubePlaylistItemResponse1))
            //         .Returns(Task.FromResult(youtubePlaylistItemResponse2))
            //         .Returns(Task.FromResult(youtubePlaylistItemResponse3))
            //         .Returns(Task.FromResult(youtubePlaylistItemResponse4))
            //         .Returns(Task.FromResult(youtubePlaylistItemResponse5));
            //     provider.SetupSequence(x => x.YouTubeApiVideosAsync(It.Is<string>(y => y.StartsWith("foo-playlist-item") && y.EndsWith("-video-id"))))
            //         .Returns(Task.FromResult(youtubeVideoListResponse1))
            //         .Returns(Task.FromResult(youtubeVideoListResponse2))
            //         .Returns(Task.FromResult(youtubeVideoListResponse3))
            //         .Returns(Task.FromResult(youtubeVideoListResponse4))
            //         .Returns(Task.FromResult(youtubeVideoListResponse5));
            //     provider.Setup(x => x.DynamoDbGetPlaylistListAsync("foo-bar@email.com", "foo-playlist-id")).Returns(Task.FromResult(dynamoGetItemResponse));
            //     provider.Setup(x => x.DynamoDbPutPlaylistListAsync("foo-b
        // }
        
        // [Fact]
        // public async Task LogicTest() {
        //     
        //     // Arrange
        //     var playlistMonitorSubscription = new PlaylistMonitorSubscription {
        //         PlaylistName = "foo-playlist",
        //         ChannelId = "foo-username"
        //     };
        //     var playlistMonitorSubscription2 = new PlaylistMonitorSubscription {
        //         PlaylistName = "foo-playlist2",
        //         ChannelId = "foo-username2"
        //     };
        //     var emailSub1 = new List<PlaylistMonitorSubscription> {
        //         playlistMonitorSubscription,
        //         playlistMonitorSubscription2
        //     };
        //     var youtubeChannelListResponse1 = new ChannelListResponse {
        //         Items = new List<Channel> {
        //             new Channel {
        //                 Id = "foo-channel-id1"
        //             }
        //         }
        //     };
        //     var youtubeChannelListResponse2 = new ChannelListResponse {
        //         Items = new List<Channel> {
        //             new Channel {
        //                 Id = "foo-channel-id2"
        //             }
        //         }
        //     };
        //     var youtubeChannelListResponse3 = new ChannelListResponse {
        //         Items = new List<Channel> {
        //             new Channel {
        //                 Id = "foo-channel-id3"
        //             }
        //         }
        //     };
        //     var youtubeChannelListResponse4 = new ChannelListResponse {
        //         Items = new List<Channel> {
        //             new Channel {
        //                 Id = "foo-channel-id4"
        //             }
        //         }
        //     };
        //     var youtubeChannelListResponse5 = new ChannelListResponse {
        //         Items = new List<Channel> {
        //             new Channel {
        //                 Id = "foo-channel-id5"
        //             }
        //         }
        //     };
        //     var youtubePlaylistResponse1 = new List<PlaylistListResponse> {
        //         new PlaylistListResponse {
        //             Items = new List<Playlist> {
        //                 new Playlist {
        //                     Id = "foo-playlist-id1",
        //                     Snippet = new PlaylistSnippet {
        //                         Title = "foo-playlist1"
        //                     }
        //                 }
        //             }
        //         }
        //     };
        //     var youtubePlaylistResponse2 = new List<PlaylistListResponse> {
        //         new PlaylistListResponse {
        //             Items = new List<Playlist> {
        //                 new Playlist {
        //                     Id = "foo-playlist-id2",
        //                     Snippet = new PlaylistSnippet {
        //                         Title = "foo-playlist2"
        //                     }
        //                 }
        //             }
        //         }
        //     };
        //     var youtubePlaylistResponse3 = new List<PlaylistListResponse> {
        //         new PlaylistListResponse {
        //             Items = new List<Playlist> {
        //                 new Playlist {
        //                     Id = "foo-playlist-id3",
        //                     Snippet = new PlaylistSnippet {
        //                         Title = "foo-playlist3"
        //                     }
        //                 }
        //             }
        //         }
        //     };
        //     var youtubePlaylistResponse4 = new List<PlaylistListResponse> {
        //         new PlaylistListResponse {
        //             Items = new List<Playlist> {
        //                 new Playlist {
        //                     Id = "foo-playlist-id4",
        //                     Snippet = new PlaylistSnippet {
        //                         Title = "foo-playlist4"
        //                     }
        //                 }
        //             }
        //         }
        //     };
        //     var youtubePlaylistResponse5 = new List<PlaylistListResponse> {
        //         new PlaylistListResponse {
        //             Items = new List<Playlist> {
        //                 new Playlist {
        //                     Id = "foo-playlist-id5",
        //                     Snippet = new PlaylistSnippet {
        //                         Title = "foo-playlist5"
        //                     }
        //                 }
        //             }
        //         }
        //     };
        //     var youtubePlaylistItemResponse1 = new List<PlaylistItemListResponse> {
        //         new PlaylistItemListResponse {
        //             Items = new List<PlaylistItem> {
        //                 new PlaylistItem {
        //                     Id = "foo-playlist1-item1-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist1-item1",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist1-item1-video-id"
        //                         },
        //                         Position = 1,
        //                         Description = "foo-bar-description1"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist1-item2-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist1-item2",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist1-item2-video-id"
        //                         },
        //                         Position = 2,
        //                         Description = "foo-bar-description1"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist1-item3-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist1-item3",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist1-item3-video-id"
        //                         },
        //                         Position = 3,
        //                         Description = "foo-bar-description1"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist1-item4-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist1-item4",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist1-item4-video-id"
        //                         },
        //                         Position = 4,
        //                         Description = "foo-bar-description1"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist1-item5-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist1-item5",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist1-item5-video-id"
        //                         },
        //                         Position = 1,
        //                         Description = "foo-bar-description1"
        //                     }
        //                 },
        //             }
        //         }
        //     };
        //     var youtubePlaylistItemResponse2 = new List<PlaylistItemListResponse> {
        //         new PlaylistItemListResponse {
        //             Items = new List<PlaylistItem> {
        //                 new PlaylistItem {
        //                     Id = "foo-playlist2-item1-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist2-item1",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist2-item1-video-id"
        //                         },
        //                         Position = 1,
        //                         Description = "foo-bar-description2"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist2-item2-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist2-item2",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist2-item2-video-id"
        //                         },
        //                         Position = 2,
        //                         Description = "foo-bar-description2"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist2-item3-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist2-item3",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist2-item3-video-id"
        //                         },
        //                         Position = 3,
        //                         Description = "foo-bar-description2"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist2-item4-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist2-item4",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist2-item4-video-id"
        //                         },
        //                         Position = 4,
        //                         Description = "foo-bar-description2"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist2-item5-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist2-item5",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist2-item5-video-id"
        //                         },
        //                         Position = 1,
        //                         Description = "foo-bar-description2"
        //                     }
        //                 },
        //             }
        //         }
        //     };
        //     var youtubePlaylistItemResponse3 = new List<PlaylistItemListResponse> {
        //         new PlaylistItemListResponse {
        //             Items = new List<PlaylistItem> {
        //                 new PlaylistItem {
        //                     Id = "foo-playlist3-item1-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist3-item1",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist3-item1-video-id"
        //                         },
        //                         Position = 1,
        //                         Description = "foo-bar-description3"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist3-item2-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist3-item2",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist3-item2-video-id"
        //                         },
        //                         Position = 2,
        //                         Description = "foo-bar-description3"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist3-item3-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist3-item3",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist3-item3-video-id"
        //                         },
        //                         Position = 3,
        //                         Description = "foo-bar-description3"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist3-item4-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist3-item4",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist3-item4-video-id"
        //                         },
        //                         Position = 4,
        //                         Description = "foo-bar-description3"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist3-item5-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist3-item5",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist3-item5-video-id"
        //                         },
        //                         Position = 1,
        //                         Description = "foo-bar-description3"
        //                     }
        //                 },
        //             }
        //         }
        //     };
        //     var youtubePlaylistItemResponse4 = new List<PlaylistItemListResponse> {
        //         new PlaylistItemListResponse {
        //             Items = new List<PlaylistItem> {
        //                 new PlaylistItem {
        //                     Id = "foo-playlist4-item1-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist4-item1",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist4-item1-video-id"
        //                         },
        //                         Position = 1,
        //                         Description = "foo-bar-description4"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist4-item2-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist4-item2",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist4-item2-video-id"
        //                         },
        //                         Position = 2,
        //                         Description = "foo-bar-description4"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist4-item3-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist4-item3",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist4-item3-video-id"
        //                         },
        //                         Position = 3,
        //                         Description = "foo-bar-description4"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist4-item4-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist4-item4",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist4-item4-video-id"
        //                         },
        //                         Position = 4,
        //                         Description = "foo-bar-description4"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist4-item5-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist4-item5",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist4-item5-video-id"
        //                         },
        //                         Position = 1,
        //                         Description = "foo-bar-description4"
        //                     }
        //                 },
        //             }
        //         }
        //     };
        //     var youtubePlaylistItemResponse5 = new List<PlaylistItemListResponse> {
        //         new PlaylistItemListResponse {
        //             Items = new List<PlaylistItem> {
        //                 new PlaylistItem {
        //                     Id = "foo-playlist5-item1-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist5-item1",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist5-item1-video-id"
        //                         },
        //                         Position = 1,
        //                         Description = "foo-bar-description5"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist5-item2-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist5-item2",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist5-item2-video-id"
        //                         },
        //                         Position = 2,
        //                         Description = "foo-bar-description5"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist5-item3-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist5-item3",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist5-item3-video-id"
        //                         },
        //                         Position = 3,
        //                         Description = "foo-bar-description5"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist5-item4-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist5-item4",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist5-item4-video-id"
        //                         },
        //                         Position = 4,
        //                         Description = "foo-bar-description5"
        //                     }
        //                 },
        //                 new PlaylistItem {
        //                     Id = "foo-playlist5-item5-id",
        //                     Snippet = new PlaylistItemSnippet {
        //                         Title = "foo-playlist5-item5",
        //                         ResourceId = new ResourceId {
        //                             VideoId = "foo-playlist5-item5-video-id"
        //                         },
        //                         Position = 1,
        //                         Description = "foo-bar-description5"
        //                     }
        //                 },
        //             }
        //         }
        //     };
        //     var youtubeVideoListResponse1 = new VideoListResponse {
        //         Items = new List<Video> {
        //             new Video {
        //                 Id = "foo-playlist-item1-video-id",
        //                 Snippet = new VideoSnippet {
        //                     ChannelTitle = "foo-playlist-item1-channel-title"
        //                 }
        //             }
        //         }
        //     };
        //     var youtubeVideoListResponse2 = new VideoListResponse {
        //         Items = new List<Video> {
        //             new Video {
        //                 Id = "foo-playlist-item2-video-id",
        //                 Snippet = new VideoSnippet {
        //                     ChannelTitle = "foo-playlist-item2-channel-title"
        //                 }
        //             }
        //         }
        //     };
        //     var youtubeVideoListResponse3 = new VideoListResponse {
        //         Items = new List<Video> {
        //             new Video {
        //                 Id = "foo-playlist-item3-video-id",
        //                 Snippet = new VideoSnippet {
        //                     ChannelTitle = "foo-playlist-item3-channel-title"
        //                 }
        //             }
        //         }
        //     };
        //     var youtubeVideoListResponse4 = new VideoListResponse {
        //         Items = new List<Video> {
        //             new Video {
        //                 Id = "foo-playlist-item4-video-id",
        //                 Snippet = new VideoSnippet {
        //                     ChannelTitle = "foo-playlist-item4-channel-title"
        //                 }
        //             }
        //         }
        //     };
        //     var youtubeVideoListResponse5 = new VideoListResponse {
        //         Items = new List<Video> {
        //             new Video {
        //                 Id = "foo-playlist-item5-video-id",
        //                 Snippet = new VideoSnippet {
        //                     ChannelTitle = "foo-playlist-item5-channel-title"
        //                 }
        //             }
        //         }
        //     };
        //     var dynamoGetItemResponse = new GetItemResponse {
        //         Item = new Dictionary<string, AttributeValue> {
        //             {"playlists", new AttributeValue {
        //                 L = new List<AttributeValue> {
        //                     new AttributeValue {
        //                         S = "{\"id\": \"foo-playlist-item2-id\",\"description\": \"foo-bar-description\",\"title\": \"foo-bar-title\",\"link\": \"http://foo-bar-link-2\",\"author\": \"foo-playlist-item2-channel-title\",\"position\": 2}"
        //                     },
        //                     new AttributeValue {
        //                         S = "{\"id\": \"foo-playlist-item3-id\",\"description\": \"foo-bar-description\",\"title\": \"foo-bar-title\",\"link\": \"http://foo-bar-link-3\",\"author\": \"foo-playlist-item3-channel-title\",\"position\": 3}"
        //                     },
        //                     new AttributeValue {
        //                         S = "{\"id\": \"foo-playlist-item4-id\",\"description\": \"foo-bar-description\",\"title\": \"foo-bar-title\",\"link\": \"http://foo-bar-link-4\",\"author\": \"foo-playlist-item4-channel-title\",\"position\": 4}"
        //                     },
        //                     new AttributeValue {
        //                         S = "{\"id\": \"foo-playlist-item5-id\",\"description\": \"foo-bar-description\",\"title\": \"foo-bar-title\",\"link\": \"http://foo-bar-link-5\",\"author\": \"foo-playlist-item5-channel-title\",\"position\": 5}"
        //                     },
        //                     new AttributeValue {
        //                         S = "{\"id\": \"foo-playlist-item6-id\",\"description\": \"foo-bar-description\",\"title\": \"foo-bar-title\",\"link\": \"http://foo-bar-link-6\",\"author\": \"foo-playlist-item6-channel-title\",\"position\": 6}"
        //                     }
        //                 }
        //             }}
        //         }
        //     };
        //     var dateNow = new DateTime(2020, 1, 4);
        //     var provider = new Mock<IDependencyProvider>(MockBehavior.Strict);
        //     provider.SetupSequence(x => x.YouTubeApiChannelSnippetAsync(It.Is<string>(y => y.StartsWith("foo-username"))))
        //         .Returns(Task.FromResult(youtubeChannelListResponse1))
        //         .Returns(Task.FromResult(youtubeChannelListResponse2))
        //         .Returns(Task.FromResult(youtubeChannelListResponse3))
        //         .Returns(Task.FromResult(youtubeChannelListResponse4))
        //         .Returns(Task.FromResult(youtubeChannelListResponse5));
        //     provider.SetupSequence(x => x.YouTubeApiPlaylistsListAllAsync(It.Is<string>(y => y.StartsWith("foo-channel-id"))))
        //         .Returns(Task.FromResult(youtubePlaylistResponse1))
        //         .Returns(Task.FromResult(youtubePlaylistResponse2))
        //         .Returns(Task.FromResult(youtubePlaylistResponse3))
        //         .Returns(Task.FromResult(youtubePlaylistResponse4))
        //         .Returns(Task.FromResult(youtubePlaylistResponse5));
        //     provider.SetupSequence(x => x.YouTubeApiPlaylistItemsAllAsync(It.Is<string>(y => y.StartsWith("foo-playlist-id"))))
        //         .Returns(Task.FromResult(youtubePlaylistItemResponse1))
        //         .Returns(Task.FromResult(youtubePlaylistItemResponse2))
        //         .Returns(Task.FromResult(youtubePlaylistItemResponse3))
        //         .Returns(Task.FromResult(youtubePlaylistItemResponse4))
        //         .Returns(Task.FromResult(youtubePlaylistItemResponse5));
        //     provider.SetupSequence(x => x.YouTubeApiVideosAsync(It.Is<string>(y => y.StartsWith("foo-playlist-item") && y.EndsWith("-video-id"))))
        //         .Returns(Task.FromResult(youtubeVideoListResponse1))
        //         .Returns(Task.FromResult(youtubeVideoListResponse2))
        //         .Returns(Task.FromResult(youtubeVideoListResponse3))
        //         .Returns(Task.FromResult(youtubeVideoListResponse4))
        //         .Returns(Task.FromResult(youtubeVideoListResponse5));
        //     provider.Setup(x => x.DynamoDbGetPlaylistListAsync("foo-bar@email.com", "foo-playlist-id")).Returns(Task.FromResult(dynamoGetItemResponse));
        //     provider.Setup(x => x.DynamoDbPutPlaylistListAsync("foo-bar@email.com", "foo-channel-id", "foo-playlist-id", "foo-playlist", It.IsAny<List<PlaylistMonitorPlaylistItem>>(), "1578096000")).Returns(Task.FromResult(dynamoGetItemResponse));
        //     provider.Setup(x => x.SesSendEmail("foo-bar-from@gmail.com", "foo-bar@email.com", It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(dynamoGetItemResponse));
        //     var logic = new Logic("foo-bar-from@gmail.com", provider.Object, new Logger());
        //     
        //     // Act
        //     // Assert
        //     await logic.Run(dateNow, "foo-bar@email.com", emailSub1);
        // }
    }
}