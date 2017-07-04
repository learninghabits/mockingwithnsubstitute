using mocking_with_nsubstitute.Controllers;
using mocking_with_nsubstitute.Repositories;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Hosting;

namespace testing_with_nunit
{
    [TestFixture]
    public class TopicControllerTests
    {
        IRepository<Topic> _topicsRepository;

        [SetUp]
        public void SetUp()
        {
            _topicsRepository = Substitute.For<IRepository<Topic>>();
        }

        [Test]
        public void TopicController_Constructor_WithANullRepository_WillThrowAnException()
        {
            //ARRANGE
            IRepository<Topic> repository = null;
            //ASSERT
            Assert.Throws<ArgumentNullException>(() => new TopicController(repository));
        }

        [Test]
        public void TopicController_Get_WhenTheRepositoryReturns2Topics_WillReturn2Topics()
        {
            var topics = new List<Topic>
                             {
                                 new Topic {Name = "ASP.NET Core", Id = 1 },
                                 new Topic {Name = "Docker for .NET Developers", Id = 2 }
                             }
                            .AsQueryable();

            _topicsRepository.All
                             .Returns(topics);                             

            var controller = new TopicController(_topicsRepository);
            SetUpHttpRequestParameters(controller);
            var response = controller.Get();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            IEnumerable<dynamic> resultData;
            Assert.IsTrue(response.TryGetContentValue(out resultData));
            var topicsArray = resultData.ToArray();
            var expandoDict0 = (IDictionary<string, object>)topicsArray[0];
            Assert.AreEqual("ASP.NET Core", expandoDict0["name"]);
            Assert.AreEqual(1, expandoDict0["id"]);
            var expandoDict1 = (IDictionary<string, object>)topicsArray[1];
            Assert.AreEqual("Docker for .NET Developers", expandoDict1["name"]);
            Assert.AreEqual(2, expandoDict1["id"]);            
        }

        //In a real world applications we should not bubble exceptions from a service but this demonstrates a point.
        [Test]
        public void TopicController_Get_WhenTheRepositoryThrowsAnException_ItWillBubbleUp()
        {
            _topicsRepository.All                            
                             .Returns(x => { throw new Exception(); });
            var controller = new TopicController(_topicsRepository);
            SetUpHttpRequestParameters(controller);
            Assert.Throws<Exception>(() => controller.Get());
        }

        [Test]
        public void TopicController_Get_WhenTheRepositoryFindsARequestedTopic_WillReturnATopic()
        {
            _topicsRepository.Find(Arg.Any<int>())
                             .Returns(new Topic { Name = "ASP.NET Core", Id = 1 });
                             
            var controller = new TopicController(_topicsRepository);
            SetUpHttpRequestParameters(controller);
            var response = controller.Get(1);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            ExpandoObject expando;
            Assert.IsTrue(response.TryGetContentValue(out expando));
            var expandoDict = (IDictionary<string, object>)expando;
            Assert.AreEqual("ASP.NET Core", expandoDict["name"]);
            Assert.AreEqual(1, expandoDict["id"]);
            _topicsRepository.Received().Find(Arg.Any<int>());
        }

        [Test]
        public void TopicController_Get_WhenTheRepositoryDoesNotFindARequestedTopic_WillReturnA404()
        {
            _topicsRepository.Find(Arg.Any<int>())
                            .Returns((Topic)null);                          

            var controller = new TopicController(_topicsRepository);
            SetUpHttpRequestParameters(controller);
            var response = controller.Get(3);
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            ExpandoObject expando;
            Assert.IsTrue(response.TryGetContentValue(out expando));
            var expandoDict = (IDictionary<string, object>)expando;
            Assert.AreEqual("The topic you requested was not found", expandoDict["message"]);
        }

        [Test]
        public void TopicController_Get_WhenTheRepositoryFindsATopicForTheGivenIdAndTutorialName_WillReturnATopic()
        {
            var topics = new List<Topic>
                {
                    new Topic {Name = "ASP.NET Core", Id = 1, Tutorials = new List<Tutorial>
                    {
                        new Tutorial
                        {
                            Name = "ASP.NET Core on Ubuntu",
                            Type = "video",
                            Url = "http://www.learninghabits.co.za/#/topics/ubuntu"
                        }
                    }},
                    new Topic {Name = "Docker for .NET Developers", Id = 2 }
                };
            _topicsRepository.Where(Arg.Any<Expression<Func<Topic, bool>>>()).Returns(topics);
            var controller = new TopicController(_topicsRepository);
            SetUpHttpRequestParameters(controller);
            var response = controller.Get(1, "ASP.NET Core on Ubuntu");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            ExpandoObject expando;
            Assert.IsTrue(response.TryGetContentValue(out expando));
            var expandoDict = (IDictionary<string, object>)expando;
            Assert.AreEqual(1, expandoDict["id"]);
            var tutorials = expandoDict["tutorials"] as IEnumerable<dynamic>;
            Assert.IsNotNull(tutorials);
            var tutorialArray = tutorials.ToArray();
            Assert.AreEqual(1, tutorialArray.Length);
            Assert.AreEqual("ASP.NET Core on Ubuntu", ((IDictionary<string, object>)tutorialArray[0])["name"]);
        }

        [Test]
        public void TopicController_Get_WhenTheRepositoryDoesNotFindATopicForTheGivenIdAndTuorialName_WillReturnA404()
        {
            _topicsRepository.Where(Arg.Any<Expression<Func<Topic, bool>>>()).Returns(new List<Topic> { });
            var controller = new TopicController(_topicsRepository);
            SetUpHttpRequestParameters(controller);
            var response = controller.Get(1, "ASP.NET Core on Ubuntu");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            ExpandoObject expando;
            Assert.IsTrue(response.TryGetContentValue(out expando));
            var expandoDict = (IDictionary<string, object>)expando;
            Assert.AreEqual("The tutorial  you requested was not found", expandoDict["message"]);
        }

        [Test]
        public void TopicController_Post_WhenTheTopicIsAddedSuccessfully_WillReturnAnOKStatusAndANavigationProperty()
        {
            var controller = new TopicController(_topicsRepository);
            SetUpHttpRequestParameters(controller);
            var response = controller.Post(new Topic
            {
                Name = "Visual Studio on a Mac",
                Tutorials = new List<Tutorial> { }
            });
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            ExpandoObject expando;
            Assert.IsTrue(response.TryGetContentValue(out expando));
            var expandoDict = (IDictionary<string, object>)expando;
            Assert.AreEqual(0, expandoDict["id"]);
            Assert.AreEqual("http://localhost/api/Topic/0", expandoDict["url"]);
            _topicsRepository.Received(1).Insert(Arg.Any<Topic>());
            _topicsRepository.Received(1).Save();
        }

        [Test]
        public void TopicController_Put_WhenTheTopicIsUpdatedSuccessfully_WillReturnAnOKStatusAndASuccessMessage()
        {
            var controller = new TopicController(_topicsRepository);
            SetUpHttpRequestParameters(controller);
            var response = controller.Put(new Topic
            {
                Name = "Visual Studio on a Mac",
                Tutorials = new List<Tutorial> { }
            });
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            ExpandoObject expando;
            Assert.IsTrue(response.TryGetContentValue(out expando));
            var expandoDict = (IDictionary<string, object>)expando;
            Assert.AreEqual("topic is updated successfully", expandoDict["message"]);
            _topicsRepository.Received(1).Update(Arg.Any<Topic>());
            _topicsRepository.Received(1).Save();
        }

        [Test]
        public void TopicController_Delete_WhenTheTopicIsDeletedSuccessfully_WillReturnAnOKStatusAndASuccessMessage()
        {
            var controller = new TopicController(_topicsRepository);
            SetUpHttpRequestParameters(controller);
            var response = controller.Delete(1);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            ExpandoObject expando;
            Assert.IsTrue(response.TryGetContentValue(out expando));
            var expandoDict = (IDictionary<string, object>)expando;
            Assert.AreEqual("The topic was deleted successfully", expandoDict["message"]);
            _topicsRepository.Received().Delete(Arg.Any<int>());
            _topicsRepository.Received().Save();
        }

        private void SetUpHttpRequestParameters(TopicController controller)
        {
            controller.Request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/Topic");
            controller.Request.Properties[HttpPropertyKeys.HttpConfigurationKey] = new HttpConfiguration();
        }
    }
}
