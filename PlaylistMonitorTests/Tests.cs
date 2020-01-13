using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using LambdaSharp;
using LambdaSharp.Logger;
using Moq;
using Newtonsoft.Json;
using Smylee.PlaylistMonitor.PlaylistMonitor;
using Xunit;
using PlaylistItem = Smylee.PlaylistMonitor.PlaylistMonitor.PlaylistItem;
using Subscription = Smylee.PlaylistMonitor.PlaylistMonitor.Subscription;

namespace Smylee.PlaylistMonitor.PlaylistMonitorTests {

    public class Logger : ILambdaLogLevelLogger {
        public void Log(LambdaLogLevel level, Exception exception, string format, params object[] arguments) {}
    }
    
    public class Tests {
        private ILambdaLogLevelLogger _lambdaLogger;

        [Fact]
        public void Test1() {
            var getRequest = new GetItemRequest {
                TableName = "foo-bar-table",
                Key = new Dictionary<string, AttributeValue> {
                    { "email", new AttributeValue { S = "foo-bar-request-email@foobar.com" } },
                    { "playlistId", new AttributeValue { S = "playlistid123"  } }
                }
            };
            var getResponse = new GetItemResponse {
                Item = new Dictionary<string, AttributeValue>()
            };
            Mock<IAmazonDynamoDB> dynamodbProvider = new Mock<IAmazonDynamoDB>(MockBehavior.Strict);
            dynamodbProvider.Setup(x => x.GetItemAsync(It.Is<GetItemRequest>(y => 
                y.TableName == "foo-bar-table" &&
                y.Key.FirstOrDefault().Value.S == "foo-bar-request-email@foobar.com"
            ), CancellationToken.None)).Returns(Task.FromResult(getResponse));

            
            
        }  
        
        [Fact]
        public void GenerateReportDeletedTest() {
            
            // Arrange
            var requestedPlaylistName = "foo-bar-playlist-name";
            var deletedItems = new List<PlaylistItem> {
                new PlaylistItem {
                    Id = "foo-bar-id-1",
                    Description = "foo-bar-description-1",
                    Link = "http://foo-bar-link-1",
                    Position = 1,
                    Title = "foo-bar-title-1",
                    ChannelTitle = "foo-bar-channel-title-1"
                },
                new PlaylistItem {
                    Id = "foo-bar-id-3",
                    Description = "foo-bar-description-3",
                    Link = "http://foo-bar-link-3",
                    Position = 3,
                    Title = "foo-bar-title-3",
                    ChannelTitle = "foo-bar-channel-title-3"
                }
            };
            var dynamoDbTableName = "foo-bar-table-name";
            var expectedEmailSubject = $"YouTube Playlist {requestedPlaylistName} has missing video";
            var expectedEmailBody = "<h3>foo-bar-playlist-name</h3>";
            var provider = new Mock<IDepenedencyProvder>(MockBehavior.Strict);
            var logic = new Logic(provider.Object, new Logger());
            
            // Act
            var result = logic.GenerateReportDeleted(requestedPlaylistName, deletedItems);

            // Assert
            Assert.Equal(result.EmailSubject, expectedEmailSubject);
            Assert.StartsWith(expectedEmailBody, result.EmailBody);
        }  
        
        [Fact]
        public void ComparePlaylistsForAddTest() {
            
            // Arrange
            var requestedPlaylistName = "foo-bar-playlist-name";
            var existingItems = new List<PlaylistItem> {
                new PlaylistItem {
                    Id = "foo-bar-id-1",
                    Description = "foo-bar-description-1",
                    Link = "http://foo-bar-link-1",
                    Position = 1,
                    Title = "foo-bar-title-1",
                    ChannelTitle = "foo-bar-channel-title-1"
                },
                new PlaylistItem {
                    Id = "foo-bar-id-3",
                    Description = "foo-bar-description-3",
                    Link = "http://foo-bar-link-3",
                    Position = 3,
                    Title = "foo-bar-title-3",
                    ChannelTitle = "foo-bar-channel-title-3"
                }
            };
            var currentItems = new List<PlaylistItem> {
                new PlaylistItem {
                    Id = "foo-bar-id-1",
                    Description = "foo-bar-description-1",
                    Link = "http://foo-bar-link-1",
                    Position = 1,
                    Title = "foo-bar-title-1",
                    ChannelTitle = "foo-bar-channel-title-1"
                },
                new PlaylistItem {
                    Id = "foo-bar-id-4",
                    Description = "foo-bar-description-4",
                    Link = "http://foo-bar-link-4",
                    Position = 4,
                    Title = "foo-bar-title-4",
                    ChannelTitle = "foo-bar-channel-title-4"
                }
            };
            var dynamoDbTableName = "foo-bar-table-name";
            var expectedEmailSubject = $"YouTube Playlist {requestedPlaylistName} has missing video";
            var expectedEmailBody = "<h3>foo-bar-playlist-name</h3>";
            var provider = new Mock<IDepenedencyProvder>(MockBehavior.Strict);
            var logic = new Logic(provider.Object, new Logger());
            
            // Act
            var result = logic.ComparePlaylistsForAdd(existingItems, currentItems);

            // Assert
            Assert.Equal(currentItems[1].Id, result.FirstOrDefault().Id);
        }
        
        [Fact]
        public void ComparePlaylistsForDeleteTest() {
            
            // Arrange
            var requestedPlaylistName = "foo-bar-playlist-name";
            var existingItems = new List<PlaylistItem> {
                new PlaylistItem {
                    Id = "foo-bar-id-1",
                    Description = "foo-bar-description-1",
                    Link = "http://foo-bar-link-1",
                    Position = 1,
                    Title = "foo-bar-title-1",
                    ChannelTitle = "foo-bar-channel-title-1"
                },
                new PlaylistItem {
                    Id = "foo-bar-id-3",
                    Description = "foo-bar-description-3",
                    Link = "http://foo-bar-link-3",
                    Position = 3,
                    Title = "foo-bar-title-3",
                    ChannelTitle = "foo-bar-channel-title-3"
                }
            };
            var currentItems = new List<PlaylistItem> {
                new PlaylistItem {
                    Id = "foo-bar-id-1",
                    Description = "foo-bar-description-1",
                    Link = "http://foo-bar-link-1",
                    Position = 1,
                    Title = "foo-bar-title-1",
                    ChannelTitle = "foo-bar-channel-title-1"
                },
                new PlaylistItem {
                    Id = "foo-bar-id-4",
                    Description = "foo-bar-description-4",
                    Link = "http://foo-bar-link-4",
                    Position = 4,
                    Title = "foo-bar-title-4",
                    ChannelTitle = "foo-bar-channel-title-4"
                }
            };
            var provider = new Mock<IDepenedencyProvder>(MockBehavior.Strict);
            var logic = new Logic(provider.Object, new Logger());
            
            // Act
            var result = logic.ComparePlaylistsForDelete(existingItems, currentItems);

            // Assert
            Assert.Equal(existingItems[1].Id, result.FirstOrDefault().Id);
        }
        
        [Fact]
        public async Task CreatePlaylistItemsFromPlaylistIdTest() {
            
            // Arrange
            var currentItems = new List<PlaylistItem> {
                new PlaylistItem {
                    Id = "foo-bar-id-1",
                    Description = "foo-bar-description-1",
                    Link = "https://www.youtube.com/watch?v=foo-bar-link-1",
                    Position = 1,
                    Title = "foo-bar-title-1",
                    ChannelTitle = "foo-bar-channel-title-1"
                },
                new PlaylistItem {
                    Id = "foo-bar-id-4",
                    Description = "foo-bar-description-4",
                    Link = "https://www.youtube.com/watch?v=foo-bar-link-4",
                    Position = 4,
                    Title = "foo-bar-title-4",
                    ChannelTitle = "foo-bar-channel-title-4"
                }
            };
            var videoListResponse = new VideoListResponse {
                Items = new List<Video> {
                    new Video {
                        Id = currentItems[0].Id
                    },
                    new Video {
                        Id = currentItems[1].Id
                    }
                }
            };
            var playlistItemListResponse = new PlaylistItemListResponse {
                Items = new List<Google.Apis.YouTube.v3.Data.PlaylistItem> {
                    new Google.Apis.YouTube.v3.Data.PlaylistItem {
                        Id = currentItems[0].Id,
                        Snippet = new PlaylistItemSnippet {
                            Title = currentItems[0].Title,
                            ChannelTitle = currentItems[0].ChannelTitle,
                            Description = currentItems[0].Description,
                            Position = currentItems[0].Position,
                            ResourceId = new ResourceId {
                                VideoId = "foo-bar-link-1"
                            }
                        }
                    },
                    new Google.Apis.YouTube.v3.Data.PlaylistItem {
                        Id = currentItems[1].Id,
                        Snippet = new PlaylistItemSnippet {
                            Title = currentItems[1].Title,
                            ChannelTitle = currentItems[1].ChannelTitle,
                            Description = currentItems[1].Description,
                            Position = currentItems[1].Position,
                            ResourceId = new ResourceId {
                                VideoId = "foo-bar-link-4"
                            }
                        }
                    }
                }
            };
            var provider = new Mock<IDepenedencyProvder>(MockBehavior.Strict);
            provider.Setup(x => x.YouTubeApiVideos(currentItems[0].Id)).Returns(Task.FromResult(videoListResponse));
            provider.Setup(x => x.YouTubeApiVideos(currentItems[1].Id)).Returns(Task.FromResult(videoListResponse));
            provider.Setup(x => x.YouTubeApiPlaylistItemsList(currentItems[0].Id, null)).Returns(Task.FromResult(playlistItemListResponse));
            var logic = new Logic(provider.Object, new Logger());
            
            // Act
            var result = await logic.CreatePlaylistItemsFromPlaylistIdAsync(currentItems[0].Id);

            // Assert
            Assert.Equal(currentItems[0].Id, result[0].Id);
            Assert.Equal(currentItems[1].Id, result[1].Id);
        }
        
        [Fact]
        public async Task GetChannelTitleFoundTest() {
            
            // Arrange
            var playlistItemListResponse = new PlaylistItemListResponse {
                Items = new List<Google.Apis.YouTube.v3.Data.PlaylistItem> {
                    new Google.Apis.YouTube.v3.Data.PlaylistItem {
                        Id = "foo-bar-id-1",
                        Snippet = new PlaylistItemSnippet {
                            Title = "foo-bar-title-1",
                            ChannelTitle = "foo-bar-channel-title-1",
                            Description = "foo-bar-description-1",
                            Position = 1,
                            ResourceId = new ResourceId {
                                VideoId = "foo-bar-video-id-1"
                            }
                        }
                    },
                    new Google.Apis.YouTube.v3.Data.PlaylistItem {
                        Id = "foo-bar-id-4",
                        Snippet = new PlaylistItemSnippet {
                            Title = "foo-bar-title-4",
                            ChannelTitle = "foo-bar-channel-title-4",
                            Description = "foo-bar-description-4",
                            Position = 1,
                            ResourceId = new ResourceId {
                                VideoId = "foo-bar-video-id-4"
                            }
                        }
                    }
                }
            };
            var videoListResponse = new VideoListResponse {
                Items = new List<Video> {
                    new Video {
                        Id = playlistItemListResponse.Items[0].Id
                    },
                    new Video {
                        Id = playlistItemListResponse.Items[1].Id
                    }
                }
            };
            var provider = new Mock<IDepenedencyProvder>(MockBehavior.Strict);
            provider.Setup(x => x.YouTubeApiVideos(playlistItemListResponse.Items[0].Snippet.ResourceId.VideoId)).Returns(Task.FromResult(videoListResponse));
            provider.Setup(x => x.YouTubeApiVideos(playlistItemListResponse.Items[1].Snippet.ResourceId.VideoId)).Returns(Task.FromResult(new VideoListResponse {
                Items = new List<Video>()
            }));
            var logic = new Logic(provider.Object, new Logger());
            
            // Act
            var result = await logic.GetChannelTitle(playlistItemListResponse.Items[0].Snippet.ResourceId.VideoId, playlistItemListResponse.Items[0].Snippet);
            var result2 = await logic.GetChannelTitle(playlistItemListResponse.Items[1].Snippet.ResourceId.VideoId, playlistItemListResponse.Items[1].Snippet);

            // Assert
            Assert.Equal(playlistItemListResponse.Items[0].Snippet.ChannelTitle, result);
            Assert.Equal("N/A", result2);
        }
        
        [Fact]
        public async Task CreatePlaylistItemsFromPlaylistIdGenerateMonitorPlaylistItemTest() {
            
            // Arrange
            var itemId = "foo-bar-id-1";
            var channelTitle = "foo-bar-channel-title-1";
            var videoId = "foo-bar-video-id-1";
            var videoTitle = "foo-bar-title-1";
            var description = "foo-bar-description-1";
            var position = 1;
            var expectedResponse = new PlaylistItem {
                Id = itemId,
                Description = description,
                Link = $"https://www.youtube.com/watch?v={videoId}",
                Position = position,
                Title = videoTitle,
                ChannelTitle = channelTitle
            };
            var provider = new Mock<IDepenedencyProvder>(MockBehavior.Strict);
            var logic = new Logic(provider.Object, new Logger());
            
            // Act
            var result = logic.GenerateMonitorPlaylistItem(channelTitle, itemId, videoId, videoTitle, description, position);
            var result2 = logic.GenerateMonitorPlaylistItem(channelTitle, itemId, videoId, videoTitle, "", position);

            // Assert
            Assert.Equal(expectedResponse.Id, result.Id);
            Assert.Equal(expectedResponse.Description, result.Description);
            Assert.Equal(expectedResponse.Link, result.Link);
            Assert.Equal(expectedResponse.Title, result.Title);
            Assert.Equal(expectedResponse.Position, result.Position);
            Assert.Equal(expectedResponse.ChannelTitle, result.ChannelTitle);
            Assert.Null(result2.Description);
        }

        
        [Fact]
        public void GetPlaylistIdTest() {
            
            // Arrange
            var requestedPlaylistName = "foo-bar-title-4";
            var currentItems = new List<Playlist> {
                new Playlist {
                    Id = "foo-bar-id-1",
                    Snippet = new PlaylistSnippet {
                        Title = "foo-bar-title-1",
                        ChannelTitle = "foo-bar-channel-title-1",
                        Description = "foo-bar-description-1",
                    }
                },
                new Playlist {
                    Id = "foo-bar-id-4",
                    Snippet = new PlaylistSnippet {
                        Title = "foo-bar-title-4",
                        ChannelTitle = "foo-bar-channel-title-4",
                        Description = "foo-bar-description-4",
                    }
                }
            };
            var provider = new Mock<IDepenedencyProvder>(MockBehavior.Strict);
            var logic = new Logic(provider.Object, new Logger());
            
            // Act
            var result = logic.GetPlaylistId(currentItems, requestedPlaylistName);
            var result2 = logic.GetPlaylistId(new List<Playlist>(), requestedPlaylistName);

            // Assert
            Assert.Equal(currentItems[1].Id, result);
            Assert.Null(result2);
        }
        
        [Fact]
        public async Task SendEmailTest() {
            
            // Arrange
            string emailSubject = "foo-bar-subject";
            string emailBody = "foo-bar-body";
            string requestEmail = "foo-bar-request-email";
            string fromEmail = "foo-bar-from-email";
            var provider = new Mock<IDepenedencyProvder>(MockBehavior.Strict);
            provider.Setup(x => x.SesSendEmail(It.Is<SendEmailRequest>(y =>
                    y.Content.Simple.Subject.Data == emailSubject &&
                    y.Content.Simple.Body.Html.Data == emailBody &&
                    y.Destination.ToAddresses[0].Contains(requestEmail) &&
                    y.FromEmailAddress == fromEmail
            ))).Returns(Task.CompletedTask);
            var logic = new Logic(provider.Object, new Logger());
            
            // Act
            await logic.SendEmail(emailSubject, emailBody, requestEmail, fromEmail);

            // Assert
            provider.Verify(x => x.SesSendEmail(It.Is<SendEmailRequest>(y =>
                y.Content.Simple.Subject.Data == emailSubject &&
                y.Content.Simple.Body.Html.Data == emailBody &&
                y.Destination.ToAddresses[0].Contains(requestEmail) &&
                y.FromEmailAddress == fromEmail
            )), Times.Once());
        }
        
        [Fact]
        public async Task GenerateEmailBodyTest() {
            
            // Arrange
            var requestedPlaylistName = "foo-bar-playlist-name";
            var deletedItems = new List<PlaylistItem> {
                new PlaylistItem {
                    Id = "foo-bar-id-1",
                    Description = "foo-bar-description-1",
                    Link = "http://foo-bar-link-1",
                    Position = 1,
                    Title = "foo-bar-title-1",
                    ChannelTitle = "foo-bar-channel-title-1"
                },
                new PlaylistItem {
                    Id = "foo-bar-id-3",
                    Description = null,
                    Link = "http://foo-bar-link-3",
                    Position = 3,
                    Title = "foo-bar-title-3",
                    ChannelTitle = "foo-bar-channel-title-3"
                }
            };
            var expected = $"<h3>{requestedPlaylistName}</h3><br /><br /><strong><a href=\"{deletedItems[0].Link}\">{deletedItems[0].Title}</a></strong><br />by {deletedItems[0].ChannelTitle}<br />{deletedItems[0].Description}<br /><br /><strong><a href=\"{deletedItems[1].Link}\">{deletedItems[1].Title}</a></strong><br />by {deletedItems[1].ChannelTitle}<br /><br />";
            var provider = new Mock<IDepenedencyProvder>(MockBehavior.Strict);
            var logic = new Logic(provider.Object, new Logger());
            
            // Act
            var response = logic.GenerateEmailBody(requestedPlaylistName, deletedItems);

            // Assert
            Assert.Equal(expected, response);
        }

        
        [Fact]
        public async Task GetChannelIdTest() {
            
            // Arrange
            var requestedPlaylistUserName = "foo-bar-username";
            var requestedPlaylistUserName2 = "foo-bar-username2";
            var channelListResponse = new ChannelListResponse {
                Items = new List<Channel> {
                    new Channel {
                        Id = "foo-bar-channel-id"
                    }
                }
            };
            var provider = new Mock<IDepenedencyProvder>(MockBehavior.Strict);
            provider.Setup(x => x.YouTubeApiChannelsList(requestedPlaylistUserName)).Returns(Task.FromResult(channelListResponse));
            provider.Setup(x => x.YouTubeApiChannelsList(requestedPlaylistUserName2)).Returns(Task.FromResult(new ChannelListResponse {
                Items = new List<Channel>()
            }));
            var logic = new Logic(provider.Object, new Logger());
            
            // Act
            var result = await logic.GetChannelId(requestedPlaylistUserName);
            var result2 = await logic.GetChannelId(requestedPlaylistUserName2);

            // Assert
            Assert.Equal("foo-bar-channel-id", result);
            Assert.Null(result2);
        }

        
        [Fact]
        public async Task GetExistingPlaylistTest() {
            
            // Arrange
            var requestedEmail = "foo-bar-email";
            var playlistId = "foo-bar-playlist-id";
            var currentItems = new List<PlaylistItem> {
                new PlaylistItem {
                    Id = "foo-bar-id-1",
                    Link = "https://www.youtube.com/watch?v=foo-bar-link-1",
                    Position = 1,
                    Title = "foo-bar-title-1",
                    ChannelTitle = "foo-bar-channel-title-1"
                },
                new PlaylistItem {
                    Id = "foo-bar-id-4",
                    Description = "foo-bar-description-4",
                    Link = "https://www.youtube.com/watch?v=foo-bar-link-4",
                    Position = 4,
                    Title = "foo-bar-title-4",
                    ChannelTitle = "foo-bar-channel-title-4"
                }
            };
            var getItemResponse = new GetItemResponse {
                Item = new Dictionary<string, AttributeValue> {
                    {"playlists", new AttributeValue {
                        L = new List<AttributeValue> {
                            new AttributeValue {
                                S = "{\"id\":\"foo-bar-id-4\",\"description\":\"foo-bar-description-4\",\"title\":\"foo-bar-title-4\",\"link\":\"http://foo-bar-link-4\",\"author\":\"foo-bar-channel-title-4\",\"position\":4}"
                            },
                            new AttributeValue {
                                S = "{\"id\":\"foo-bar-id-1\",\"title\":\"foo-bar-title-1\",\"link\":\"http://foo-bar-link-1\",\"author\":\"foo-bar-channel-title-1\",\"position\":1}"
                            }
                        }
                    }}
                }
            };
            var provider = new Mock<IDepenedencyProvder>(MockBehavior.Strict);
            provider.Setup(x => x.DynamoDbGetPlaylistList(requestedEmail, playlistId)).Returns(Task.FromResult(getItemResponse));
            var logic = new Logic(provider.Object, new Logger());
            
            // Act
            var result = await logic.GetExistingPlaylist(requestedEmail, playlistId);

            // Assert
            Assert.Equal(currentItems[0].Id, result[1].Id);
            Assert.Null(result[1].Description);
        }

        
        [Fact]
        public async Task GetSubscriptionsTest() {
            
            // Arrange
            var getResponse = new ScanResponse {
                Items = new List<Dictionary<string, AttributeValue>> {
                    new Dictionary<string, AttributeValue> {
                        { "email", new AttributeValue {
                            S = "foo-bar-email-1"
                        }},
                        {"playlists", new AttributeValue {
                            L = new List<AttributeValue> {
                                new AttributeValue {
                                    S = JsonConvert.SerializeObject(new Subscription {
                                        UserName = "user-1-1",   
                                        PlaylistName = "playlist-1-1"
                                    })
                                },
                                new AttributeValue {
                                    S = JsonConvert.SerializeObject(new Subscription {
                                        UserName = "user-1-2",   
                                        PlaylistName = "playlist-1-2"
                                    })
                                }
                            }
                        }}
                    },
                    new Dictionary<string, AttributeValue> {
                        { "email", new AttributeValue {
                            S = "foo-bar-email-2"
                        }},
                        {"playlists", new AttributeValue {
                            L = new List<AttributeValue> {
                                new AttributeValue {
                                    S = JsonConvert.SerializeObject(new Subscription {
                                        UserName = "user-2-1",   
                                        PlaylistName = "playlist-2-1"
                                    })
                                },
                                new AttributeValue {
                                    S = JsonConvert.SerializeObject(new Subscription {
                                        UserName = "user-2-2",   
                                        PlaylistName = "playlist-2-2"
                                    })
                                }
                            }
                        }}
                    }
                }
            };
            var provider = new Mock<IDepenedencyProvder>(MockBehavior.Strict);
            provider.Setup(x => x.DynamoDbGetSubscriptionList()).Returns(Task.FromResult(getResponse));
            var logic = new Logic(provider.Object, new Logger());
            
            // Act
            var result = await logic.GetSubscriptions();
            // var result2 = await logic.GetChannelId(requestedPlaylistUserName2);

            // Assert
            var firstSubscription = result.FirstOrDefault();
            Assert.Equal(getResponse.Items[0].GetValueOrDefault("email").S, firstSubscription.Key);
            Assert.Equal("user-1-1", firstSubscription.Value[0].UserName);
            Assert.Equal("playlist-1-1", firstSubscription.Value[0].PlaylistName);
        }
    }
}