#if UNITY_EDITOR
using _001_Scripts.Data;
using _001_Scripts.Data.Message;
using _001_Scripts.Managers;
using MessagePipe;
using NUnit.Framework;
using System;

namespace _001_Scripts.Editor.Tests
{
    public sealed class LogCollectionEditModeTests
    {
        [Test]
        public void Add_StoresUniqueLogAndPublishesChange()
        {
            var publisher = new FakePublisher();
            var service = new LogCollectionService(publisher);
            var entry = new LogEntry { id = "log-1", title = "First", body = "Body" };

            Assert.That(service.Add(entry), Is.True);
            Assert.That(service.Add(entry), Is.False);
            Assert.That(service.Contains("LOG-1"), Is.True);
            Assert.That(service.GetAllLogs(), Has.Count.EqualTo(1));
            Assert.That(publisher.Count, Is.EqualTo(1));
            Assert.That(publisher.LastId, Is.EqualTo("log-1"));
        }

        [Test]
        public void JsonCatalog_MapsEntriesByIdAndRejectsDuplicates()
        {
            var catalog = new JsonLogCatalog();
            catalog.LoadJson("{\"logs\":[{\"id\":\"alpha\",\"title\":\"Alpha\",\"body\":\"Body\",\"imageResource\":\"\"}]}");

            Assert.That(catalog.Exists("ALPHA"), Is.True);
            Assert.That(catalog.Get("alpha").title, Is.EqualTo("Alpha"));
            Assert.That(catalog.GetAll(), Has.Count.EqualTo(1));
            Assert.Throws<FormatException>(() => catalog.LoadJson(
                "{\"logs\":[{\"id\":\"same\",\"title\":\"A\"},{\"id\":\"same\",\"title\":\"B\"}]}"));
        }

        private sealed class FakePublisher : IPublisher<LogCollectionChangedMessage>
        {
            public int Count { get; private set; }
            public string LastId { get; private set; }

            public void Publish(LogCollectionChangedMessage message)
            {
                Count++;
                LastId = message.LogId;
            }
        }
    }
}
#endif
