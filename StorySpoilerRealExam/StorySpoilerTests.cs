using StorySpoilerRealExam.Models;
using System;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace StorySpoilerRealExam.Tests
{
    [TestFixture]
    public class StorySpoilerTests
    {
        private RestClient client;
        private string? createdStoryId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {

            string token = GetJwtToken("vasi456", "vasi456");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString() ?? string.Empty;

        }


        [Test, Order(1)]
        public void CreateStory_ShouldReturnCreatedStory()
        {
            var story = new StoryDTO
            {
                Title = "New story",
                Description = "This is a new story.",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);

            if (json.TryGetProperty("storyId", out var idProp))
            {
                createdStoryId = idProp.GetString() ?? string.Empty;
            }
            else if (json.TryGetProperty("data", out var dataProp) && dataProp.TryGetProperty("storyId", out var nestedIdProp))
            {
                createdStoryId = nestedIdProp.GetString() ?? string.Empty;
            }
            else
            {
                Assert.Fail("Could not extract storyId from response: " + response.Content);
            }

            Assert.That(createdStoryId, Is.Not.Null.And.Not.Empty);
            Assert.That(response.Content, Does.Contain("Successfully created!"));
        }

        [Test, Order(2)]

        public void EditCreatedStory_ShouldReturnUpdatedStory()
        {
            var updatedStory = new StoryDTO
            {
                Title = "Edited story",
                Description = "This is the edied story.",
                Url = ""
            };
            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);

            request.AddJsonBody(updatedStory);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(response.Content, Does.Contain("Successfully edited"));
        }


        [Test, Order(3)]

        public void GetAllStories_ShouldReturnListOfStories()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var stories = JsonSerializer.Deserialize<List<StoryDTO>>(response.Content);

            Assert.That(stories, Is.Not.Empty);

        }

        [Test, Order(4)]
        public void DeletedStory_ShouldReturnOk()
        {
            Assert.That(createdStoryId, Is.Not.Null.And.Not.Empty);

            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);
            var response = client.Execute(request);

            if (string.IsNullOrEmpty(response.Content))
            {
                Assert.Fail();
            }

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));
        }


        [Test, Order(5)]

        public void CreateStoryWithNonExistedData_ShouldReturnBadRequest()
        {
            var fakeStory = new StoryDTO
            {
                Title = "",
                Description = "",

            };
            var request = new RestRequest("/api/Story/Create", Method.Post);

            request.AddJsonBody(fakeStory);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]

        public void EditedNonExistedStory_ShouldReturnNotFound()
        {

            var fakeStoryId = "741852963";
            var fakeStory = new
            {
                Title = "Fake story",
                Description = "Fake story here",

            };
            var request = new RestRequest($"/api/Story/Edit/{fakeStoryId}", Method.Put);

            request.AddJsonBody(fakeStory);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            Assert.That(response.Content, Does.Contain("No spoilers..."));
        }

        [Test, Order(7)]

        public void DeleteNonExistedStory_ShouldReturnBadRequest()
        {
            var fakeStoryId = "123456789";
            var request = new RestRequest($"/api/Story/Delete/{fakeStoryId}", Method.Delete);
            var response = client.Execute(request);


            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this story spoiler!"));

        }


        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}